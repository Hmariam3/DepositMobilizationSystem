' ========================================================================
' VBScript: Test Database Connection and UserAchieved Table
' Purpose: Test if we can insert into UserAchieved table
' ========================================================================

Option Explicit

Const DB_CONNECTION_STRING = "Provider=SQLOLEDB;Data Source=ERP-LP-09\SQLEXPRESS;Initial Catalog=TRMS;Integrated Security=SSPI;"

Dim conn, rs, sql

' Create connection
Set conn = CreateObject("ADODB.Connection")
conn.Open DB_CONNECTION_STRING

WScript.Echo "Connected successfully!"

' Check UserAchieved table structure
sql = "SELECT TOP 1 * FROM UserAchieved"
Set rs = CreateObject("ADODB.Recordset")
rs.Open sql, conn, 1, 3

WScript.Echo "UserAchieved table columns:"
Dim i
For i = 0 To rs.Fields.Count - 1
    WScript.Echo "  " & rs.Fields(i).Name & " (" & rs.Fields(i).Type & ")"
Next

rs.Close

' Try to insert a test record
WScript.Echo ""
WScript.Echo "Attempting to insert test record..."

sql = "SELECT * FROM UserAchieved WHERE UserName = 'TEST_USER_VBS'"
Set rs = CreateObject("ADODB.Recordset")
rs.Open sql, conn, 2, 3  ' adOpenDynamic, adLockOptimistic

If rs.EOF Then
    rs.AddNew
    rs("UserName") = "TEST_USER_VBS"
    rs("UserID") = 999
    rs("Process") = "Test"
    rs("District") = "Test"
    rs("Branch") = "Test"
    rs("Position") = "Test"
    rs("UserTarget") = 100000
    rs("CollectedAmount") = 50000
    rs("AchievedAmount") = 45000
    rs("AchievedPercent") = 45
    rs("UpdatedDate") = Now()
    rs.Update
    WScript.Echo "Test record inserted successfully!"
Else
    WScript.Echo "Test record already exists, updating..."
    rs("CollectedAmount") = 50000
    rs("AchievedAmount") = 45000
    rs("UpdatedDate") = Now()
    rs.Update
    WScript.Echo "Test record updated successfully!"
End If

rs.Close
conn.Close

WScript.Echo ""
WScript.Echo "Test completed! Check UserAchieved table for TEST_USER_VBS"

Set rs = Nothing
Set conn = Nothing
