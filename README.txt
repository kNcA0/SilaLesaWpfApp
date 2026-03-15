SilaLesaWpfApp (WPF .NET Framework)

1) Create database
   - Run SilaLesaWpfApp\Model\CampingBookingDB.sql in SSMS.
   - Database name: CampingBookingDB

2) Configure connection string
   - Open SilaLesaWpfApp\App.config
   - Change Data Source to your SQL Server instance:
     Examples:
       .\SQLEXPRESS
       (localdb)\MSSQLLocalDB
       localhost

3) Run
   - Open SilaLesaWpfApp.sln in Visual Studio (2019/2022)
   - Build + Start

Demo accounts (from SQL script):
  admin / hash_admin_123
  moderator / hash_moderator_123
  user / hash_user_123

Roles:
  - admin: manage roles/users + view/add customers/sites/services + update booking status
  - moderator: view data + update booking status (no access to roles/users)
  - user: choose customer, search free sites, add extra services, create booking, cancel booking
