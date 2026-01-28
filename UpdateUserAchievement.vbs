' ========================================================================
' VBScript: Update User Achievement Data
' Purpose: Calculate and update UserAchieved table for all users
' ========================================================================

Option Explicit


' Database connection string
Const DB_CONNECTION_STRING = "Provider=SQLOLEDB;Data Source=ERP-LP-09\SQLEXPRESS;Initial Catalog=TRMS;Integrated Security=SSPI;"
Const LOG_FILE_NAME = "UpdateUserAchievement.log"

Dim conn, rsUsers, rsDeposits, rsAccounts, rsReserves, cmd
Dim fso, logFile
Dim userName, userID, process, district, branch, position
Dim assignedTarget, rawUserDeposit, totalUserAchieved
Dim accountNumber, trueInitial, currentBalance, totalDeposits, userContribution
Dim availableFund, userAchieved, newAchievedPercent
Dim sql, recordCount

' Initialize Logging
Set fso = CreateObject("Scripting.FileSystemObject")
' Open log file for appending (8), create if not exists (True)
Set logFile = fso.OpenTextFile(LOG_FILE_NAME, 8, True)

LogMessage "============================================================"
LogMessage "Script Started"
LogMessage "============================================================"

' Create connection object
Set conn = CreateObject("ADODB.Connection")
conn.Open DB_CONNECTION_STRING

LogMessage "Connected to database successfully."
LogMessage "Starting User Achievement calculation..."
LogMessage String(60, "-")

recordCount = 0

' Get all users from the Users table
Set rsUsers = CreateObject("ADODB.Recordset")

sql = "SELECT UserName, ID, Process, District, Branch, Postion, " & _
      "ISNULL(DepositTargetAmount, 0) AS DepositTargetAmount " & _
      "FROM Users " & _
      "WHERE UserName IN (" & _
      "'hailemariamk', 'mohammedaka'"& _
      ")"
rsUsers.Open sql, conn

' Loop through each user
Do While Not rsUsers.EOF
    userName = rsUsers("UserName")
    userID = rsUsers("ID")
    process = Nz(rsUsers("Process"), "")
    district = Nz(rsUsers("District"), "")
    branch = Nz(rsUsers("Branch"), "")
    position = Nz(rsUsers("Postion"), "")
    assignedTarget = CDbl(rsUsers("DepositTargetAmount"))
    
    LogMessage "Processing user: " & userName
    
    ' Get all user's deposit rows
    Set rsDeposits = CreateObject("ADODB.Recordset")
    sql = "SELECT AccountNumber, Amount FROM DepositPlan WHERE [User] = '" & SQLEscape(userName) & "'"
    rsDeposits.Open sql, conn
    
    ' Calculate raw user deposit and collect unique accounts
    rawUserDeposit = 0
    Dim accountsList, accKey
    Set accountsList = CreateObject("Scripting.Dictionary")
    
    Do While Not rsDeposits.EOF
        rawUserDeposit = rawUserDeposit + CDbl(rsDeposits("Amount"))
        
        ' Explicitly get Value to avoid storing Field object reference causing 'Object closed' error
        Dim accVal
        accVal = rsDeposits("AccountNumber").Value
        
        If Not IsNull(accVal) And CStr(accVal) <> "" Then
            If Not accountsList.Exists(accVal) Then
                accountsList.Add accVal, True
            End If
        End If
        
        rsDeposits.MoveNext
    Loop
    rsDeposits.Close
    Set rsDeposits = Nothing
    
    ' Initialize total user achieved
    totalUserAchieved = 0
    
    ' Process each unique account
    If accountsList.Count > 0 Then
        For Each accKey In accountsList.Keys
            accountNumber = CStr(accKey)
            
            ' Get all deposit rows for this account
            Set rsAccounts = CreateObject("ADODB.Recordset")
            sql = "SELECT DID, RefDate, Amount, [User], Prev_Ini_Bal " & _
                  "FROM DepositPlan " & _
                  "WHERE AccountNumber = '" & SQLEscape(accountNumber) & "' " & _
                  "AND Prev_Ini_Bal IS NOT NULL " & _
                  "ORDER BY RefDate, DID"
            rsAccounts.Open sql, conn
            
            If Not rsAccounts.EOF Then
                ' Get True Initial Balance
                trueInitial = 0
                If Not rsAccounts.EOF Then
                    ' MVC-equivalent: FirstOrDefault after ordering
                    trueInitial = CDbl(rsAccounts("Prev_Ini_Bal"))
                End If
                
                ' Calculate total deposits and user contribution
                totalDeposits = 0
                userContribution = 0
                rsAccounts.MoveFirst
                Do While Not rsAccounts.EOF
                    totalDeposits = totalDeposits + CDbl(rsAccounts("Amount"))
                    
                    If rsAccounts("User") = userName Then
                        userContribution = userContribution + CDbl(rsAccounts("Amount"))
                    End If
                    
                    rsAccounts.MoveNext
                Loop
                
                ' Get current balance from AccountReserve
                Set rsReserves = CreateObject("ADODB.Recordset")
                sql = "SELECT TOP 1 ISNULL(AccountBalance, 0) AS AccountBalance " & _
                      "FROM AccountReserve WHERE AccountNumber = '" & SQLEscape(accountNumber) & "' " & _
                      "ORDER BY CreatedDate DESC"
                rsReserves.Open sql, conn
                
                If Not rsReserves.EOF Then
                    currentBalance = CDbl(rsReserves("AccountBalance"))
                Else
                    currentBalance = 0
                End If
                rsReserves.Close
                Set rsReserves = Nothing
                
                ' Calculate user achievement
                If userContribution > 0 Then
                    availableFund = currentBalance - trueInitial
                    If availableFund < 0 Then availableFund = 0
                    If availableFund > totalDeposits Then availableFund = totalDeposits
                    
                    If totalDeposits > 0 Then
                        userAchieved = (userContribution / totalDeposits) * availableFund
                    Else
                        userAchieved = 0
                    End If
                    
                    If userAchieved > userContribution Then userAchieved = userContribution
                    
                    totalUserAchieved = totalUserAchieved + userAchieved
                End If
            End If
            
            rsAccounts.Close
            Set rsAccounts = Nothing
        Next
    End If
    
    accountsList.RemoveAll
    
    ' Calculate achievement percentage
    If assignedTarget > 0 And totalUserAchieved > 0 Then
        newAchievedPercent = Round((totalUserAchieved / assignedTarget) * 100, 2)
    Else
        newAchievedPercent = 0
    End If
    
    ' Use SQL to INSERT or UPDATE - more reliable than recordset
    Set cmd = CreateObject("ADODB.Command")
    cmd.ActiveConnection = conn
    
    ' Check if record exists
    sql = "SELECT COUNT(*) AS RecCount FROM UserAchieved WHERE UserName = '" & SQLEscape(userName) & "'"
    Set rsReserves = CreateObject("ADODB.Recordset")
    rsReserves.Open sql, conn
    Dim exists
    exists = (rsReserves("RecCount") > 0)
    rsReserves.Close
    Set rsReserves = Nothing
    
    If exists Then
        ' UPDATE
        sql = "UPDATE UserAchieved SET " & _
              "UserID = " & userID & ", " & _
              "Process = " & SQLValue(process) & ", " & _
              "District = " & SQLValue(district) & ", " & _
              "Branch = " & SQLValue(branch) & ", " & _
              "Position = " & SQLValue(position) & ", " & _
              "UserTarget = " & IIf(assignedTarget > 0, assignedTarget, "NULL") & ", " & _
              "CollectedAmount = " & rawUserDeposit & ", " & _
              "AchievedAmount = " & totalUserAchieved & ", " & _
              "AchievedPercent = " & newAchievedPercent & ", " & _
              "UpdatedDate = GETDATE() " & _
              "WHERE UserName = '" & SQLEscape(userName) & "'"
        
        cmd.CommandText = sql
        cmd.Execute
        
        LogMessage "  -> Updated: Target=" & assignedTarget & ", Collected=" & Round(rawUserDeposit, 2) & ", Achieved=" & Round(totalUserAchieved, 2) & ", Percent=" & newAchievedPercent & "%"
    Else
        ' INSERT
        sql = "INSERT INTO UserAchieved (UserName, UserID, Process, District, Branch, Position, UserTarget, CollectedAmount, AchievedAmount, AchievedPercent, UpdatedDate) " & _
              "VALUES ('" & SQLEscape(userName) & "', " & userID & ", " & SQLValue(process) & ", " & SQLValue(district) & ", " & _
              SQLValue(branch) & ", " & SQLValue(position) & ", " & IIf(assignedTarget > 0, assignedTarget, "NULL") & ", " & _
              rawUserDeposit & ", " & totalUserAchieved & ", " & newAchievedPercent & ", GETDATE())"
        
        cmd.CommandText = sql
        cmd.Execute
        
        LogMessage "  -> Inserted: Target=" & assignedTarget & ", Collected=" & Round(rawUserDeposit, 2) & ", Achieved=" & Round(totalUserAchieved, 2) & ", Percent=" & newAchievedPercent & "%"
    End If
    
    recordCount = recordCount + 1
    Set cmd = Nothing
    
    rsUsers.MoveNext
Loop

rsUsers.Close
Set rsUsers = Nothing
conn.Close
Set conn = Nothing

LogMessage String(60, "-")
LogMessage "Completed! Processed " & recordCount & " users."
LogMessage "============================================================"

logFile.Close
Set logFile = Nothing
Set fso = Nothing
Set accountsList = Nothing

' Helper Functions
Sub LogMessage(msg)
    WScript.Echo msg
    logFile.WriteLine Now & " - " & msg
End Sub

Function Nz(value, defaultValue)
    If IsNull(value) Then
        Nz = defaultValue
    Else
        Nz = value
    End If
End Function

Function SQLEscape(str)
    SQLEscape = Replace(str, "'", "''")
End Function

Function SQLValue(str)
    If str = "" Or IsNull(str) Then
        SQLValue = "NULL"
    Else
        SQLValue = "'" & SQLEscape(str) & "'"
    End If
End Function

Function IIf(condition, trueValue, falseValue)
    If condition Then
        IIf = trueValue
    Else
        IIf = falseValue
    End If
End Function
