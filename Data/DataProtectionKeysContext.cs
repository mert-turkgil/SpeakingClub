using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SpeakingClub.Data
{
    // Stores ASP.NET Core DataProtection keys in SQL Server instead of the file system,
    // so key rotation no longer depends on write permissions in the Plesk publish folder.
    public class DataProtectionKeysContext : DbContext, IDataProtectionKeyContext
    {
        public DataProtectionKeysContext(DbContextOptions<DataProtectionKeysContext> options)
            : base(options)
        {
        }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

        // Production only applies migrations on an empty database, so the table is
        // created with idempotent SQL instead of a migration.
        public void EnsureKeysTable()
        {
            Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[dbo].[DataProtectionKeys]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DataProtectionKeys] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_DataProtectionKeys] PRIMARY KEY,
        [FriendlyName] NVARCHAR(MAX) NULL,
        [Xml] NVARCHAR(MAX) NULL
    );
END");
        }
    }
}
