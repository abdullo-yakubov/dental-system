# Security

Medical records and backups are sensitive. The local login currently uses a shared default access code `111111` seeded in `Persistence/Seed/DbSeeder.cs`; `DentalConstants.MinAccessCodeLength` is 4. This offers no individual accountability. The login handler has attempt tracking and audit entries, but the shared code and low minimum length remain material risks.

Use only synthetic records for demos. Do not place patient data, backup files or local secrets in Git. Before live use, require an installation-specific initial secret and mandatory code change, review role/person identity requirements, verify file authorization, and test encrypted backup restore on a disposable PostgreSQL instance. The restore HTTP route is disabled by default (`BackupSettings:EnableRestore=false`). The underlying `BackupService.RestoreBackupAsync` drops and recreates its configured database; never point a test restore at a working database.
