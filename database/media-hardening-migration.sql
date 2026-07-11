/* Media hardening is intentionally backwards compatible: existing URLs and rows remain valid. */
IF OBJECT_ID(N'dbo.PatientDocuments', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_PatientDocuments_DocumentType')
        ALTER TABLE dbo.PatientDocuments WITH NOCHECK
        ADD CONSTRAINT CK_PatientDocuments_DocumentType CHECK (DocumentType BETWEEN 1 AND 6);

    ALTER TABLE dbo.PatientDocuments CHECK CONSTRAINT CK_PatientDocuments_DocumentType;
END;

/* PdfUrl and ReceiptUrl are retained for API/database compatibility.
   New PDFs are generated on demand to avoid duplicated server storage. */
