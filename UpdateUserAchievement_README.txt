========================================================================
VBScript: Update User Achievement Data - README
========================================================================

PURPOSE:
--------
This VBScript replicates the C# logic from HomeController.cs to calculate
and update the UserAchieved table for all users in the system. It can be
run on-demand without requiring users to log in.

WHAT IT DOES:
-------------
For each user in the Users table, the script:
1. Retrieves the user's assigned deposit target
2. Calculates raw deposit amount (sum of all deposits by user)
3. For each unique account the user contributed to:
   - Gets the true initial balance (Prev_Ini_Bal from earliest record)
   - Gets current balance from AccountReserve table
   - Calculates total deposits to that account (all users)
   - Calculates user's contribution to that account
   - Determines available fund (current - initial, capped at total deposits)
   - Calculates user's proportional achievement share
4. Saves/updates the UserAchieved table with:
   - UserTarget
   - CollectedAmount (raw deposits)
   - AchievedAmount (calculated achievement)
   - AchievedPercent
   - UpdatedDate

BEFORE RUNNING:
---------------
1. IMPORTANT: Update the database connection string in the script
   
   Open UpdateUserAchievement.vbs and find this line:
   
   Const DB_CONNECTION_STRING = "Provider=SQLOLEDB;Data Source=YOUR_SERVER;Initial Catalog=TRMS;Integrated Security=SSPI;"
   
   Replace YOUR_SERVER with your actual SQL Server name, for example:
   
   Const DB_CONNECTION_STRING = "Provider=SQLOLEDB;Data Source=localhost\SQLEXPRESS;Initial Catalog=TRMS;Integrated Security=SSPI;"
   
   OR if using SQL authentication:
   
   Const DB_CONNECTION_STRING = "Provider=SQLOLEDB;Data Source=YOUR_SERVER;Initial Catalog=TRMS;User ID=sa;Password=yourpassword;"

2. Ensure you have permissions to:
   - Read from: Users, DepositPlans, AccountReserve tables
   - Write to: UserAchieved table

3. Make a backup of the UserAchieved table before first run (recommended)

HOW TO RUN:
-----------
Method 1: Double-click the UpdateUserAchievement.vbs file
Method 2: Open Command Prompt and run:
          cscript UpdateUserAchievement.vbs

Method 2 (using cscript) is recommended as it shows progress in the console.

WHAT YOU'LL SEE:
----------------
The script will display:
- Connection status
- Each user being processed
- Whether a record was inserted or updated
- Target, Collected, Achieved amounts and percentage for each user
- Completion message

EXAMPLE OUTPUT:
---------------
Connected to database successfully.
Starting User Achievement calculation...
------------------------------------------------------------
Processing user: habtamugg
  -> Updated record: Target=1000000, Collected=450000, Achieved=425000, Percent=42.5%
Processing user: johnsmith
  -> Inserted new record: Target=800000, Collected=320000, Achieved=310000, Percent=38.75%
------------------------------------------------------------
User Achievement calculation completed successfully!

WHEN TO RUN:
------------
- Daily (can be scheduled using Windows Task Scheduler)
- After bulk deposit imports
- When you need to refresh achievement data for all users
- Before generating reports

SCHEDULING (Optional):
----------------------
To run this automatically every day:
1. Open Windows Task Scheduler
2. Create a new Basic Task
3. Set trigger (e.g., Daily at 11:00 PM)
4. Action: Start a program
5. Program: cscript.exe
6. Arguments: "C:\Users\Hmariam K\source\repos\TRMS\UpdateUserAchievement.vbs"
7. Save and test

TROUBLESHOOTING:
----------------
Error: "Provider cannot be found"
Solution: Install SQL Server Native Client or use different provider

Error: "Login failed"
Solution: Check connection string, server name, and credentials

Error: "Invalid object name 'UserAchieved'"
Solution: Verify table exists and spelling is correct

Script runs but no updates:
Solution: Check if Users table has data and UserName field is populated

DIFFERENCES FROM C# VERSION:
----------------------------
- Processes ALL users (not just logged-in user)
- Runs independently (no web session required)
- Console output instead of ViewBag
- No breakdown list generation (only final totals)
- Simplified error handling

NOTES:
------
- The script uses the same calculation logic as HomeController.cs
- It preserves existing values if calculation fails
- UpdatedDate is set to current date/time on each run
- Script is safe to run multiple times (idempotent)

========================================================================
