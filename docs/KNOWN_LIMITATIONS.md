# Known limitations

- The server-only build passes, but the complete Windows solution and installer have not been built in this sandbox.
- Full Windows solution restore is blocked in the current sandbox by registry access; launcher and installer are unverified.
- Default shared access code and 4-character minimum are unsuitable for real patient records.
- The checked-in connection string contains `CHANGE_ME`; secure first-run database credentials are not established.
- Backup and restore have not been tested on an isolated copy. Restore code drops the configured database.
- Restore HTTP access is disabled by default until an isolated restore test succeeds.
- UI, 3D performance, mobile/accessibility, and screenshots have not been verified in a running app.
- `CLAUDE.md` refers to architecture files absent from this checkout.
