-- Add time range metadata to application_log_chunks for intelligent chunk selection.
IF COL_LENGTH('dbo.application_log_chunks', 'time_start') IS NULL
BEGIN
    ALTER TABLE dbo.application_log_chunks
        ADD time_start DATETIME2 NULL;
END
GO

IF COL_LENGTH('dbo.application_log_chunks', 'time_end') IS NULL
BEGIN
    ALTER TABLE dbo.application_log_chunks
        ADD time_end DATETIME2 NULL;
END
GO

IF COL_LENGTH('dbo.application_log_chunks', 'entry_count') IS NULL
BEGIN
    ALTER TABLE dbo.application_log_chunks
        ADD entry_count INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_application_log_chunks_time_range'
      AND object_id = OBJECT_ID(N'dbo.application_log_chunks'))
BEGIN
    CREATE INDEX IX_application_log_chunks_time_range
        ON dbo.application_log_chunks (application_log_id, time_start, time_end);
END
GO
