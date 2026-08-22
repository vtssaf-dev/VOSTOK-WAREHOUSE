Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports Microsoft.Win32

Public Class MainForm
    Inherits Form

    Private ReadOnly warehouseCombo As New ComboBox()
    Private ReadOnly doorButtons(4) As Button
    Private ReadOnly statusLabel As New Label()
    Private ReadOnly talkButton As New Button()
    Private ReadOnly sipStatusLabel As New Label()
    Private ReadOnly sipService As New SipTalkService()
    Private vlcPath As String = ""

    Private Const UserName As String = "admin"
    Private Const Password As String = "CHANGE_ME"

    Private ReadOnly cameras As New Dictionary(Of String, String) From {
        {"WareHouse-7", "rtsp://admin:CHANGE_ME@192.168.5.131:554/Streaming/Channels/101"},
        {"WareHouse-9", "rtsp://admin:CHANGE_ME@192.168.5.133:554/Streaming/Channels/101"},
        {"WareHouse-4", "rtsp://admin:CHANGE_ME@192.168.5.134:554/Streaming/Channels/101"},
        {"WareHouse-5", "rtsp://admin:CHANGE_ME@192.168.5.132:554/Streaming/Channels/101"}
    }

    Private ReadOnly doorIps As String() = {
        "192.168.5.131", "192.168.5.133", "192.168.5.135",
        "192.168.5.134", "192.168.5.132"
    }

    Public Sub New()
        Text = "VOSTOK-WAREHOUSE"
        StartPosition = FormStartPosition.CenterScreen
        ClientSize = New Size(874, 378)
        FormBorderStyle = FormBorderStyle.FixedSingle
        MaximizeBox = False
        BackColor = Color.White

        Dim bg = Path.Combine(Application.StartupPath, "background.jpg")
        If File.Exists(bg) Then
            BackgroundImage = Image.FromFile(bg)
            BackgroundImageLayout = ImageLayout.Stretch
        End If

        BuildInterface()
        AddHandler Shown, AddressOf MainForm_Shown
    End Sub

    Private Sub BuildInterface()
        Dim title As New Label With {.Text = "VOSTOK-WAREHOUSE", .Font = New Font("Segoe UI", 20, FontStyle.Bold), .AutoSize = True, .Location = New Point(28, 20), .BackColor = Color.Transparent}
        Controls.Add(title)

        Dim cameraLabel As New Label With {.Text = "Warehouse Camera", .AutoSize = True, .Font = New Font("Segoe UI", 10, FontStyle.Bold), .Location = New Point(30, 82), .BackColor = Color.Transparent}
        Controls.Add(cameraLabel)

        warehouseCombo.DropDownStyle = ComboBoxStyle.DropDownList
        warehouseCombo.Location = New Point(30, 108)
        warehouseCombo.Size = New Size(270, 30)
        warehouseCombo.Items.AddRange(New Object() {"WareHouse-7", "WareHouse-9", "WareHouse-4", "WareHouse-5"})
        warehouseCombo.Enabled = False
        AddHandler warehouseCombo.SelectedIndexChanged, AddressOf WarehouseCombo_SelectedIndexChanged
        Controls.Add(warehouseCombo)

        talkButton.Text = "TALK"
        talkButton.Size = New Size(100, 30)
        talkButton.Location = New Point(310, 108)
        talkButton.BackColor = Color.Red
        talkButton.ForeColor = Color.White
        talkButton.FlatStyle = FlatStyle.Flat
        talkButton.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        talkButton.FlatAppearance.BorderSize = 0
        AddHandler talkButton.MouseDown, AddressOf TalkButton_MouseDown
        AddHandler talkButton.MouseUp, AddressOf TalkButton_MouseUp
        AddHandler talkButton.MouseLeave, AddressOf TalkButton_MouseUp
        Controls.Add(talkButton)

        sipStatusLabel.Text = "SIP: Starting..."
        sipStatusLabel.AutoSize = True
        sipStatusLabel.Location = New Point(420, 115)
        sipStatusLabel.Font = New Font("Segoe UI", 8)
        sipStatusLabel.ForeColor = Color.DarkOrange
        sipStatusLabel.BackColor = Color.Transparent
        Controls.Add(sipStatusLabel)

        Dim doorLabel As New Label With {.Text = "Door Control", .AutoSize = True, .Font = New Font("Segoe UI", 10, FontStyle.Bold), .Location = New Point(30, 168), .BackColor = Color.Transparent}
        Controls.Add(doorLabel)

        Dim names = New String() {"Door 1 - WH7", "Door 2 - WH9", "Door 3", "Door 4 - WH4", "Door 5 - WH5"}

        For i = 0 To 4
            Dim b = New Button With {
                .Text = names(i), .Name = "CommandButton" & (i + 1).ToString(),
                .Size = New Size(150, 52),
                .Location = New Point(30 + (i Mod 3) * 170, 200 + (i \ 3) * 65),
                .BackColor = Color.Red, .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat, .Font = New Font("Segoe UI", 9, FontStyle.Bold), .Tag = i
            }
            If i = 2 Then
                b.Enabled = False
                b.BackColor = Color.Gray
            End If
            b.FlatAppearance.BorderSize = 0
            AddHandler b.Click, AddressOf DoorButton_Click
            doorButtons(i) = b
            Controls.Add(b)
        Next

        statusLabel.Text = "Checking VLC..."
        statusLabel.AutoSize = True
        statusLabel.ForeColor = Color.DimGray
        statusLabel.Location = New Point(520, 112)
        statusLabel.BackColor = Color.Transparent
        Controls.Add(statusLabel)

        Dim info As New Label With {
            .Text = "Select a warehouse to open its live camera in VLC." & Environment.NewLine & "Press and hold TALK to speak through the Hikvision device.",
            .AutoSize = True, .Location = New Point(520, 160),
            .Font = New Font("Segoe UI", 10), .BackColor = Color.Transparent
        }
        Controls.Add(info)
    End Sub

    Private Sub MainForm_Shown(sender As Object, e As EventArgs)
        vlcPath = FindVlcPath()
        If String.IsNullOrWhiteSpace(vlcPath) Then
            warehouseCombo.Enabled = False
            statusLabel.Text = "VLC NOT INSTALLED"
            statusLabel.ForeColor = Color.Red
        Else
            warehouseCombo.Enabled = True
            statusLabel.Text = "VLC Ready"
            statusLabel.ForeColor = Color.Green
        End If
        sipStatusLabel.Text = sipService.Status
    End Sub

    Private Function FindVlcPath() As String
        Dim candidates = New String() {"C:\Program Files\VideoLAN\VLClc.exe", "C:\Program Files (x86)\VideoLAN\VLClc.exe"}
        For Each p In candidates
            If File.Exists(p) Then Return p
        Next
        For Each view In New RegistryView() {RegistryView.Registry64, RegistryView.Registry32}
            Try
                Using key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view)
                    Using subKey = key.OpenSubKey("SOFTWARE\VideoLAN\VLC")
                        If subKey IsNot Nothing Then
                            Dim installDir = TryCast(subKey.GetValue("InstallDir"), String)
                            If Not String.IsNullOrWhiteSpace(installDir) Then
                                Dim exe = Path.Combine(installDir, "vlc.exe")
                                If File.Exists(exe) Then Return exe
                            End If
                        End If
                    End Using
                End Using
            Catch
            End Try
        Next
        Return ""
    End Function

    Private Sub WarehouseCombo_SelectedIndexChanged(sender As Object, e As EventArgs)
        If warehouseCombo.SelectedItem Is Nothing OrElse String.IsNullOrWhiteSpace(vlcPath) Then Return
        Dim rtsp As String = Nothing
        If Not cameras.TryGetValue(warehouseCombo.SelectedItem.ToString(), rtsp) Then Return
        Try
            Dim psi As New ProcessStartInfo With {.FileName = vlcPath, .Arguments = $"--width=640 --height=360 --aspect-ratio=16:9 ""{rtsp}""", .UseShellExecute = False}
            Process.Start(psi)
        Catch ex As Exception
            MessageBox.Show("Unable to start VLC:" & Environment.NewLine & ex.Message, "VLC Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Async Sub DoorButton_Click(sender As Object, e As EventArgs)
        Dim button = DirectCast(sender, Button)
        Dim index = CInt(button.Tag)
        button.Enabled = False
        Try
            Dim ok = Await OpenDoorAsync(doorIps(index))
            If ok Then
                button.BackColor = Color.Lime
                button.ForeColor = Color.Black
            Else
                button.BackColor = Color.OrangeRed
            End If
            Await Task.Delay(5000)
            If index <> 2 Then
                button.BackColor = Color.Red
                button.ForeColor = Color.White
            End If
        Catch ex As Exception
            button.BackColor = Color.OrangeRed
            MessageBox.Show("Door command failed:" & Environment.NewLine & ex.Message, "Door Control", MessageBoxButtons.OK, MessageBoxIcon.Error)
            If index <> 2 Then
                button.BackColor = Color.Red
                button.ForeColor = Color.White
            End If
        Finally
            If index <> 2 Then button.Enabled = True
        End Try
    End Sub

    Private Async Function OpenDoorAsync(ip As String) As Task(Of Boolean)
        Dim curl = FindCurlPath()
        If String.IsNullOrWhiteSpace(curl) Then Throw New FileNotFoundException("curl.exe was not found.")
        Dim url = $"http://{ip}/ISAPI/AccessControl/RemoteControl/door/1"
        Dim xml = "<RemoteControlDoor><cmd>open</cmd></RemoteControlDoor>"
        Dim psi As New ProcessStartInfo With {
            .FileName = curl,
            .Arguments = $"--digest -u ""{UserName}:{Password}"" -H ""Content-Type: application/xml"" -X PUT ""{url}"" -d ""{xml}""",
            .UseShellExecute = False, .CreateNoWindow = True, .RedirectStandardOutput = True, .RedirectStandardError = True
        }
        Using p = Process.Start(psi)
            If p Is Nothing Then Return False
            Await p.WaitForExitAsync()
            Return p.ExitCode = 0
        End Using
    End Function

    Private Function FindCurlPath() As String
        Dim systemCurl = Path.Combine(Environment.SystemDirectory, "curl.exe")
        If File.Exists(systemCurl) Then Return systemCurl
        Return "curl.exe"
    End Function

    Private Async Sub TalkButton_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left Then Return
        If Not sipService.IsDeviceRegistered() Then
            sipStatusLabel.Text = "SIP: Device not registered"
            sipStatusLabel.ForeColor = Color.Red
            MessageBox.Show("DS-K1T502DBFWX is not registered." & Environment.NewLine & Environment.NewLine &
                            "Set the Hikvision SIP/VoIP server IP to this PC's IP address." & Environment.NewLine &
                            "Server port: 5060", "TALK", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        talkButton.BackColor = Color.Lime
        talkButton.ForeColor = Color.Black
        talkButton.Text = "TALKING..."
        sipStatusLabel.Text = "SIP: Calling Door1..."
        sipStatusLabel.ForeColor = Color.Green

        Dim result = Await sipService.TalkAsync()
        If result Then
            talkButton.Text = "TALKING"
            sipStatusLabel.Text = "SIP: TALKING"
        Else
            talkButton.BackColor = Color.Red
            talkButton.ForeColor = Color.White
            talkButton.Text = "TALK"
            sipStatusLabel.Text = sipService.Status
            sipStatusLabel.ForeColor = Color.Red
        End If
    End Sub

    Private Sub TalkButton_MouseUp(sender As Object, e As MouseEventArgs)
        sipService.StopTalk()
        talkButton.BackColor = Color.Red
        talkButton.ForeColor = Color.White
        talkButton.Text = "TALK"
        sipStatusLabel.Text = sipService.Status
        sipStatusLabel.ForeColor = Color.DarkOrange
    End Sub

    Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Try
            sipService.StopTalk()
            sipService.Shutdown()
        Catch
        End Try
    End Sub
End Class
