use master 
create database AccountManagement_DB
go

use AccountManagement_DB
CREATE TABLE ChartOfAccounts (
    AccountId INT PRIMARY KEY IDENTITY(1,1),
    AccountName NVARCHAR(100) NOT NULL,
    ParentAccountId INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    Remarks NVARCHAR(255)
);
go


CREATE PROCEDURE sp_ManageChartOfAccounts
    @Action NVARCHAR(10),
    @AccountId INT = NULL,
    @AccountName NVARCHAR(100) = NULL,
    @ParentAccountId INT = NULL,
    @IsActive BIT = 1,
    @Remarks NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = 'CREATE'
    BEGIN
        INSERT INTO ChartOfAccounts (AccountName, ParentAccountId, IsActive, Remarks)
        VALUES (@AccountName, @ParentAccountId, @IsActive, @Remarks)
    END
    ELSE IF @Action = 'UPDATE'
    BEGIN
        UPDATE ChartOfAccounts
        SET AccountName = @AccountName,
            ParentAccountId = @ParentAccountId,
            IsActive = @IsActive,
            Remarks = @Remarks
        WHERE AccountId = @AccountId
    END
    ELSE IF @Action = 'DELETE'
    BEGIN
        DELETE FROM ChartOfAccounts WHERE AccountId = @AccountId
    END
END
go

CREATE VIEW vw_ChartOfAccountTree AS
WITH AccountTree AS (
    SELECT AccountId, AccountName, ParentAccountId,
           CAST(AccountName AS NVARCHAR(MAX)) AS HierarchyPath
    FROM ChartOfAccounts
    WHERE ParentAccountId IS NULL

    UNION ALL

    SELECT c.AccountId, c.AccountName, c.ParentAccountId,
           CAST(p.HierarchyPath + ' > ' + c.AccountName AS NVARCHAR(MAX))
    FROM ChartOfAccounts c
    INNER JOIN AccountTree p ON c.ParentAccountId = p.AccountId
)
SELECT * FROM AccountTree;
