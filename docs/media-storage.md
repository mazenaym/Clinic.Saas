# Media storage

Patient documents, user avatars, and clinic logos use `IFileStorageService`. The default local implementation writes under `FileStorage:RootPath`; when empty it uses the API `storage` directory. In production, set this to a persistent mounted volume shared by every API instance and include it in backups.

Images are decoded (not trusted by extension), resized, stripped of incidental metadata, and compressed. Avatars/logos become WebP (maximum 1200px, quality 78); patient JPEG/PNG images are capped at 2400px. PDF, DOCX, and RTF files are signature/container validated. The per-file limit is 10 MiB for documents and 8 MiB for profile media.

Files are private and only streamed through authorized endpoints. Responses support range processing where applicable. PDF documents for prescriptions, receipts, and invoices are generated on demand to avoid duplicate stored copies.

For multi-server deployments, do not use instance-local ephemeral storage. Point `FileStorage:RootPath` to durable shared storage or replace the registered `IFileStorageService` with an object-storage adapter.
