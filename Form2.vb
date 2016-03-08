Public Class Form2
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
    Private Sub Form2_Load(sender As Object, e As EventArgs) Handles MyBase.Load

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
    Public Sub LoadFields(strUserName As String)
        Dim S As String = "LDAP://DC=pricexpert,DC=com"
        Dim Parent As New DirectoryServices.DirectoryEntry(S)
        Dim Search As New DirectoryServices.DirectorySearcher(Parent)
        Dim intDummy As Integer
        Const ADS_UF_ACCOUNTDISABLE As Integer = &H2

        Search.PropertiesToLoad.Add("memberOf")
        Search.SearchScope = DirectoryServices.SearchScope.Subtree
        Search.Filter = "(&(objectClass=User)(sAMAccountName=" & strUserName & "))"

        Search.PropertiesToLoad.Add("SAMAccountName")
        Search.PropertiesToLoad.Add("sn")
        Search.PropertiesToLoad.Add("GivenName")
        Search.PropertiesToLoad.Add("Enabled")
        Search.PropertiesToLoad.Add("LockoutTime")

        Dim Result As DirectoryServices.SearchResult = Search.FindOne()

        Dim ResultEntry As DirectoryServices.DirectoryEntry = Result.GetDirectoryEntry()

        intDummy = CInt(ResultEntry.Properties("userAccountControl").Value)

        If CBool(intDummy And ADS_UF_ACCOUNTDISABLE) Then 'Account Disabled
            chkEnabled.Checked = False
        Else
            chkEnabled.Checked = True
        End If

        If ResultEntry.InvokeGet("IsAccountLocked") Then 'Account Locked
            chkLocked.Checked = True
            chkLocked.Enabled = True
        Else
            chkLocked.Checked = False
        End If

        txtFirstName.Text = ResultEntry.Properties("GivenName").Value.ToString
        txtLastName.Text = ResultEntry.Properties("sn").Value.ToString
        txtUserName.Text = ResultEntry.Properties("SAMAccountName").Value.ToString

    End Sub

   
    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Form1.Show()
        Me.Close()

    End Sub

    Private Sub btSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        Dim S As String = "LDAP://DC=pricexpert,DC=com"
        Dim Parent As New DirectoryServices.DirectoryEntry(S)
        Dim Search As New DirectoryServices.DirectorySearcher(Parent)
        Dim getAns As Integer

        Search.Filter = "(&(objectCategory=Person)(objectClass=user) (SAMAccountName=" & txtUserName.Text & "))"
        Search.SearchScope = DirectoryServices.SearchScope.Subtree
        Dim Result As DirectoryServices.SearchResult = Search.FindOne()
        If Not Result Is Nothing Then
            
            getAns = MsgBox("About to save changes.  Are you sure?", MsgBoxStyle.OkCancel, "Saving...")
            Select Case getAns
                Case MsgBoxResult.Cancel
                    Exit Sub
                Case MsgBoxResult.Ok
                    Using context = New DirectoryServices.AccountManagement.PrincipalContext(DirectoryServices.AccountManagement.ContextType.Domain, "pricexpert.com")
                        Using user = DirectoryServices.AccountManagement.UserPrincipal.FindByIdentity(context, txtUserName.Text)
                            user.GivenName = txtFirstName.Text
                            user.Surname = txtLastName.Text
                            user.DisplayName = txtFirstName.Text & " " & txtLastName.Text
                            If chkEnabled.Checked = True Then
                                user.Enabled = True
                            Else
                                user.Enabled = False
                            End If
                            If chkLocked.Checked = False Then
                                user.UnlockAccount()
                            End If
                            user.Save()
                        End Using

                    End Using

                    Form1.Load_Grid()

                    Form1.Show()

                    Me.Close()
            End Select

        End If

    End Sub

    Public Sub Text_Changed()
        btnSave.Enabled = True

    End Sub

    Private Sub txtFirstName_TextChanged(sender As Object, e As EventArgs) Handles txtFirstName.KeyPress

        Text_Changed()

    End Sub

    Private Sub txtLastName_TextChanged(sender As Object, e As EventArgs) Handles txtLastName.KeyPress

        Text_Changed()

    End Sub

   
    Private Sub chkChangePass_CheckedChanged(sender As Object, e As EventArgs) Handles chkChangePass.CheckedChanged
        If chkChangePass.Checked = True Then
            txtPassword.Enabled = True
            txtConfirm.Enabled = True
            lblPWStrength.Visible = True
            lblPWMatch.Visible = True
        Else
            txtPassword.Enabled = False
            txtConfirm.Enabled = False
            lblPWStrength.Visible = False
            lblPWMatch.Visible = False
        End If
    End Sub

    
    Private Sub chkEnabled_CheckedClicked(sender As Object, e As EventArgs) Handles chkEnabled.Click
        If chkEnabled.Checked = True Then
            chkEnabled.Checked = False

        End If

    End Sub
End Class