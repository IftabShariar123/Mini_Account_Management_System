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



-------------Voucher part---------------
use AccountManagement_DB
CREATE TABLE Voucher (
    VoucherId INT IDENTITY(1,1) PRIMARY KEY,
    VoucherDate DATE NOT NULL,
    ReferenceNo NVARCHAR(50),
    VoucherType NVARCHAR(20) NOT NULL
);
go

use AccountManagement_DB
CREATE TABLE VoucherEntry (
    EntryId INT IDENTITY(1,1) PRIMARY KEY,
    VoucherId INT FOREIGN KEY REFERENCES Voucher(VoucherId),
    AccountId INT FOREIGN KEY REFERENCES ChartOfAccounts(AccountId),
    DebitAmount DECIMAL(18,2) DEFAULT 0,
    CreditAmount DECIMAL(18,2) DEFAULT 0
);
go

use AccountManagement_DB
CREATE TYPE VoucherEntryType AS TABLE (
    AccountId INT,
    DebitAmount DECIMAL(18,2),
    CreditAmount DECIMAL(18,2)
);
go

-----------Save or Create----------
CREATE PROCEDURE sp_SaveVoucher
    @VoucherDate DATE,
    @ReferenceNo NVARCHAR(50),
    @VoucherType NVARCHAR(20),
    @Entries VoucherEntryType READONLY
AS
BEGIN
    DECLARE @VoucherId INT;

    INSERT INTO Voucher (VoucherDate, ReferenceNo, VoucherType)
    VALUES (@VoucherDate, @ReferenceNo, @VoucherType);

    SET @VoucherId = SCOPE_IDENTITY();

    INSERT INTO VoucherEntry (VoucherId, AccountId, DebitAmount, CreditAmount)
    SELECT @VoucherId, AccountId, DebitAmount, CreditAmount FROM @Entries;
END


---------List----------
CREATE PROCEDURE sp_GetAllVouchers
AS
BEGIN
    SELECT 
        v.VoucherId,
        v.VoucherDate,
        v.ReferenceNo,
        v.VoucherType,
        SUM(e.DebitAmount) AS TotalDebit,
        SUM(e.CreditAmount) AS TotalCredit
    FROM Voucher v
    INNER JOIN VoucherEntry e ON v.VoucherId = e.VoucherId
    GROUP BY v.VoucherId, v.VoucherDate, v.ReferenceNo, v.VoucherType
    ORDER BY v.VoucherDate DESC
END


----------Details---------
CREATE PROCEDURE sp_GetVoucherDetails
    @VoucherId INT
AS
BEGIN
    SELECT 
        v.VoucherId,
        v.VoucherDate,
        v.ReferenceNo,
        v.VoucherType,
        e.DebitAmount,
        e.CreditAmount,
        a.AccountName
    FROM Voucher v
    INNER JOIN VoucherEntry e ON v.VoucherId = e.VoucherId
    INNER JOIN ChartOfAccounts a ON e.AccountId = a.AccountId
    WHERE v.VoucherId = @VoucherId
END
go

----------------Role & Permission------------
USE AccountManagement_DB
CREATE TABLE RoleModuleAccess (
    Id INT IDENTITY PRIMARY KEY,
    RoleName NVARCHAR(50),
    ModuleName NVARCHAR(100),
    CanCreate BIT DEFAULT 0,
    CanUpdate BIT DEFAULT 0,
    CanDelete BIT DEFAULT 0,
    CanViewList BIT DEFAULT 0,
    CanViewDetails BIT DEFAULT 0,
    CONSTRAINT UQ_RoleModule UNIQUE (RoleName, ModuleName)
);
GO


---------Create---------
CREATE PROCEDURE sp_AssignModuleAccess
    @RoleName NVARCHAR(50),
    @ModuleName NVARCHAR(100),
    @CanCreate BIT = 0,
    @CanUpdate BIT = 0,
    @CanDelete BIT = 0,
    @CanViewList BIT = 0,
    @CanViewDetails BIT = 0
AS
BEGIN
    IF EXISTS (SELECT 1 FROM RoleModuleAccess WHERE RoleName = @RoleName AND ModuleName = @ModuleName)
    BEGIN
        UPDATE RoleModuleAccess
        SET 
            CanCreate = @CanCreate,
            CanUpdate = @CanUpdate,
            CanDelete = @CanDelete,
            CanViewList = @CanViewList,
            CanViewDetails = @CanViewDetails
        WHERE RoleName = @RoleName AND ModuleName = @ModuleName
    END
    ELSE
    BEGIN
        INSERT INTO RoleModuleAccess (RoleName, ModuleName, CanCreate, CanUpdate, CanDelete, CanViewList, CanViewDetails)
        VALUES (@RoleName, @ModuleName, @CanCreate, @CanUpdate, @CanDelete, @CanViewList, @CanViewDetails)
    END
END
GO

CREATE PROCEDURE sp_HasAccess
    @RoleName NVARCHAR(50),
    @ModuleName NVARCHAR(100)
AS
BEGIN
    -- Check if record exists
    IF EXISTS (SELECT 1 FROM RoleModuleAccess 
               WHERE RoleName = @RoleName AND ModuleName = @ModuleName)
    BEGIN
        -- Return existing permissions
        SELECT 
            CanCreate,
            CanUpdate,
            CanDelete,
            CanViewList,
            CanViewDetails
        FROM RoleModuleAccess
        WHERE RoleName = @RoleName AND ModuleName = @ModuleName
    END
    ELSE
    BEGIN
        -- Return all false values if no record exists
        SELECT 
            0 AS CanCreate,
            0 AS CanUpdate,
            0 AS CanDelete,
            0 AS CanViewList,
            0 AS CanViewDetails
    END
END