Option Explicit

' Constants for connection, SOAP endpoint, and log directory
Const CONNECTION_STRING = "Provider=SQLOLEDB;Data Source=ERP-LP-09\SQLEXPRESS;Initial Catalog=TRMS;Integrated Security=SSPI;"
' Const CONNECTION_STRING = "Provider=SQLOLEDB;Data Source=COOPFILESERVER;Initial Catalog=TRMS;User ID=sa;Password=123456;"
Const API_URL = "http://10.1.130.17:5108/api/balance/by-account?accountNo="
Const LOG_DIRECTORY = "D:\Mapping\"

Dim fso
Set fso = CreateObject("Scripting.FileSystemObject")

' Create log directory if it doesn't exist
If Not fso.FolderExists(LOG_DIRECTORY) Then
    fso.CreateFolder(LOG_DIRECTORY)
End If

' Log an error message to errors.log
Sub LogError(message)
    Dim logPath, logFile
    logPath = LOG_DIRECTORY & "errors.log"
    Set logFile = fso.OpenTextFile(logPath, 8, True)
    logFile.WriteLine Now & " - " & message
    logFile.Close
End Sub

' Log a success message to Updatedeposit.log
Sub LogSuccess(message)
    Dim logPath, logFile
    logPath = LOG_DIRECTORY & "UpdateAccountBalanceMapping.log"
    Set logFile = fso.OpenTextFile(logPath, 8, True)
    logFile.WriteLine Now & " - " & message
    logFile.Close
End Sub

' Fetch account balance from the REST API
Function GetAccountBalance(accountNumber)
    Dim http, url
    url = API_URL & accountNumber

    Set http = CreateObject("MSXML2.ServerXMLHTTP.6.0")
    On Error Resume Next
    http.open "GET", url, False
    http.send
    If Err.Number <> 0 Then
        LogError "HTTP request failed for account " & accountNumber & " : " & Err.Description
        GetAccountBalance = ""
        Err.Clear
        On Error GoTo 0
        Exit Function
    End If
    On Error GoTo 0

    If http.Status = 200 Then
        GetAccountBalance = http.responseText
    Else
        LogError "API returned status " & http.Status & " for account " & accountNumber & " : " & http.responseText
        GetAccountBalance = ""
    End If
End Function

' Parse the JSON response to extract the localBalance
Function ParseBalance(response)
    ' Expected format: {"accountNo":"1000092078365","localBalance":2221.86}
    Dim balance, startPos, endPos, searchKey
    searchKey = """localBalance"":"
    
    startPos = InStr(response, searchKey)
    If startPos > 0 Then
        startPos = startPos + Len(searchKey)
        ' Check for any trailing characters like , or }
        Dim possibleEndings, i, currentEnd, endings
        endings = Array(",", "}", " ", vbCr, vbLf)
        endPos = Len(response) + 1
        
        For i = 0 To UBound(endings)
            currentEnd = InStr(startPos, response, endings(i))
            If currentEnd > 0 And currentEnd < endPos Then
                endPos = currentEnd
            End If
        Next
        
        balance = Trim(Mid(response, startPos, endPos - startPos))
        ' Remove any potential quotes if it's returned as a string "2221.86"
        balance = Replace(balance, """", "")
        
        If IsNumeric(balance) Then
            ParseBalance = CDbl(balance)
        Else
            LogError "localBalance is not numeric: " & balance
            ParseBalance = ""
        End If
    Else
        LogError "localBalance key not found in response: " & response
        ParseBalance = ""
    End If
End Function

' Update the account balance in the specified table
Sub UpdateAccountBalanceInDatabase(tableName, accountNumber, balance)
    Dim conn, cmd, sql
    Set conn = CreateObject("ADODB.Connection")

    On Error Resume Next
    conn.Open CONNECTION_STRING
    If Err.Number <> 0 Then
        LogError "Database connection failed for account " & accountNumber & ": " & Err.Description
        Err.Clear
        Exit Sub
    End If
    On Error GoTo 0

    sql = "UPDATE " & tableName & " SET CurrentBalance = ? WHERE AccountNumber = ?"

    Set cmd = CreateObject("ADODB.Command")
    With cmd
        .ActiveConnection = conn
        .CommandText = sql
        .CommandType = 1 ' adCmdText
        .Parameters.Append .CreateParameter("Balance", 5, 1, , balance) ' adDouble
        .Parameters.Append .CreateParameter("AccountNumber", 200, 1, 50, accountNumber) ' adVarChar
        On Error Resume Next
        .Execute
        If Err.Number <> 0 Then
            LogError "AccountMapping Failed to update account: " & accountNumber & " | Error: " & Err.Description
            Err.Clear
        Else
            LogSuccess "Updated balance for account: " & accountNumber & " | Balance: " & balance
        End If
        On Error GoTo 0
    End With

    conn.Close
End Sub

' Main routine to loop through all accounts in table and update balances
Sub UpdateAccountBalances(tableName)
    Dim conn, rs, sql, accountNumber, mmacc, mmbal, finalBalance, response, balance, updatedCount
    updatedCount = 0

    Set conn = CreateObject("ADODB.Connection")
    conn.Open CONNECTION_STRING
    sql = "SELECT AccountNumber, MMACC, MMBAL FROM " & tableName
    Set rs = conn.Execute(sql)

    Do Until rs.EOF
        accountNumber = Trim(rs("AccountNumber").Value)
        LogSuccess "Reading account: " & accountNumber
        mmacc = rs("MMACC").Value
        mmbal = rs("MMBAL").Value

        If accountNumber <> "" Then
            response = GetAccountBalance(accountNumber)
            balance = ParseBalance(response)

            If IsNumeric(balance) Then
                finalBalance = CDbl(balance)               

                ' ----- APPLY YOUR RULE -----
                Dim cleanMMBAL

                If IsNull(mmbal) Then
                    cleanMMBAL = ""
                Else
                    cleanMMBAL = Trim(CStr(mmbal))
                    cleanMMBAL = Replace(cleanMMBAL, ",", "") ' remove commas
                End If

                If Trim(mmacc) <> "" And cleanMMBAL <> "" And IsNumeric(cleanMMBAL) Then
                    finalBalance = finalBalance + CDbl(cleanMMBAL)
                End If

                ' ---------------------------
                LogSuccess "MMACC=" & mmacc & ", MMBAL=" & mmbal & ", FinalBalance=" & finalBalance
                UpdateAccountBalanceInDatabase tableName, accountNumber, finalBalance
                updatedCount = updatedCount + 1
            Else
                LogError "Balance parsing failed or empty for account: " & accountNumber
            End If
        Else
            LogError "Empty AccountNumber skipped."
        End If

        rs.MoveNext
    Loop


    rs.Close
    conn.Close
    LogSuccess "Total accounts updated in table " & tableName & ": " & updatedCount
End Sub

' =============== EXECUTE ===============
UpdateAccountBalances "AccountMapping"

WScript.Echo "Account balance update complete. Check logs at " & LOG_DIRECTORY