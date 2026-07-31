-- Add remote_name to application_log_chunks for Core API stored file identity.
IF COL_LENGTH('dbo.application_log_chunks', 'remote_name') IS NULL
BEGIN
    ALTER TABLE dbo.application_log_chunks
        ADD remote_name VARCHAR(100) NOT NULL CONSTRAINT DF_application_log_chunks_remote_name DEFAULT ('');
END
GO
