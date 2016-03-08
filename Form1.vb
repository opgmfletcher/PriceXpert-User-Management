
Public Class Form1
    Public Sub DataGridView1_DoubleClick()
        'Dim strAns As Integer = MsgBox(DataGridView1.CurrentCell.Value)
        'Do Load User
        If DataGridView1.RowCount = 0 Then Exit Sub

        Form2.Show()
        Form2.LoadFields(DataGridView1.SelectedRows(0).Cells(0).Value.ToString)
    End Sub

    Public Sub Load_Grid()
        Dim Users(,) As String = Nothing
        Dim S As String = "LDAP://DC=pricexpert,DC=com"
        Dim Parent As New DirectoryServices.DirectoryEntry(S)
        Dim Search As New DirectoryServices.DirectorySearcher(Parent)

        Dim currentADUser As System.DirectoryServices.AccountManagement.UserPrincipal
        currentADUser = System.DirectoryServices.AccountManagement.UserPrincipal.Current
        Dim currOU As String = currentADUser.DistinguishedName

        Dim currOUShort As String
        currOUShort = Strings.Replace(currOU, Strings.Left(currOU, Strings.InStr(currOU, ",")), "")
        currOUShort = Strings.Left(currOUShort, Strings.InStr(currOUShort, ",") - 1)
        currOUShort = Strings.Replace(currOUShort, "OU=", "")

        Dim tblUsers As New DataTable
        tblUsers.Columns.Add("Login", GetType(String))
        tblUsers.Columns.Add("Name", GetType(String))
        tblUsers.Columns.Add("Enabled", GetType(String))
        tblUsers.Columns.Add("Locked", GetType(String))

        Dim rRow As DataRow

        Search.SearchScope = DirectoryServices.SearchScope.Subtree
        Search.Filter = "(CN=" & currOUShort & " PriceXpert Users)"
        Search.PropertiesToLoad.Add("member")

        Const ADS_UF_ACCOUNTDISABLE As Integer = &H2
        Dim intDummy As Integer

        Dim Result As DirectoryServices.SearchResult = Search.FindOne
        Dim prop_value As String, i As Integer = 0


        If Result IsNot Nothing Then
            If Result.Properties("member").Count > 0 Then
                ReDim Users(1, Result.Properties("member").Count - 1)
                For Each prop_value In Result.Properties("member")
                    Dim S2 As New DirectoryServices.DirectorySearcher(Parent)
                    rRow = tblUsers.NewRow()
                    S2.SearchScope = DirectoryServices.SearchScope.Subtree
                    S2.Filter = "(" & prop_value.Substring(0, prop_value.IndexOf(","c)) & ")"
                    S2.PropertiesToLoad.Add("SAMAccountName")
                    S2.PropertiesToLoad.Add("DisplayName")
                    S2.PropertiesToLoad.Add("Enabled")
                    S2.PropertiesToLoad.Add("LockoutTime")

                    Dim R2 As DirectoryServices.SearchResult = S2.FindOne
                    rRow(0) = R2.GetDirectoryEntry.Properties("SAMAccountName").Value
                    rRow(1) = R2.GetDirectoryEntry.Properties("DisplayName").Value

                    intDummy = CInt(R2.GetDirectoryEntry.Properties("userAccountControl").Value)

                    If CBool(intDummy And ADS_UF_ACCOUNTDISABLE) Then
                        rRow(2) = "N"
                    Else
                        rRow(2) = "Y"
                    End If

                    If R2.GetDirectoryEntry.InvokeGet("IsAccountLocked") Then
                        rRow(3) = "Y"
                    Else
                        rRow(3) = "N"
                    End If
                    'For Each Prop As String In R2.Properties("SAMAccountName")

                    'rRow(0) = Prop
                    'For Each Prop2 As String In R2.Properties("DisplayName")

                    'rRow(1) = Prop2
                    'For Each Prop3 As String In R2.Properties("Enabled")
                    'rRow(2) = Prop3
                    'Next
                    'Next


                    'i = i + 1
                    'Next
                    tblUsers.Rows.Add(rRow)
                Next

            End If
        End If

        DataGridView1.DataSource = tblUsers
        DataGridView1.Sort(DataGridView1.Columns(0), System.ComponentModel.ListSortDirection.Ascending)
        DataGridView1.Columns(0).Width = 97
        DataGridView1.Columns(1).Width = 200
        DataGridView1.Columns(2).Width = 50
        DataGridView1.Columns(3).Width = 50

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        
        Load_Grid()


    End Sub

    Private Sub DataGridView1_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.CellContentDoubleClick
        DataGridView1_DoubleClick()
    End Sub

    Private Sub DataGridView1_RowDoubleClick() Handles DataGridView1.RowHeaderMouseDoubleClick
        DataGridView1_DoubleClick()
    End Sub

    Private Sub btnEditSelected_Click(sender As Object, e As EventArgs) Handles btnEditSelected.Click
        DataGridView1_DoubleClick()
    End Sub

    Private Sub btnAddNew_Click(sender As Object, e As EventArgs) Handles btnAddNew.Click
        Form3.Show()

    End Sub
End Class
