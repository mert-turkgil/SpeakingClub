# Restoring the production database

Read this when a deploy has changed the database and the result is wrong: rows missing, a column
emptied, a migration that did more than it should. Nothing here is automatic — restoring is a
deliberate, manual act, and it overwrites whatever the site has written since the backup was taken.

## Where the backups are

| Backup | Where | Made when | Restored how |
|---|---|---|---|
| **Pre-migration `.bacpac`** (made by `plesk-deploy.yml`) | Plesk → Dosyalar → `httpdocs/publish/_db-backups/almanca-konus-YYYYMMDD-HHMMSS-<commit>.bacpac` | Automatically, right before a deploy applies EF migrations (or every deploy if `BACKUP_ALWAYS` is `'true'`). The last `BACKUP_KEEP` (10) are kept. | Path B below |
| **Plesk dump** | Plesk → Veritabanları → `alma6203_db` → *Dökümü dışarı aktar* | Only when you click it | *Dökümü içeri aktar* — Path A |
| **Plesk backup** | Plesk → Yedekleme Yöneticisi | When scheduled / clicked there | Plesk → Yedekleme Yöneticisi → Geri yükle |

The `.bacpac` is a zip of the schema plus every row of every table, including users' personal data.
Download it only to your own machine and delete local copies when you are done.

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

## After any restore — fix the code before the next deploy

The restore also rewinds `__EFMigrationsHistory`, so the migration that caused this is "pending"
again and the next push to `main` would re-apply it. `git revert` the commit that added it (or
replace it with a corrected migration), then push. That deploy takes a fresh backup, applies
whatever is now pending, and removes `app_offline.htm`.
