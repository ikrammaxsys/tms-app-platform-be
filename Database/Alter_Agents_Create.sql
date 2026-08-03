/*
  Create Agents table for platform monitoring agents.
  Run against existing Toray_App_Platform databases.
*/
USE Toray_App_Platform;
GO

IF OBJECT_ID(N'dbo.Agents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Agents
    (
        id                  INT             NOT NULL IDENTITY(1,1),
        uid                 NVARCHAR(64)    NOT NULL,
        name                NVARCHAR(150)   NOT NULL,
        server_id           INT             NOT NULL,
        auth_token          NVARCHAR(128)   NOT NULL,
        status              NVARCHAR(50)    NOT NULL,
        last_ready_at       DATETIME2(0)    NULL,
        created_at          DATETIME2(0)    NOT NULL,
        config_json         NVARCHAR(MAX)   NULL
    );
END
GO
