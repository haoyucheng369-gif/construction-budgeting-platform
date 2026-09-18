IF DB_ID(N'Budget') IS NULL
    CREATE DATABASE [Budget];
GO
IF SUSER_ID(N'budget_reader') IS NULL
    CREATE LOGIN [budget_reader] WITH PASSWORD = '$(BudgetReaderPassword)', CHECK_POLICY = ON;
GO
USE [Budget];
GO
IF SCHEMA_ID(N'budget') IS NULL
    EXEC(N'CREATE SCHEMA [budget] AUTHORIZATION [dbo]');
GO
IF OBJECT_ID(N'budget.ProjectBaselines', N'U') IS NULL
BEGIN
    CREATE TABLE budget.ProjectBaselines (
        ProjectId uniqueidentifier NOT NULL PRIMARY KEY,
        Amount decimal(18, 2) NOT NULL CHECK (Amount >= 0),
        Currency char(3) NOT NULL
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM budget.ProjectBaselines WHERE ProjectId = '11111111-1111-1111-1111-111111111111')
    INSERT INTO budget.ProjectBaselines (ProjectId, Amount, Currency)
    VALUES ('11111111-1111-1111-1111-111111111111', 1300.00, 'EUR');
GO
IF USER_ID(N'budget_reader') IS NULL
    CREATE USER [budget_reader] FOR LOGIN [budget_reader];
GRANT SELECT ON SCHEMA::budget TO [budget_reader];
DENY INSERT, UPDATE, DELETE ON SCHEMA::budget TO [budget_reader];
DENY CREATE TABLE TO [budget_reader];
GO
