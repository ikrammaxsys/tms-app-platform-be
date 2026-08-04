/*
  Create Organizations table for platform tenancy / grouping.
  Run against existing Toray_App_Platform databases.
*/
USE Toray_App_Platform;
GO

IF OBJECT_ID(N'dbo.Organizations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Organizations
    (
        id                  INT             NOT NULL IDENTITY(1,1),
        code                NVARCHAR(50)    NOT NULL,
        name                NVARCHAR(150)   NOT NULL,
        CONSTRAINT PK_Organizations PRIMARY KEY CLUSTERED (id),
        CONSTRAINT UQ_Organizations_Code UNIQUE (code),
        CONSTRAINT UQ_Organizations_Name UNIQUE (name)
    );
END
GO
