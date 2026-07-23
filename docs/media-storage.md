# Media storage

Patient documents, user avatars, and clinic logos use `IFileStorageService`. In Development the configured default is `.data/media`, resolved from the API content root. Testing uses the OS temporary directory. Production refuses to start when `FileStorage:RootPath` is empty, points inside the publish directory, or is not writable. Set it to a persistent mounted volume shared by every API instance and include it in backups.

Images are decoded (not trusted by extension), resized, stripped of incidental metadata, and compressed. Avatars/logos become WebP (maximum 1200px, quality 78); patient JPEG/PNG images are capped at 2400px. PDF, DOCX, and RTF files are signature/container validated. The per-file limit is 10 MiB for documents and 8 MiB for profile media.

Patient images also receive a separate 320px WebP thumbnail. The document table calls the thumbnail endpoint and never downloads the original image for its grid. Animated images and images above 40 million decoded pixels are rejected. DOCX archives are capped at 2,000 entries, 100 MiB uncompressed, and a 200:1 per-entry compression ratio.

Files are private and only streamed through authorized endpoints. Responses support range processing where applicable. PDF documents for prescriptions, receipts, and invoices are generated on demand to avoid duplicate stored copies.

Protected branding endpoints are `GET|POST|DELETE /api/media/me/avatar` and `GET|POST|DELETE /api/media/tenant/logo`. Avatar operations always resolve the authenticated user; tenant logo operations always resolve the authenticated tenant, and logo mutation requires Admin plus `settings.manage`.

DOCX and RTF are download-only (`Content-Disposition: attachment`). PDF and images may be inline. All responses carry `X-Content-Type-Options: nosniff`. Replacements save the new object, update the database, then stage/delete the old object. Document deletion stages the file, deletes the database row, and restores the file if the database operation fails. Stale staged deletions are swept after one hour.

For multi-server deployments, do not use instance-local ephemeral storage. Point `FileStorage:RootPath` to durable shared storage or replace the registered `IFileStorageService` with an object-storage adapter.

Production PDF rendering also requires `Pdf:FontPath` to point to a deployed, licensed Arabic Unicode TTF/OTF font. Development falls back to Arial only for local convenience; Production will not start without an explicit font.

Legacy `uploads/...` keys are read only from the historical `AppContext.BaseDirectory/uploads` root with the same full-path containment check. New writes never use that location. Migrate legacy files by copying each tenant subtree to the durable root, rewriting `PatientDocuments.FileUrl` from `uploads/...` to `storage/...` in a transaction, verifying file counts/checksums, and only then removing the old tree. The fallback should be removed after migration verification.

Apply `database/media-hardening-migration.sql` to the ClinicFlow application database after a backup. It reports and stops on invalid legacy `DocumentType` rows; correct those rows and rerun. The script is idempotent once the constraint is trusted.
