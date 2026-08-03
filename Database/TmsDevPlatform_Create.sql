/*
================================================================================
  TMS Dev Platform - Database schema (SQL Server)
  ERD: Application_Groups / Servers -> Applications -> application_uptime_logs
================================================================================
*/
USE Toray_App_Platform;
GO
/* Drop tables if present (dev recreate) */
IF OBJECT_ID(N'dbo.application_uptime_logs', N'U') IS NOT NULL DROP TABLE dbo.application_uptime_logs;
IF OBJECT_ID(N'dbo.Application_deployments', N'U') IS NOT NULL DROP TABLE dbo.Application_deployments;
IF OBJECT_ID(N'dbo.Applications', N'U') IS NOT NULL DROP TABLE dbo.Applications;
IF OBJECT_ID(N'dbo.Agents', N'U') IS NOT NULL DROP TABLE dbo.Agents;
IF OBJECT_ID(N'dbo.Application_Groups', N'U') IS NOT NULL DROP TABLE dbo.Application_Groups;
IF OBJECT_ID(N'dbo.Servers', N'U') IS NOT NULL DROP TABLE dbo.Servers;
GO
/* -------------------------------------------------------------------------- */
/*  Application_Groups                                                        */
/* -------------------------------------------------------------------------- */
CREATE TABLE dbo.Application_Groups
(
    id                  INT             NOT NULL IDENTITY(1,1),
    name                NVARCHAR(150)   NOT NULL,
    CONSTRAINT PK_Application_Groups PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_Application_Groups_Name UNIQUE (name)
);
GO
/* -------------------------------------------------------------------------- */
/*  Servers                                                                   */
/* -------------------------------------------------------------------------- */
CREATE TABLE dbo.Servers
(
    id                  INT             NOT NULL IDENTITY(1,1),
    ip_address          NVARCHAR(100)   NOT NULL,
    environment         NVARCHAR(50)    NOT NULL CONSTRAINT DF_Servers_ENV DEFAULT (N'Live'),
    internal_external   NVARCHAR(20)    NOT NULL CONSTRAINT DF_Servers_IE DEFAULT (N'Internal'),
    country             NVARCHAR(100)   NULL,
    provider            NVARCHAR(100)   NULL,
    domain              NVARCHAR(255)   NOT NULL,
    CONSTRAINT PK_Servers PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_Servers_Domain UNIQUE (domain),
    CONSTRAINT CK_Servers_ENV CHECK (environment IN (N'Live', N'Test', N'Development')),
    CONSTRAINT CK_Servers_IE CHECK (internal_external IN (N'Internal', N'External'))
);
GO
CREATE NONCLUSTERED INDEX IX_Servers_ENV
    ON dbo.Servers (environment);
GO
/* -------------------------------------------------------------------------- */
/*  Applications                                                              */
/* -------------------------------------------------------------------------- */
CREATE TABLE dbo.Applications
(
    id                      INT             NOT NULL IDENTITY(1,1),
    uid                     NVARCHAR(64)    NOT NULL,
    name                    NVARCHAR(150)   NOT NULL,
    version                 NVARCHAR(50)    NOT NULL,
    commit_id               NVARCHAR(64)    NULL,
    status                  NVARCHAR(50)    NOT NULL CONSTRAINT DF_Apps_STATUS DEFAULT (N'Healthy'),
    last_deployment         DATETIME2(0)    NULL,
    app_url                 NVARCHAR(500)   NULL,
    repository_url          NVARCHAR(500)   NULL,
    id_server               INT             NOT NULL,
    id_application_group    INT             NOT NULL,
    healthcheck_url         VARCHAR(200)    NULL,
    is_healthcheck          INT             NOT NULL CONSTRAINT DF_Apps_IsHealthcheck DEFAULT (0),
    logs_path               VARCHAR(200)    NULL,
    is_scaning_logs         INT             NOT NULL CONSTRAINT DF_Apps_IsScaningLogs DEFAULT (0),
    CONSTRAINT PK_Applications PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_Applications_Uid UNIQUE (uid),
    CONSTRAINT FK_Applications_Server FOREIGN KEY (id_server)
        REFERENCES dbo.Servers (id),
    CONSTRAINT FK_Applications_Group FOREIGN KEY (id_application_group)
        REFERENCES dbo.Application_Groups (id),
    CONSTRAINT CK_Apps_STATUS CHECK (status IN (N'Healthy', N'Warning', N'Down', N'Inactive'))
);
GO
CREATE NONCLUSTERED INDEX IX_Applications_Server
    ON dbo.Applications (id_server);
GO
CREATE NONCLUSTERED INDEX IX_Applications_Group
    ON dbo.Applications (id_application_group);
GO
CREATE NONCLUSTERED INDEX IX_Applications_Status
    ON dbo.Applications (status);
GO
/* -------------------------------------------------------------------------- */
/*  Agents                                                                    */
/* -------------------------------------------------------------------------- */
CREATE TABLE dbo.agents
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
GO

/* -------------------------------------------------------------------------- */
/*  application_uptime_logs                                                   */
/* -------------------------------------------------------------------------- */
CREATE TABLE dbo.application_uptime_logs
(
    id                  INT             NOT NULL IDENTITY(1,1),
    id_application      INT             NOT NULL,
    latency             INT             NULL,          -- milliseconds
    status              NVARCHAR(50)    NOT NULL,
    [timestamp]         DATETIME2(0)    NOT NULL CONSTRAINT DF_Uptime_TS DEFAULT (DATEADD(HOUR, 8, SYSUTCDATETIME())),
    CONSTRAINT PK_application_uptime_logs PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_Uptime_Application FOREIGN KEY (id_application)
        REFERENCES dbo.Applications (id),
    CONSTRAINT CK_Uptime_STATUS CHECK (status IN (N'Up', N'Down', N'Degraded'))
);
GO
CREATE NONCLUSTERED INDEX IX_Uptime_App_Timestamp
    ON dbo.application_uptime_logs (id_application, [timestamp] DESC);
GO
/* -------------------------------------------------------------------------- */
/*  Application_deployments                                                   */
/* -------------------------------------------------------------------------- */
CREATE TABLE dbo.Application_deployments
(
    id                  INT             NOT NULL IDENTITY(1,1),
    application_id      INT             NULL,
    commit_no           VARCHAR(100)    NULL,
    version             VARCHAR(100)    NULL,
    [timestamp]         VARCHAR(100)    NULL,
    CONSTRAINT Application_deployments_PK PRIMARY KEY CLUSTERED (id)
);
GO
CREATE NONCLUSTERED INDEX IX_Application_deployments_App
    ON dbo.Application_deployments (application_id);
GO
/* -------------------------------------------------------------------------- */
/*  Seed data                                                                 */
/* -------------------------------------------------------------------------- */
SET IDENTITY_INSERT dbo.Application_Groups ON;
INSERT INTO dbo.Application_Groups (id, name)
VALUES
    (1, N'ACL Stack'),
    (2, N'Vendor'),
    (3, N'Sales'),
    (4, N'Infra');
SET IDENTITY_INSERT dbo.Application_Groups OFF;
GO
SET IDENTITY_INSERT dbo.Servers ON;
INSERT INTO dbo.Servers (id, ip_address, environment, internal_external, country, provider, domain)
VALUES
    (1, N'10.188.9.124',  N'Live',        N'External', N'JP', N'Vendor',     N'ihrs-vendor.toray'),
    (2, N'10.188.9.136',  N'Live',        N'External', N'JP', N'AWS',        N'tms-eapp.webapp.toray'),
    (3, N'10.188.9.159',  N'Live',        N'Internal', N'JP', N'AWS',        N'tms-iapp.intr.webapp.toray'),
    (4, N'10.188.9.198',  N'Test',        N'External', N'JP', N'AWS',        N'tms-sales.webapp.toray'),
    (5, N'10.188.9.146',  N'Test',        N'Internal', N'JP', N'AWS',        N'tms-internal-test.webapp.toray'),
    (6, N'10.200.0.81',   N'Test',        N'Internal', N'MY', N'On-premise', N'onprem-test.toray.local'),
    (7, N'10.230.8.170',  N'Development', N'Internal', N'MY', N'AWS',        N'tms-dev.webapp.toray');
SET IDENTITY_INSERT dbo.Servers OFF;
GO
SET IDENTITY_INSERT dbo.Applications ON;
INSERT INTO dbo.Applications
    (id, uid, name, version, commit_id, status, last_deployment, app_url, repository_url, id_server, id_application_group)
VALUES
    (1,  N'app-auth-api-eapp',       N'Auth API',          N'v3.2.0',     N'tms0001', N'Healthy', '2026-07-20T09:00:00', N'https://tms-eapp.webapp.toray/auth-api',          N'https://github.com/tms-dev-platform/auth-api',          2, 1),
    (2,  N'app-dsp-eapp',            N'DSP',               N'v5.1.0',     N'tms0002', N'Healthy', '2026-07-18T10:00:00', N'https://tms-eapp.webapp.toray/dsp',               N'https://github.com/tms-dev-platform/dsp',               2, 1),
    (3,  N'app-ui-foundation-eapp',  N'UI Foundation',     N'v2.8.1',     N'tms0003', N'Healthy', '2026-07-15T11:00:00', N'https://tms-eapp.webapp.toray/UiFoundation',      N'https://github.com/tms-dev-platform/ui-foundation',     2, 1),
    (4,  N'app-core-api-eapp',       N'Core API',          N'v4.0.3',     N'tms0004', N'Warning', '2026-07-22T08:40:00', N'https://tms-eapp.webapp.toray/core-api',          N'https://github.com/tms-dev-platform/core-api',          2, 1),
    (5,  N'app-ihrs',                N'IHRS',              N'v1.4.0',     N'tms0005', N'Healthy', '2026-06-01T08:00:00', N'http://10.188.9.124/ihrs',                        N'https://github.com/tms-dev-platform/ihrs',              1, 2),
    (6,  N'app-auth-api-sales',      N'Auth API',          N'v3.2.0',     N'tms0006', N'Healthy', '2026-07-21T15:00:00', N'https://tms-sales.webapp.toray/auth-api',         N'https://github.com/tms-dev-platform/auth-api',          4, 1),
    (7,  N'app-core-api-sales',      N'Core API',          N'v4.0.1',     N'tms0009', N'Warning', '2026-07-23T09:00:00', N'https://tms-sales.webapp.toray/core-api',         N'https://github.com/tms-dev-platform/core-api',          4, 1),
    (8,  N'app-tas-sales',           N'TAS',               N'v2.1.0',     N'tms0010', N'Healthy', '2026-07-10T12:00:00', N'https://tms-sales.webapp.toray/tas',              N'https://github.com/tms-dev-platform/tas',               4, 3),
    (9,  N'app-vcs-sales',           N'VCS',               N'v1.9.5',     N'tms0011', N'Healthy', '2026-07-12T13:00:00', N'https://tms-sales.webapp.toray/vcs',              N'https://github.com/tms-dev-platform/vcs',               4, 3),
    (10, N'app-aws-migration',       N'AWS Migration App', N'v0.9.0',     N'tms0012', N'Warning', '2026-07-24T07:00:00', N'http://10.188.9.146/migration',                   N'https://github.com/tms-dev-platform/aws-migration-app', 5, 4),
    (11, N'app-auth-api-dev',        N'Auth API',          N'v3.3.0-dev', N'tms0013', N'Healthy', '2026-07-26T16:00:00', N'http://10.230.8.170/auth-api',                    N'https://github.com/tms-dev-platform/auth-api',          7, 1),
    (12, N'app-core-api-dev',        N'Core API',          N'v4.1.0-dev', N'tms0016', N'Warning', '2026-07-27T09:00:00', N'http://10.230.8.170/core-api',                    N'https://github.com/tms-dev-platform/core-api',          7, 1);
SET IDENTITY_INSERT dbo.Applications OFF;
GO
/* Seed ~30 days of daily uptime samples for each application */
DECLARE @end DATE = '2026-07-27';
DECLARE @appId INT;
DECLARE @appStatus NVARCHAR(50);
DECLARE @d INT;
DECLARE @day DATE;
DECLARE @latency INT;
DECLARE app_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT id, status FROM dbo.Applications;
OPEN app_cursor;
FETCH NEXT FROM app_cursor INTO @appId, @appStatus;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @d = 29;
    WHILE @d >= 0
    BEGIN
        SET @day = DATEADD(DAY, -@d, @end);
        SET @latency = 40 + ((@appId * 7 + @d) % 80);
        IF @appStatus = N'Down' AND @d IN (0, 1, 2, 10, 18)
            INSERT INTO dbo.application_uptime_logs (id_application, latency, status, [timestamp])
            VALUES (@appId, @latency + 500, N'Down', CAST(@day AS DATETIME2(0)));
        ELSE IF @appStatus = N'Warning' AND @d IN (3, 11, 15, 19)
            INSERT INTO dbo.application_uptime_logs (id_application, latency, status, [timestamp])
            VALUES (@appId, @latency + 200, CASE WHEN @d = 15 THEN N'Degraded' ELSE N'Down' END, CAST(@day AS DATETIME2(0)));
        ELSE IF @appStatus = N'Healthy' AND @d IN (8, 22)
            INSERT INTO dbo.application_uptime_logs (id_application, latency, status, [timestamp])
            VALUES (@appId, @latency + 300, N'Down', CAST(@day AS DATETIME2(0)));
        ELSE
            INSERT INTO dbo.application_uptime_logs (id_application, latency, status, [timestamp])
            VALUES (@appId, @latency, N'Up', CAST(@day AS DATETIME2(0)));
        SET @d = @d - 1;
    END
    FETCH NEXT FROM app_cursor INTO @appId, @appStatus;
END
CLOSE app_cursor;
DEALLOCATE app_cursor;
GO
PRINT N'TMS Dev Platform ERD schema created successfully.';
GO
