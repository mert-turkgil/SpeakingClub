// ============================================================================
// DbBackup: data-only backup of the production database, for plesk-deploy.yml.
//
// The deploy backs the database up before it applies EF migrations. Its first
// choice is a SqlPackage .bacpac, but SqlPackage reads the whole schema, which
// needs the VIEW DEFINITION permission. The Plesk database login doesn't have
// it and can't grant it to itself. This tool needs nothing beyond SELECT: it
// writes every row of every table into a T-SQL restore script. The schema comes
// back from the EF migrations, so the backup records which ones the database had.
//
// Output: a .zip holding data.sql (the restore script) and README.txt (the
// migrations, row counts per table, where the restore steps are).
// Restoring: .github/deploy/RESTORE.md, Path C.
//
// Usage: BACKUP_CONNECTION_STRING="..." dotnet run --project .github/tools/DbBackup -- <output.zip>
// ============================================================================
using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using Microsoft.Data.SqlClient;

const string HistoryTable = "__EFMigrationsHistory";
const int RowsPerInsert = 100;   // rows per INSERT ... VALUES (SQL Server allows up to 1000)
const int InsertsPerBatch = 10;  // INSERT statements between GO separators

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: DbBackup <output.zip>   (connection string in BACKUP_CONNECTION_STRING)");
    return 2;
}
var connectionString = Environment.GetEnvironmentVariable("BACKUP_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("BACKUP_CONNECTION_STRING is not set.");
    return 2;
}
var outputPath = Path.GetFullPath(args[0]);
var commit = Environment.GetEnvironmentVariable("GITHUB_SHA") is { Length: >= 7 } sha ? sha[..7] : null;

// Tables the app creates at startup instead of through a migration. The restore script creates them
// when they are missing, with the same SQL the app uses (Data/DataProtectionKeysContext.cs).
var createdByApp = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["dbo.DataProtectionKeys"] = """
        CREATE TABLE [dbo].[DataProtectionKeys] (
            [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_DataProtectionKeys] PRIMARY KEY,
            [FriendlyName] NVARCHAR(MAX) NULL,
            [Xml] NVARCHAR(MAX) NULL
        );
        """
};

try
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    var database = connection.Database;
    var takenAt = DateTime.UtcNow;

    // One serializable transaction for every read, so the tables agree with each other (no
    // submission pointing at a user created halfway through). The site is still online: its
    // writes wait the few seconds this takes.
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
    SqlCommand Command(string sql) => new(sql, connection, transaction) { CommandTimeout = 600 };

    // Every user table this login can read. With SELECT on the database (db_datareader) that is all of them.
    var tables = new List<Table>();
    await using (var command = Command("""
        SELECT s.name, t.name
        FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE t.is_ms_shipped = 0
        ORDER BY s.name, t.name
        """))
    await using (var reader = await command.ExecuteReaderAsync())
    {
        while (await reader.ReadAsync()) tables.Add(new Table(reader.GetString(0), reader.GetString(1)));
    }
    if (tables.Count == 0)
    {
        Console.Error.WriteLine($"No tables are visible in '{database}'. The login needs at least SELECT on the database (db_datareader).");
        return 1;
    }

    // The migrations rebuild __EFMigrationsHistory themselves, so its rows go into README.txt, not data.sql.
    // There is one per schema the migrations ran in (tables land in the default schema of the login
    // that ran them).
    var histories = tables.Where(t => t.Name == HistoryTable).ToList();
    var migrations = new List<(Table History, string Id)>();
    foreach (var history in histories)
    {
        await using var command = Command($"SELECT MigrationId FROM {history.Quoted} ORDER BY MigrationId");
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) migrations.Add((history, reader.GetString(0)));
    }
    var dataTables = tables.Except(histories).ToList();
    var counts = new List<(Table Table, long Rows)>();

    await using (var file = File.Create(outputPath))
    using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
    {
        using (var sql = new StreamWriter(zip.CreateEntry("data.sql").Open(), new UTF8Encoding(true)) { NewLine = "\r\n" })
        {
            var script = new RestoreScript(sql);
            sql.WriteLine($"-- Data backup of [{database}], taken {takenAt:yyyy-MM-dd HH:mm:ss} UTC{(commit != null ? $" before deploying {commit}" : "")}.");
            sql.WriteLine("-- Replaces every row in the tables below. Rebuild the tables with the EF migrations listed in");
            sql.WriteLine("-- README.txt first, then run this with `sqlcmd -b -f 65001 -i data.sql` or in SSMS.");
            sql.WriteLine("-- It runs as one transaction: it either completes or changes nothing. See .github/deploy/RESTORE.md, Path C.");
            sql.WriteLine();
            sql.WriteLine("SET NOCOUNT ON;");
            sql.WriteLine("SET XACT_ABORT ON;");
            script.Begin();
            foreach (var table in dataTables)
            {
                script.Statement(createdByApp.TryGetValue(table.Key, out var create)
                    ? $"IF OBJECT_ID(N'{table.QuotedForString}', N'U') IS NULL\r\nBEGIN\r\n{create.TrimEnd()}\r\nEND;"
                    : $"IF OBJECT_ID(N'{table.QuotedForString}', N'U') IS NULL THROW 50000, N'Table {table.QuotedForString} does not exist. Rebuild the schema with the migrations in README.txt first.', 1;");
            }
            script.Statement("BEGIN TRANSACTION;");
            // Foreign keys are off while the rows go in (the order of the tables doesn't matter then) and are
            // checked again at the end. The DELETEs also clear rows the migrations seeded (HasData).
            foreach (var table in dataTables) script.Statement($"ALTER TABLE {table.Quoted} NOCHECK CONSTRAINT ALL;");
            foreach (var table in dataTables) script.Statement($"DELETE FROM {table.Quoted};");
            script.Go();

            foreach (var table in dataTables)
            {
                await using var command = Command($"SELECT * FROM {table.Quoted}");
                await using var reader = await command.ExecuteReaderAsync();
                // Computed and rowversion columns can't be inserted; the database fills them in.
                var columns = reader.GetColumnSchema()
                    .Where(c => c.IsExpression != true && c.DataTypeName is not ("timestamp" or "rowversion"))
                    .ToList();
                var identity = columns.Any(c => c.IsIdentity == true);
                var insert = $"INSERT INTO {table.Quoted} ({string.Join(", ", columns.Select(c => Quote(c.ColumnName)))}) VALUES";
                var rows = new List<string>(RowsPerInsert);
                long count = 0;
                var inserts = 0;

                void Flush()
                {
                    if (rows.Count == 0) return;
                    if (count == rows.Count && identity) script.Statement($"SET IDENTITY_INSERT {table.Quoted} ON;");
                    script.Statement(insert + "\r\n" + string.Join(",\r\n", rows) + ";");
                    rows.Clear();
                    if (++inserts % InsertsPerBatch == 0) script.Go();
                }

                while (await reader.ReadAsync())
                {
                    rows.Add("(" + string.Join(", ", columns.Select(c => Literal(reader.GetValue(c.ColumnOrdinal!.Value)))) + ")");
                    count++;
                    if (rows.Count == RowsPerInsert) Flush();
                }
                Flush();
                if (identity && count > 0) script.Statement($"SET IDENTITY_INSERT {table.Quoted} OFF;");
                script.Go();
                counts.Add((table, count));
            }

            foreach (var table in dataTables) script.Statement($"ALTER TABLE {table.Quoted} WITH CHECK CHECK CONSTRAINT ALL;");
            script.Statement("COMMIT TRANSACTION;");
            script.Statement($"PRINT N'Restore complete: {counts.Sum(c => c.Rows)} rows in {counts.Count} tables.';");
            script.Go();
        }

        using var readme = new StreamWriter(zip.CreateEntry("README.txt").Open(), new UTF8Encoding(false)) { NewLine = "\r\n" };
        readme.WriteLine($"Data backup of {database}, taken {takenAt:yyyy-MM-dd HH:mm:ss} UTC{(commit != null ? $" before deploying {commit}" : "")}.");
        readme.WriteLine();
        readme.WriteLine("data.sql holds every row of every table. The tables themselves are rebuilt from the");
        readme.WriteLine("EF Core migrations, so first bring an empty database to the state this one had. The");
        readme.WriteLine("migrations it had applied (newest last):");
        foreach (var group in migrations.GroupBy(m => m.History))
        {
            readme.WriteLine();
            readme.WriteLine($"  in schema [{group.Key.Schema}]:");
            foreach (var (_, id) in group) readme.WriteLine("    " + id);
        }
        if (migrations.Count == 0) readme.WriteLine("  (none: there was no __EFMigrationsHistory table)");
        readme.WriteLine();
        readme.WriteLine("Rows per table:");
        readme.WriteLine();
        var width = counts.Max(c => c.Table.Key.Length) + 2;
        foreach (var (table, rows) in counts) readme.WriteLine($"  {table.Key.PadRight(width)}{rows,8}");
        readme.WriteLine();
        readme.WriteLine("How to restore: .github/deploy/RESTORE.md in the repository, \"Path C\".");
    }

    await transaction.CommitAsync();
    Console.WriteLine($"Backed up {counts.Sum(c => c.Rows)} rows from {counts.Count} tables of '{database}' to {outputPath}.");
    foreach (var group in migrations.GroupBy(m => m.History)) Console.WriteLine($"Migrations applied in [{group.Key.Schema}]: {group.Count()}, newest {group.Last().Id}");
    foreach (var (table, rows) in counts) Console.WriteLine($"  {table.Key}: {rows}");
    return 0;
}
catch (Exception ex)
{
    // No half-written backup left behind for the workflow to upload.
    if (File.Exists(outputPath)) File.Delete(outputPath);
    Console.Error.WriteLine($"Backup failed: {ex.Message}");
    return 1;
}

static string Quote(string name) => "[" + name.Replace("]", "]]") + "]";

// A T-SQL literal for a value as SqlClient returns it.
static string Literal(object value) => value switch
{
    DBNull => "NULL",
    string s => StringLiteral(s),
    bool b => b ? "1" : "0",
    byte or short or int or long or decimal => Convert.ToString(value, CultureInfo.InvariantCulture)!,
    double d => d.ToString("R", CultureInfo.InvariantCulture),
    float f => f.ToString("R", CultureInfo.InvariantCulture),
    // ISO 8601 with the T is read the same way whatever the server's language or DATEFORMAT is
    DateTime dt => $"CAST('{dt.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture)}' AS datetime2(7))",
    DateTimeOffset dto => $"CAST('{dto.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffzzz", CultureInfo.InvariantCulture)}' AS datetimeoffset(7))",
    TimeSpan ts => $"CAST('{ts.ToString(@"hh\:mm\:ss\.fffffff", CultureInfo.InvariantCulture)}' AS time(7))",
    Guid g => $"'{g}'",
    byte[] bytes => "0x" + Convert.ToHexString(bytes),
    _ => throw new NotSupportedException($"Can't write a value of type {value.GetType()} into the restore script.")
};

// N'...' with quotes doubled. Line breaks become NCHAR(13)/NCHAR(10), so no line of the script can
// look like a GO separator, and "$(" is split so sqlcmd doesn't read it as a scripting variable.
// The pieces are joined as NVARCHAR(MAX); joining short literals would cut the text at 4000 characters.
static string StringLiteral(string s)
{
    var body = s.Replace("'", "''");
    if (body.IndexOfAny(['\r', '\n']) < 0 && !body.Contains("$(")) return "N'" + body + "'";
    body = body.Replace("$(", "$'+N'(").Replace("\r\n", "'+NCHAR(13)+NCHAR(10)+N'")
        .Replace("\r", "'+NCHAR(13)+N'").Replace("\n", "'+NCHAR(10)+N'");
    return "CAST(N'' AS NVARCHAR(MAX))+N'" + body + "'";
}

record Table(string Schema, string Name)
{
    public string Key => $"{Schema}.{Name}";
    public string Quoted => $"[{Schema.Replace("]", "]]")}].[{Name.Replace("]", "]]")}]";
    public string QuotedForString => Quoted.Replace("'", "''");
}

// Writes the restore script's statements in GO-separated batches. The first batch opens the
// transaction. Each batch ends by recording its number in the session, and each later batch starts
// by checking the previous one got that far. A batch that failed (a runtime error rolls back through
// XACT_ABORT, a compile error doesn't) never records its number, so the next batch rolls back and
// stops, and so does every batch after it, in sqlcmd and in SSMS alike.
sealed class RestoreScript(TextWriter writer)
{
    private int _completed;
    private bool _open;

    public void Begin()
    {
        writer.WriteLine("EXEC sp_set_session_context N'restore_step', 0;");
        _open = true;
    }

    public void Statement(string sql)
    {
        if (!_open)
        {
            writer.WriteLine($"IF ISNULL(CAST(SESSION_CONTEXT(N'restore_step') AS int), -1) <> {_completed} BEGIN IF @@TRANCOUNT > 0 ROLLBACK; THROW 50000, N'Restore stopped: an earlier batch failed, nothing was changed.', 1; END;");
            _open = true;
        }
        writer.WriteLine(sql);
    }

    public void Go()
    {
        if (!_open) return;
        writer.WriteLine($"EXEC sp_set_session_context N'restore_step', {++_completed};");
        writer.WriteLine("GO");
        _open = false;
    }
}
