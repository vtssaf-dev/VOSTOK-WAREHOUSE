Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports Microsoft.Win32

Public Class MainForm
    Inherits Form

    ' ============================================================
    ' CONTROLS
    ' ============================================================

    Private ReadOnly warehouseCombo As New ComboBox()
    Private ReadOnly doorButtons(4) As Button
    Private ReadOnly statusLabel As New Label()
    Private ReadOnly talkButton As New Button()
    Private ReadOnly sipStatusLabel As New Label()

    Private ReadOnly sipService As New SipTalkService()

    Private vlcPath As String = ""

    ' ============================================================
    ' HIKVISION LOGIN
    ' ============================================================

    Private Const UserName As String = "admin"

    ' IMPORTANT:
    ' Change CHANGE_ME to your actual password.
    Private Const Password As String = "CHANGE_ME"

    ' ============================================================
    ' CAMERA LIST
    ' ============================================================

    Private ReadOnly cameras As New Dictionary(Of String, String)()

    ' ============================================================
    ' DOOR IP ADDRESSES
    ' ============================================================

    Private ReadOnly doorIps As String() = {
        "192.168.5.131",
        "192.168.5.133",
        "192.168.5.135",
        "192.168.5.134",
        "192.168.5.132"
    }

    ' ============================================================
    ' CONSTRUCTOR
    ' ============================================================

    Public Sub New()

        InitializeCameras()

        Text = "VOSTOK-WAREHOUSE"

        StartPosition = FormStartPosition.CenterScreen

        ClientSize = New Size(874, 378)

        FormBorderStyle = FormBorderStyle.FixedSingle

        MaximizeBox = False

        MinimizeBox = True

        BackColor = Color.White

        ' --------------------------------------------------------
        ' BACKGROUND IMAGE
        ' --------------------------------------------------------

        Dim bgPath As String =
            Path.Combine(
                Application.StartupPath,
                "background.jpg")

        If File.Exists(bgPath) Then

            BackgroundImage =
                Image.FromFile(bgPath)

            BackgroundImageLayout =
                ImageLayout.Stretch

        End If

        ' --------------------------------------------------------
        ' BUILD FORM
        ' --------------------------------------------------------

        BuildInterface()

        AddHandler Shown,
            AddressOf MainForm_Shown

    End Sub

    ' ============================================================
    ' INITIALIZE CAMERAS
    ' ============================================================

    Private Sub InitializeCameras()

        cameras.Add(
            "WareHouse-7",
            "rtsp://admin:Vos@3558817@192.168.5.131:554/Streaming/Channels/101")

        cameras.Add(
            "WareHouse-9",
            "rtsp://admin:Vos@3558817@192.168.5.133:554/Streaming/Channels/101")

        cameras.Add(
            "WareHouse-4",
            "rtsp://admin:Vos@3558817@192.168.5.134:554/Streaming/Channels/101")

        cameras.Add(
            "WareHouse-5",
            "rtsp://admin:Vos@3558817@192.168.5.132:554/Streaming/Channels/101")

    End Sub

    ' ============================================================
    ' BUILD USER INTERFACE
    ' ============================================================

    Private Sub BuildInterface()

        ' --------------------------------------------------------
        ' TITLE
        ' --------------------------------------------------------

        Dim title As New Label()

        title.Text =
            "VOSTOK-WAREHOUSE"

        title.Font =
            New Font(
                "Segoe UI",
                20,
                FontStyle.Bold)

        title.AutoSize = True

        title.Location =
            New Point(28, 20)

        title.BackColor =
            Color.Transparent

        Controls.Add(title)

        ' --------------------------------------------------------
        ' CAMERA LABEL
        ' --------------------------------------------------------

        Dim cameraLabel As New Label()

        cameraLabel.Text =
            "Warehouse Camera"

        cameraLabel.AutoSize = True

        cameraLabel.Font =
            New Font(
                "Segoe UI",
                10,
                FontStyle.Bold)

        cameraLabel.Location =
            New Point(30, 82)

        cameraLabel.BackColor =
            Color.Transparent

        Controls.Add(cameraLabel)

        ' --------------------------------------------------------
        ' WAREHOUSE COMBOBOX
        ' --------------------------------------------------------

        warehouseCombo.DropDownStyle =
            ComboBoxStyle.DropDownList

        warehouseCombo.Location =
            New Point(30, 108)

        warehouseCombo.Size =
            New Size(270, 30)

        warehouseCombo.Items.Add(
            "WareHouse-7")

        warehouseCombo.Items.Add(
            "WareHouse-9")

        warehouseCombo.Items.Add(
            "WareHouse-4")

        warehouseCombo.Items.Add(
            "WareHouse-5")

        warehouseCombo.Enabled =
            False

        AddHandler warehouseCombo.SelectedIndexChanged,
            AddressOf WarehouseCombo_SelectedIndexChanged

        Controls.Add(warehouseCombo)

        ' --------------------------------------------------------
        ' TALK BUTTON
        ' --------------------------------------------------------

        talkButton.Text =
            "TALK"

        talkButton.Name =
            "TalkButton"

        talkButton.Size =
            New Size(100, 30)

        talkButton.Location =
            New Point(310, 108)

        talkButton.BackColor =
            Color.Red

        talkButton.ForeColor =
            Color.White

        talkButton.FlatStyle =
            FlatStyle.Flat

        talkButton.Font =
            New Font(
                "Segoe UI",
                9,
                FontStyle.Bold)

        talkButton.FlatAppearance.BorderSize =
            0

        ' MouseDown
        AddHandler talkButton.MouseDown,
            AddressOf TalkButton_MouseDown

        ' MouseUp
        AddHandler talkButton.MouseUp,
            AddressOf TalkButton_MouseUp

        ' MouseLeave
        AddHandler talkButton.MouseLeave,
            AddressOf TalkButton_MouseLeave

        Controls.Add(talkButton)

        ' --------------------------------------------------------
        ' SIP STATUS
        ' --------------------------------------------------------

        sipStatusLabel.Text =
            "SIP: Starting..."

        sipStatusLabel.AutoSize =
            True

        sipStatusLabel.Location =
            New Point(420, 115)

        sipStatusLabel.Font =
            New Font(
                "Segoe UI",
                8)

        sipStatusLabel.ForeColor =
            Color.DarkOrange

        sipStatusLabel.BackColor =
            Color.Transparent

        Controls.Add(sipStatusLabel)

        ' --------------------------------------------------------
        ' DOOR CONTROL LABEL
        ' --------------------------------------------------------

        Dim doorLabel As New Label()

        doorLabel.Text =
            "Door Control"

        doorLabel.AutoSize =
            True

        doorLabel.Font =
            New Font(
                "Segoe UI",
                10,
                FontStyle.Bold)

        doorLabel.Location =
            New Point(30, 168)

        doorLabel.BackColor =
            Color.Transparent

        Controls.Add(doorLabel)

        ' --------------------------------------------------------
        ' DOOR BUTTON NAMES
        ' --------------------------------------------------------

        Dim names As String() = {
            "Door 1 - WH7",
            "Door 2 - WH9",
            "Door 3",
            "Door 4 - WH4",
            "Door 5 - WH5"
        }

        ' --------------------------------------------------------
        ' CREATE DOOR BUTTONS
        ' --------------------------------------------------------

        For i As Integer = 0 To 4

            Dim b As New Button()

            b.Text =
                names(i)

            b.Name =
                "CommandButton" &
                (i + 1).ToString()

            b.Size =
                New Size(150, 52)

            b.Location =
                New Point(
                    30 + (i Mod 3) * 170,
                    200 + (i \ 3) * 65)

            b.BackColor =
                Color.Red

            b.ForeColor =
                Color.White

            b.FlatStyle =
                FlatStyle.Flat

            b.Font =
                New Font(
                    "Segoe UI",
                    9,
                    FontStyle.Bold)

            b.Tag =
                i

            b.FlatAppearance.BorderSize =
                0

            ' ----------------------------------------------------
            ' DISABLE DOOR 3
            ' ----------------------------------------------------

            If i = 2 Then

                b.Enabled =
                    False

                b.BackColor =
                    Color.Gray

                b.ForeColor =
                    Color.White

                b.Text =
                    "Door 3 - DISABLED"

            End If

            AddHandler b.Click,
                AddressOf DoorButton_Click

            doorButtons(i) =
                b

            Controls.Add(b)

        Next

        ' --------------------------------------------------------
        ' VLC STATUS
        ' --------------------------------------------------------

        statusLabel.Text =
            "Checking VLC..."

        statusLabel.AutoSize =
            True

        statusLabel.ForeColor =
            Color.DimGray

        statusLabel.Location =
            New Point(520, 112)

        statusLabel.BackColor =
            Color.Transparent

        Controls.Add(statusLabel)

        ' --------------------------------------------------------
        ' INFORMATION
        ' --------------------------------------------------------

        Dim info As New Label()

        info.Text =
            "Select a warehouse to open its live camera in VLC." &
            Environment.NewLine &
            "Press and hold TALK to speak through the Hikvision device."

        info.AutoSize =
            True

        info.Location =
            New Point(520, 160)

        info.Font =
            New Font(
                "Segoe UI",
                10)

        info.BackColor =
            Color.Transparent

        Controls.Add(info)

    End Sub

    ' ============================================================
    ' FORM SHOWN
    ' ============================================================

    Private Sub MainForm_Shown(
        sender As Object,
        e As EventArgs)

        vlcPath =
            FindVlcPath()

        If String.IsNullOrWhiteSpace(vlcPath) Then

            warehouseCombo.Enabled =
                False

            statusLabel.Text =
                "VLC NOT INSTALLED"

            statusLabel.ForeColor =
                Color.Red

        Else

            warehouseCombo.Enabled =
                True

            statusLabel.Text =
                "VLC Ready"

            statusLabel.ForeColor =
                Color.Green

        End If

        sipStatusLabel.Text =
            sipService.Status

    End Sub

    ' ============================================================
    ' FIND VLC
    ' ============================================================

    Private Function FindVlcPath() As String

        Dim candidates As String() = {
            "C:\Program Files\VideoLAN\VLC\vlc.exe",
            "C:\Program Files (x86)\VideoLAN\VLC\vlc.exe"
        }

        For Each p As String In candidates

            If File.Exists(p) Then

                Return p

            End If

        Next

        For Each view As RegistryView In
            New RegistryView() {
                RegistryView.Registry64,
                RegistryView.Registry32
            }

            Try

                Using key As RegistryKey =
                    RegistryKey.OpenBaseKey(
                        RegistryHive.LocalMachine,
                        view)

                    Using subKey As RegistryKey =
                        key.OpenSubKey(
                            "SOFTWARE\VideoLAN\VLC")

                        If subKey IsNot Nothing Then

                            Dim installDir As String =
                                TryCast(
                                    subKey.GetValue(
                                        "InstallDir"),
                                    String)

                            If Not String.IsNullOrWhiteSpace(
                                installDir) Then

                                Dim exe As String =
                                    Path.Combine(
                                        installDir,
                                        "vlc.exe")

                                If File.Exists(exe) Then

                                    Return exe

                                End If

                            End If

                        End If

                    End Using

                End Using

            Catch

            End Try

        Next

        Return ""

    End Function

    ' ============================================================
    ' CAMERA SELECTION
    ' ============================================================

    Private Sub WarehouseCombo_SelectedIndexChanged(
        sender As Object,
        e As EventArgs)

        If warehouseCombo.SelectedItem Is Nothing Then

            Return

        End If

        If String.IsNullOrWhiteSpace(vlcPath) Then

            Return

        End If

        Dim name As String =
            warehouseCombo.SelectedItem.ToString()

        Dim rtsp As String = Nothing

        If Not cameras.TryGetValue(
            name,
            rtsp) Then

            Return

        End If

        Try

            Dim psi As New ProcessStartInfo()

            psi.FileName =
                vlcPath

            psi.Arguments =
                "--width=640 " &
                "--height=360 " &
                "--aspect-ratio=16:9 " &
                """" & rtsp & """"

            psi.UseShellExecute =
                False

            Process.Start(psi)

        Catch ex As Exception

            MessageBox.Show(
                "Unable to start VLC:" &
                Environment.NewLine &
                ex.Message,
                "VLC Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

        End Try

    End Sub

    ' ============================================================
    ' DOOR BUTTON
    ' ============================================================

    Private Async Sub DoorButton_Click(
        sender As Object,
        e As EventArgs)

        Dim button As Button =
            DirectCast(
                sender,
                Button)

        Dim index As Integer =
            CInt(button.Tag)

        ' --------------------------------------------------------
        ' EXTRA PROTECTION FOR DOOR 3
        ' --------------------------------------------------------

        If index = 2 Then

            Return

        End If

        button.Enabled =
            False

        Try

            Dim ok As Boolean =
                Await OpenDoorAsync(
                    doorIps(index))

            If ok Then

                button.BackColor =
                    Color.Lime

                button.ForeColor =
                    Color.Black

            Else

                button.BackColor =
                    Color.OrangeRed

            End If

            Await Task.Delay(5000)

            button.BackColor =
                Color.Red

            button.ForeColor =
                Color.White

        Catch ex As Exception

            button.BackColor =
                Color.OrangeRed

            MessageBox.Show(
                "Door command failed:" &
                Environment.NewLine &
                ex.Message,
                "Door Control",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

            button.BackColor =
                Color.Red

            button.ForeColor =
                Color.White

        Finally

            button.Enabled =
                True

        End Try

    End Sub

    ' ============================================================
    ' OPEN DOOR
    ' ============================================================

    Private Async Function OpenDoorAsync(
        ip As String) As Task(Of Boolean)

        Dim curl As String =
            FindCurlPath()

        If String.IsNullOrWhiteSpace(curl) Then

            Throw New FileNotFoundException(
                "curl.exe was not found.")

        End If

        Dim url As String =
            "http://" &
            ip &
            "/ISAPI/AccessControl/RemoteControl/door/1"

        Dim xml As String =
            "<RemoteControlDoor><cmd>open</cmd></RemoteControlDoor>"

        Dim psi As New ProcessStartInfo()

        psi.FileName =
            curl

        psi.Arguments =
            "--digest " &
            "-u """ &
            UserName &
            ":" &
            Password &
            """ " &
            "-H ""Content-Type: application/xml"" " &
            "-X PUT " &
            """" &
            url &
            """ " &
            "-d """ &
            xml &
            """"

        psi.UseShellExecute =
            False

        psi.CreateNoWindow =
            True

        psi.RedirectStandardOutput =
            True

        psi.RedirectStandardError =
            True

        Using p As Process =
            Process.Start(psi)

            If p Is Nothing Then

                Return False

            End If

            Await p.WaitForExitAsync()

            Return p.ExitCode = 0

        End Using

    End Function

    ' ============================================================
    ' FIND CURL
    ' ============================================================

    Private Function FindCurlPath() As String

        Dim systemCurl As String =
            Path.Combine(
                Environment.SystemDirectory,
                "curl.exe")

        If File.Exists(systemCurl) Then

            Return systemCurl

        End If

        Return "curl.exe"

    End Function

    ' ============================================================
    ' TALK BUTTON - MOUSE DOWN
    ' ============================================================

    Private Async Sub TalkButton_MouseDown(
        sender As Object,
        e As MouseEventArgs)

        If e.Button <> MouseButtons.Left Then

            Return

        End If

        ' --------------------------------------------------------
        ' CHECK DEVICE REGISTRATION
        ' --------------------------------------------------------

        If Not sipService.IsDeviceRegistered() Then

            sipStatusLabel.Text =
                "SIP: Device not registered"

            sipStatusLabel.ForeColor =
                Color.Red

            MessageBox.Show(
                "DS-K1T502DBFWX is not registered." &
                Environment.NewLine &
                Environment.NewLine &
                "Set the Hikvision SIP/VoIP server IP " &
                "to this PC's IP address." &
                Environment.NewLine &
                "SIP Server Port: 5060",
                "TALK",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)

            Return

        End If

        ' --------------------------------------------------------
        ' TALKING STATUS
        ' --------------------------------------------------------

        talkButton.BackColor =
            Color.Lime

        talkButton.ForeColor =
            Color.Black

        talkButton.Text =
            "TALKING..."

        sipStatusLabel.Text =
            "SIP: Calling Device..."

        sipStatusLabel.ForeColor =
            Color.Green

        ' --------------------------------------------------------
        ' START TALK
        ' --------------------------------------------------------

        Dim result As Boolean =
            Await sipService.TalkAsync()

        If result Then

            talkButton.Text =
                "TALKING"

            sipStatusLabel.Text =
                "SIP: TALKING"

            sipStatusLabel.ForeColor =
                Color.Green

        Else

            talkButton.BackColor =
                Color.Red

            talkButton.ForeColor =
                Color.White

            talkButton.Text =
                "TALK"

            sipStatusLabel.Text =
                sipService.Status

            sipStatusLabel.ForeColor =
                Color.Red

        End If

    End Sub

    ' ============================================================
    ' TALK BUTTON - MOUSE UP
    ' ============================================================

    Private Sub TalkButton_MouseUp(
        sender As Object,
        e As MouseEventArgs)

        StopTalking()

    End Sub

    ' ============================================================
    ' TALK BUTTON - MOUSE LEAVE
    ' ============================================================

    Private Sub TalkButton_MouseLeave(
        sender As Object,
        e As EventArgs)

        StopTalking()

    End Sub

    ' ============================================================
    ' STOP TALKING
    ' ============================================================

    Private Sub StopTalking()

        Try

            sipService.StopTalk()

        Catch

        End Try

        talkButton.BackColor =
            Color.Red

        talkButton.ForeColor =
            Color.White

        talkButton.Text =
            "TALK"

        sipStatusLabel.Text =
            sipService.Status

        sipStatusLabel.ForeColor =
            Color.DarkOrange

    End Sub

    ' ============================================================
    ' FORM CLOSING
    ' ============================================================

    Private Sub MainForm_FormClosing(
        sender As Object,
        e As FormClosingEventArgs) _
        Handles MyBase.FormClosing

        Try

            sipService.StopTalk()

            sipService.Shutdown()

        Catch

        End Try

    End Sub

End Class
