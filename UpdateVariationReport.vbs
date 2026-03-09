' ========================================================================
' VBScript: Update Variation Report Data
' Purpose: Calculate and update VariationReport table from AccountMapping
' ========================================================================

Option Explicit

' Database connection string
Const DB_CONNECTION_STRING = "Provider=SQLOLEDB;Data Source=ERP-LP-09\SQLEXPRESS;Initial Catalog=TRMS;Integrated Security=SSPI;"
Const LOG_FILE_NAME = "UpdateVariationReport.log"

Dim conn, rsData, cmd, fso, logFile
Dim userID, userName, process, district, branch, position
Dim accCount, begBal, curBal, variation, changePercent
Dim sql, recordCount, exists, rsCheck

' Initialize Logging
Set fso = CreateObject("Scripting.FileSystemObject")
' Open log file for appending (8), create if not exists (True)
Set logFile = fso.OpenTextFile(LOG_FILE_NAME, 8, True)

LogMessage "============================================================"
LogMessage "Variation Report Script Started"
LogMessage "============================================================"

' Create connection object
On Error Resume Next
Set conn = CreateObject("ADODB.Connection")
conn.Open DB_CONNECTION_STRING

If Err.Number <> 0 Then
    LogMessage "ERROR: Could not connect to database. " & Err.Description
    WScript.Quit
End If
On Error GoTo 0

LogMessage "Connected to database successfully."
LogMessage "Starting Variation calculation..."
LogMessage String(60, "-")

recordCount = 0

' Get aggregated data from AccountMapping grouped by User
' Using a direct JOIN and GROUP BY is efficient for 3M+ records
sql = "SELECT " & _
      "u.ID, u.UserName, u.Process, u.District, u.Branch, u.Postion, " & _
      "COUNT(m.Acc_ID) AS AccCount, " & _
      "SUM(ISNULL(m.BegginingBalance, 0)) AS BegBal, " & _
      "SUM(ISNULL(m.CurrentBalance, 0)) AS CurBal " & _
      "FROM Users u " & _
      "INNER JOIN AccountMapping m ON u.UserName = m.UserName " & _
      "GROUP BY u.ID, u.UserName, u.Process, u.District, u.Branch, u.Postion"

Set rsData = CreateObject("ADODB.Recordset")
rsData.Open sql, conn

' Loop through each user's aggregated data
Do While Not rsData.EOF
    userID = rsData("ID")
    userName = rsData("UserName")
    process = Nz(rsData("Process"), "")
    district = Nz(rsData("District"), "")
    branch = Nz(rsData("Branch"), "")
    position = Nz(rsData("Postion"), "")
    accCount = rsData("AccCount")
    begBal = CDbl(rsData("BegBal"))
    curBal = CDbl(rsData("CurBal"))
    
    variation = curBal - begBal
    If begBal <> 0 Then
        changePercent = Round((variation / begBal) * 100, 2)
    Else
        changePercent = 0
    End If
    
    ' Check if record exists in VariationReport
    Set rsCheck = CreateObject("ADODB.Recordset")
    sql = "SELECT COUNT(*) AS RecCount FROM VariationReport WHERE UserName = '" & SQLEscape(userName) & "'"
    rsCheck.Open sql, conn
    exists = (rsCheck("RecCount") > 0)
    rsCheck.Close
    Set rsCheck = Nothing
    
    Set cmd = CreateObject("ADODB.Command")
    cmd.ActiveConnection = conn
    
    If exists Then
        ' UPDATE
        sql = "UPDATE VariationReport SET " & _
              "UserID = " & userID & ", " & _
              "Process = " & SQLValue(process) & ", " & _
              "District = " & SQLValue(district) & ", " & _
              "Branch = " & SQLValue(branch) & ", " & _
              "Position = " & SQLValue(position) & ", " & _
              "Accounts = '" & accCount & "', " & _
              "BeginningBalance = " & begBal & ", " & _
              "CurrentBalance = " & curBal & ", " & _
              "Variation = " & variation & ", " & _
              "Change = " & changePercent & ", " & _
              "UpdatedDate = GETDATE() " & _
              "WHERE UserName = '" & SQLEscape(userName) & "'"
    Else
        ' INSERT
        sql = "INSERT INTO VariationReport (UserID, UserName, Process, District, Branch, Position, Accounts, BeginningBalance, CurrentBalance, Variation, Change, UpdatedDate) " & _
              "VALUES (" & userID & ", '" & SQLEscape(userName) & "', " & SQLValue(process) & ", " & SQLValue(district) & ", " & _
              SQLValue(branch) & ", " & SQLValue(position) & ", '" & accCount & "', " & begBal & ", " & curBal & ", " & _
              variation & ", " & changePercent & ", GETDATE())"
    End If
    
    cmd.CommandText = sql
    cmd.Execute
    Set cmd = Nothing
    
    recordCount = recordCount + 1
    If recordCount Mod 100 = 0 Then
        LogMessage "Processed " & recordCount & " users..."
    End If
    
    rsData.MoveNext
Loop

rsData.Close
Set rsData = Nothing
conn.Close
Set conn = Nothing

LogMessage String(60, "-")
LogMessage "Completed! Processed " & recordCount & " reports."
LogMessage "============================================================"

logFile.Close
Set logFile = Nothing
Set fso = Nothing

' Helper Functions
Sub LogMessage(msg)
    WScript.Echo msg
    logFile.WriteLine Now & " - " & msg
End Sub

Function Nz(value, defaultValue)
    If IsNull(value) Or value = "" Then
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
