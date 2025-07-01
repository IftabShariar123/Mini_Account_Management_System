# Mini Account Management System – Voucher & Chart of Accounts

This is a demo accounting module built as part of a technical assignment for Qtec Solution Ltd. It demonstrates a simplified financial accounting system using:

- ASP.NET Core Razor Pages
- MS SQL Server with Stored Procedures (no LINQ)
- ASP.NET Identity (Admin, Accountant, Viewer)
- ADO.NET for all data operations
- Dynamic voucher entry with account linking

---

## ✅ Features

### 1. User Role & Permission Management
- Roles: **Admin**, **Accountant**, **Viewer**
- Role-based access control via ASP.NET Identity
- Stored procedure-based access rights to modules
- Admin UI to assign modules to roles

### 2. Chart of Accounts
- Parent/Child account tree support
- Create, Edit, Delete accounts
- Stored procedure: sp_ManageChartOfAccounts
- Dropdown for parent accounts

### 3. Voucher Entry Module
- Voucher Types: Journal, Payment, Receipt
- Dynamic line-item entry (Debit/Credit) with Account dropdown
- Stored procedure: sp_SaveVoucher
- Table-valued parameter: VoucherEntryType
- List view + total Debit/Credit + details page

---

## 🗃️ Database Design

### Tables:
- ChartOfAccounts(AccountId, AccountName, ParentId, AccountType)
- Voucher(VoucherId, VoucherDate, ReferenceNo, VoucherType)
- VoucherEntry(EntryId, VoucherId, AccountId, DebitAmount, CreditAmount)
- RoleModuleAccess(RoleName, ModuleName)

### Stored Procedures:
- sp_ManageChartOfAccounts
- sp_SaveVoucher
- sp_GetAllVouchers
- sp_GetVoucherDetails
- sp_AssignModuleAccess
- sp_HasAccess

### User-defined Table Types:
- VoucherEntryType

---

## 🛠 Technologies Used

| Technology       | Purpose                          |
|------------------|----------------------------------|
| ASP.NET Core     | Razor Pages Web UI               |
| MS SQL Server    | Backend Data Storage             |
| Stored Procedures| All DB Operations (No LINQ)      |
| ADO.NET          | SQL Connection + Execution       |
| Bootstrap        | UI Styling                       |
| Identity         | User & Role Management           |

---------------------------------------------------------------
Guideline-- how to use this software>>

Step-1>> Firstly Create SQL Server DB and run the SQL scripts for:Tables , Stored Procedures

Step-2>> Open this project in Visual Studio....go to appsetings.json give your server name in connectionstring
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=MiniAccountDb;Trusted_Connection=True;"
}


Step-3>>  do migration for some seed value then run the project.

Step-4>> Do login >> Username: Shariar@gmail.com
                    Password: Shariar@123 ( this is by default Admin user)
        without Admin can't manage role management

Step-5>> After login you can Create, Edit, Delete Accounts from ChartOfAccounts and also Vouchers
        Without giving role permission can't process Accounts and Vouchers

All Screenshots included in images folder which stay in project root

----------------------------------------------------

        ## 🖼️ Screenshots

![Home Screenshot](images/1.png)

![Register Screenshot](images/2.png)

![Login Screenshot](images/3.png)


![Role & Permission management Screenshot](images/4.png)

![List of ChartofAccount Screenshot](images/5.png)

![Create of ChartOfAccount Screenshot](images/6.png)

![Edit of ChartOfAccount Screenshot](images/7.png)

![Delete of ChartOfAccount Screenshot](images/8.png)

![List of Voucher Screenshot](images/9.png)

![Creat of Voucher Screenshot](images/10.png)

![Details of Voucher Screenshot](images/11.png)





