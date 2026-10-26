# Restoring the production database

Read this when a deploy has changed the database and the result is wrong: rows missing, a column
emptied, a migration that did more than it should. Nothing here is automatic — restoring is a
deliberate, manual act, and it overwrites whatever the site has written since the backup was taken.

## Where the backups are

| Backup | Where | Made when | Restored how |
|---|---|---|---|
| **Pre-migration `.bacpac`** (made by `plesk-deploy.yml`) | Plesk → Dosyalar → `httpdocs/publish/_db-backups/almanca-konus-YYYYMMDD-HHMMSS-<commit>.bacpac` | Automatically, right before a deploy applies EF migrations (or every deploy if `BACKUP_ALWAYS` is `'true'`). The last `BACKUP_KEEP` (10) are kept. | Path B below |
| **Pre-migration `.data.zip`** (made by `plesk-deploy.yml`) | Same folder, `almanca-konus-YYYYMMDD-HHMMSS-<commit>.data.zip` | Instead of the `.bacpac` when SqlPackage can't export one. That is every time today: the Plesk database login lacks the VIEW DEFINITION permission SqlPackage needs. If the hosting provider runs `GRANT VIEW DEFINITION TO [<the login>]` in `alma6203_db`, deploys make `.bacpac` files again. | Path C below |
| **Plesk dump** | Plesk → Veritabanları → `alma6203_db` → *Dökümü dışarı aktar* | Only when you click it | *Dökümü içeri aktar* — Path A |
| **Plesk backup** | Plesk → Yedekleme Yöneticisi | When scheduled / clicked there | Plesk → Yedekleme Yöneticisi → Geri yükle |

The `.bacpac` is a zip of the schema plus every row of every table, and the `.data.zip` holds every
row of every table. Both include users' personal data. Download them only to your own machine and
delete local copies when you are done.

**Before a deploy you already know is risky** (a migration with `DropColumn`, `DropTable`, or an
`AlterColumn` that changes a type), click **Dökümü dışarı aktar** in Plesk first. That is the
backup with the shortest restore path.

## Step 0 — stop the app

Restoring under a running app means it keeps writing while you overwrite. In Plesk → Dosyalar,
upload `.github/deploy/app_offline.htm` into `httpdocs/publish/`. IIS stops the app within seconds
and shows the maintenance page. The next deploy deletes the file again at the end; you can also
delete it by hand once you are finished.

## Path A — Plesk dump

Plesk → Veritabanları → `alma6203_db` → **Dökümü içeri aktar**, choose the dump you exported, and
confirm that the existing data is replaced.

## Path B — `.bacpac` with SqlPackage

SqlPackage refuses to import into a database that already has objects, so the database is emptied
first.

1. Step 0, then download the `.bacpac` you want from `_db-backups/` — pick the one whose commit hash
   matches the deploy that went wrong (the run's summary page on GitHub names the file too).
2. Check that the file opens: in SSMS on your PC, right-click **Databases → Import Data-tier
   Application…**, import it as `SpeakingClub_Restore`, and look at a few tables. This is the moment
   to notice you picked the wrong file.
3. In SSMS connected to the production server, empty the database. This drops every table — it is
   the point of no return:

   ```sql
   DECLARE @sql NVARCHAR(MAX) = N'';
   SELECT @sql += N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name)
                + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
   FROM sys.foreign_keys fk
   JOIN sys.tables t ON t.object_id = fk.parent_object_id
   JOIN sys.schemas s ON s.schema_id = t.schema_id;
   SELECT @sql += N'DROP TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N';' + CHAR(10)
   FROM sys.tables t
   JOIN sys.schemas s ON s.schema_id = t.schema_id;
   EXEC sp_executesql @sql;
   ```
4. From your PC (`dotnet tool install --global microsoft.sqlpackage` once):

   ```
   sqlpackage /Action:Import /SourceFile:"almanca-konus-....bacpac" /TargetConnectionString:"<the PROD_CONNECTION_STRING value>"
   ```

## Path C — `.data.zip` (data-only backup)

Made by `.github/tools/DbBackup`. It holds `data.sql`, a script with every row of every table, and
`README.txt`, which lists the migrations the database had and the row count of each table. The tables
themselves are rebuilt from the EF migrations first, then `data.sql` fills them.

1. Step 0, then download the `.data.zip` you want from `_db-backups/` and unzip it. As with Path B,
   pick the file whose commit hash matches the deploy that went wrong.
2. Empty the database with the SQL in Path B, step 3.
3. Rebuild the tables as they were. In the repository on your PC, check out the commit from the file
   name, then bring each EF context up to the newest migration README.txt lists for it. Migrations
   in `Data/Migrations/IdentityMigrations` belong to `ApplicationDbContext`, those in
   `Data/Migrations/SpeakingClubMigrations` to `SpeakingClubContext`:

   ```
   git checkout <commit>
   dotnet ef database update <newest Identity migration> --context ApplicationDbContext --connection "<the PROD_CONNECTION_STRING value>"
   dotnet ef database update <newest SpeakingClub migration> --context SpeakingClubContext --connection "<the PROD_CONNECTION_STRING value>"
   git checkout main
   ```

   Use the production login (`PROD_CONNECTION_STRING`), not an admin login. Tables land in the
   default schema of the login that creates them, which for the Plesk login is a schema named after
   it. README.txt shows which schema the backup's tables came from.
4. Fill the tables, from your PC:

   ```
   sqlcmd -S "<server-ip>\MSSQLSERVER2022" -d alma6203_db -U "<login>" -P "<password>" -b -f 65001 -i data.sql
   ```

   (Or open `data.sql` in SSMS, connected to `alma6203_db`, and press Execute.) It deletes the rows
   the migrations seeded, inserts every row from the backup, and checks the foreign keys, all in one
   transaction. It ends with `Restore complete: N rows in M tables.` If it stops with an error,
   nothing has been changed: fix the cause (usually a table from step 3 that doesn't match) and run
   it again.
5. Compare a few tables against the row counts in README.txt.

## After any restore — fix the code before the next deploy

The restore also rewinds `__EFMigrationsHistory`, so the migration that caused this is "pending"
again and the next push to `main` would re-apply it. `git revert` the commit that added it (or
replace it with a corrected migration), then push. That deploy takes a fresh backup, applies
whatever is now pending, and removes `app_offline.htm`.
