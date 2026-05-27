This folder contains scripts to safely clean and (optionally) re-test the Challenge service.

Files:
- cleanup-challenge.sql  - T-SQL script that deletes Challenge-related tables. Edit @Confirm = 1 to run.
- cleanup-challenge.ps1  - PowerShell helper to execute the SQL script using sqlcmd or Invoke-Sqlcmd.

Usage:
1. Review cleanup-challenge.sql and confirm it matches your schema.
2. Backup the database before running.
3. Run the PowerShell helper with: 
   .\cleanup-challenge.ps1 -ConnectionString 'Server=...;Database=...;User Id=...;Password=...' -Force

Caution: These scripts are destructive. Only run on a non-production copy unless you intend to delete production data.

Recommended workflow:
1. Make a full backup / snapshot of the Challenge database.
2. Run the cleanup helper in a staging environment to confirm expected rows are removed.
3. Rebuild and deploy the service binary built in this session (Challenge.API) to staging.
4. Use test accounts (zalotech@gmail.com / 123456 as creator, conbothi3@gmail.com / 123456 as participant) to run E2E flows.
5. Verify USER_SKILLS and SKILL_POINT_TRANSACTIONS are created after grading.

If you want, I can also prepare an automated E2E PowerShell test that performs login, create challenge, publish, submit and verify results. Ask for it explicitly if desired.