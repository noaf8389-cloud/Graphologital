-- יצירת Login ומשתמש למסד GraphologitalDB

USE master;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.sql_logins
    WHERE name = 'graphologital_user'
)
BEGIN
    CREATE LOGIN graphologital_user
    WITH PASSWORD = 'GraphologitalDB_2026!';
END
GO

USE GraphologitalDB;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_principals
    WHERE name = 'graphologital_user'
)
BEGIN
    CREATE USER graphologital_user
    FOR LOGIN graphologital_user;
END
GO

ALTER ROLE db_datareader ADD MEMBER graphologital_user;
ALTER ROLE db_datawriter ADD MEMBER graphologital_user;
GO

PRINT 'graphologital_user permissions created successfully.';
GO