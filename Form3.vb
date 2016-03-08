Public Class Form3
    Function ValidatePassword(ByVal pwd As String,
    Optional ByVal minLength As Integer = 8,
    Optional ByVal numUpper As Integer = 1,
    Optional ByVal numLower As Integer = 1,
    Optional ByVal numNumbers As Integer = 1,
    Optional ByVal numSpecial As Integer = 1) As Boolean


        Dim upper As New System.Text.RegularExpressions.Regex("[A-Z]")
        Dim lower As New System.Text.RegularExpressions.Regex("[a-z]")
        Dim number As New System.Text.RegularExpressions.Regex("[0-9]")
        ' Special is "none of the above".
        Dim special As New System.Text.RegularExpressions.Regex("[^a-zA-Z0-9]")

        ' Check the length.
        If Len(pwd) < minLength Then Return False
        ' Check for minimum number of occurrences.
        If upper.Matches(pwd).Count < numUpper Then Return False
        If lower.Matches(pwd).Count < numLower Then Return False
        If number.Matches(pwd).Count < numNumbers Then Return False
        If special.Matches(pwd).Count < numSpecial Then Return False

        ' Passed all checks.
        Return True
    End Function

    Public Shared Sub SetADProperty(ByVal de As DirectoryServices.DirectoryEntry, _
ByVal pName As String, ByVal pValue As String)
        'First make sure the property value isnt "nothing"
        If Not pValue Is Nothing Then
            'Check to see if the DirectoryEntry contains this property already
            If de.Properties.Contains(pName) Then 'The DE contains this property
                'Update the properties value
                de.Properties(pName)(0) = pValue
            Else    'Property doesnt exist
                'Add the property and set it's value
                de.Properties(pName).Add(pValue)
            End If
        End If
    End Sub

    Private Shared Sub SetPassword(ByVal dEntry As DirectoryServices.DirectoryEntry, _
ByVal sPassword As String)
        Dim oPassword As Object() = New Object() {sPassword}
        Dim ret As Object = dEntry.Invoke("SetPassword", oPassword)
        dEntry.CommitChanges()
    End Sub

    Private Shared Sub EnableAccount(ByVal de As DirectoryServices.DirectoryEntry)
        'UF_DONT_EXPIRE_PASSWD 0x10000
        Dim exp As Integer = CInt(de.Properties("userAccountControl").Value)
        de.Properties("userAccountControl").Value = exp Or &H1
        de.CommitChanges()
        'UF_ACCOUNTDISABLE 0x0002
        Dim val As Integer = CInt(de.Properties("userAccountControl").Value)
        de.Properties("userAccountControl").Value = val And Not &H2
        de.CommitChanges()
    End Sub

    Private Sub txtConfirm_TextChanged(sender As Object, e As EventArgs) Handles txtConfirm.TextChanged
        If txtConfirm.Text <> txtPassword.Text Then
            txtConfirm.ForeColor = Color.Red
            txtPassword.ForeColor = Color.Red
            lblPWMatch.Text = "Passwords do not match"
            lblPWMatch.ForeColor = Color.Red
        Else
            txtConfirm.ForeColor = Color.Black
            txtPassword.ForeColor = Color.Black
            lblPWMatch.Text = "Passwords match"
            lblPWMatch.ForeColor = Color.Green
        End If
        
    End Sub

    Private Sub txtPassword_TextChanged(sender As Object, e As EventArgs) Handles txtPassword.TextChanged
        If ValidatePassword(txtPassword.Text) <> True Then
            lblPWStrength.Text = "Password strength: Weak"
            lblPWStrength.ForeColor = Color.Red
        Else
            lblPWStrength.Text = "Password strength: Strong"
            lblPWStrength.ForeColor = Color.Green
        End If
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        Dim getAns As Integer
        getAns = MsgBox("About to save new user.  Are you sure?", MsgBoxStyle.OkCancel, "Saving...")
        Select Case getAns
            Case MsgBoxResult.Cancel
                Exit Sub
            Case MsgBoxResult.Ok

                Dim strDS As String = "LDAP://DC=pricexpert,DC=com"
                Dim Parent As New DirectoryServices.DirectoryEntry(strDS)
                Dim Search As New DirectoryServices.DirectorySearcher(Parent)


                Dim currentADUser As System.DirectoryServices.AccountManagement.UserPrincipal
                currentADUser = System.DirectoryServices.AccountManagement.UserPrincipal.Current
                Dim strCurrOU As String = currentADUser.DistinguishedName
                Dim strCurrOUShort As String

                strCurrOU = Strings.Replace(strCurrOU, Strings.Left(strCurrOU, Strings.InStr(strCurrOU, ",")), "")

                strCurrOUShort = Strings.Left(strCurrOU, Strings.InStr(strCurrOU, ",") - 1)
                strCurrOUShort = Strings.Replace(strCurrOUShort, "OU=", "")

                strDS = "LDAP://" & strCurrOU

                Dim dirEntry As New DirectoryServices.DirectoryEntry(strDS)
                'Create user account
                Dim adUsers As DirectoryServices.DirectoryEntries = dirEntry.Children
                Dim newUser As DirectoryServices.DirectoryEntry = adUsers.Add("CN=" & txtUsername.Text, "user")

                newUser.Properties("sAMAccountName").Value = txtUsername.Text

                'Set properties
                newUser.Properties("givenname").Value = txtFirstName.Text
                newUser.Properties("sn").Value = txtLastName.Text
                newUser.Properties("DisplayName").Value = txtFirstName.Text & " " & txtLastName.Text
                newUser.Properties("userPrincipalName").Value = txtUsername.Text
                newUser.CommitChanges()
                'Set the password
                SetPassword(newUser, txtPassword.Text)

                'Add the user to the specified group
                Dim dirGroup As New DirectoryServices.DirectoryEntry("LDAP://pricexpert.com/CN=" & strCurrOUShort & " PriceXpert Users," & strCurrOU)
                dirGroup.Properties("member").Add("CN=" & txtUsername.Text & "," & strCurrOU)
                dirGroup.CommitChanges()

                'Enable the account
                EnableAccount(newUser)
                'Close & clean-up
                newUser.Close()
                dirEntry.Close()
                MsgBox("New user created.")
                Form1.Load_Grid()

                Form1.Show()

                Me.Close()
        End Select

    End Sub

    Public Function IsValidADLogin(ByVal loginName As String, _
       ByVal givenName As String, ByVal surName As String) As Boolean
        Try
            Dim search As New DirectoryServices.DirectorySearcher()
            search.Filter = String.Format("(&(SAMAccountName={0})(givenName={1})(sn={2}))", loginName, givenName, surName)
            search.PropertiesToLoad.Add("cn")
            search.PropertiesToLoad.Add("SAMAccountName")   'Users login name
            search.PropertiesToLoad.Add("givenName")    'Users first name
            search.PropertiesToLoad.Add("sn")   'Users last name
            'Use the .FindOne() Method to stop as soon as a match is found
            Dim result As DirectoryServices.SearchResult = search.FindOne()
            If result Is Nothing Then
                Return False
            Else
                Return True
            End If
        Catch ex As Exception
            MessageBox.Show(ex.Message, "Active Directory Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1)
            Return False
        End Try
    End Function

    Private Sub btnCheck_Click(sender As Object, e As EventArgs) Handles btnCheck.Click
        Dim strNewUser As String
        Dim intCounter As Integer
        Dim blUserCheck As Boolean

        intCounter = 0
        'Get cust code
        Dim currentADUser As System.DirectoryServices.AccountManagement.UserPrincipal
        currentADUser = System.DirectoryServices.AccountManagement.UserPrincipal.Current
        Dim currOU As String = currentADUser.DistinguishedName

        Dim currOUShort As String
        currOUShort = Strings.Replace(currOU, Strings.Left(currOU, Strings.InStr(currOU, ",")), "")
        currOUShort = Strings.Left(currOUShort, Strings.InStr(currOUShort, ",") - 1)
        currOUShort = Strings.Replace(currOUShort, "OU=", "")

        'build username
        strNewUser = currOUShort & "." & Strings.Left(txtFirstName.Text, 1) & txtLastName.Text
        If IsValidADLogin(strNewUser, txtFirstName.Text, txtLastName.Text) = True Then 'Already exists
            Do Until blUserCheck = False
                intCounter = intCounter + 1
                blUserCheck = IsValidADLogin(strNewUser & intCounter.ToString, txtFirstName.Text, txtLastName.Text)
                ' Exit if it gets out of hand
                If intCounter > 99 Then
                    MsgBox("Problem with creating user as defined. Please contact Support.", MsgBoxStyle.Critical, "Error!")
                    Exit Sub
                End If
            Loop
            strNewUser = strNewUser & intCounter.ToString
            txtUsername.Text = strNewUser
        Else
            txtUsername.Text = strNewUser
        End If

        btnSave.Enabled = True

    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Form1.Show()
        Me.Close()
    End Sub
End Class