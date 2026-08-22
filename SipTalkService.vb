Imports System.Net
Imports System.Threading.Tasks
Imports SIPSorcery.Media
Imports SIPSorcery.SIP
Imports SIPSorcery.SIP.App
Imports SIPSorceryMedia.Windows

Public Class SipTalkService
    Private ReadOnly sipTransport As SIPTransport
    Private ReadOnly userAgent As SIPUserAgent
    Private audioEndpoint As WindowsAudioEndPoint = Nothing
    Private mediaSession As VoIPMediaSession = Nothing
    Private registeredContact As SIPURI = Nothing
    Private registeredUser As String = ""
    Private Const SIP_PORT As Integer = 5060

    Public Property Status As String = "Starting SIP..."

    Public Sub New()
        sipTransport = New SIPTransport()
        Dim localEndpoint As New IPEndPoint(IPAddress.Any, SIP_PORT)
        sipTransport.AddSIPChannel(New SIPUDPChannel(localEndpoint))
        AddHandler sipTransport.SIPTransportRequestReceived, AddressOf SIPRequestReceived
        userAgent = New SIPUserAgent(sipTransport, Nothing)
        Status = "SIP listening on UDP 5060"
    End Sub

    Private Async Function SIPRequestReceived(localEndPoint As SIPEndPoint,
                                               remoteEndPoint As SIPEndPoint,
                                               request As SIPRequest) As Task
        Try
            If request.Method = SIPMethodsEnum.REGISTER Then
                Await ProcessRegister(request)
            ElseIf request.Method = SIPMethodsEnum.OPTIONS Then
                Dim response = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Ok, Nothing)
                Await sipTransport.SendResponseAsync(response)
            End If
        Catch ex As Exception
            Status = "SIP error: " & ex.Message
        End Try
    End Function

    Private Async Function ProcessRegister(request As SIPRequest) As Task
        If request.Header.Contact Is Nothing OrElse request.Header.Contact.Count = 0 Then
            Dim response = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.BadRequest, Nothing)
            Await sipTransport.SendResponseAsync(response)
            Return
        End If

        registeredContact = request.Header.Contact(0).ContactURI
        If request.Header.From IsNot Nothing Then
            registeredUser = request.Header.From.FromURI.User
        End If

        Status = "Hikvision registered: " & registeredUser

        Dim okResponse = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Ok, Nothing)
        Await sipTransport.SendResponseAsync(okResponse)
    End Function

    Public Function IsDeviceRegistered() As Boolean
        Return registeredContact IsNot Nothing
    End Function

    Public Async Function TalkAsync() As Task(Of Boolean)
        If registeredContact Is Nothing Then
            Status = "DS-K1T502DBFWX not registered"
            Return False
        End If

        Try
            audioEndpoint = New WindowsAudioEndPoint(New AudioEncoder())
            mediaSession = New VoIPMediaSession(audioEndpoint.ToMediaEndPoints())
            mediaSession.AcceptRtpFromAny = True

            Status = "Calling Hikvision..."
            Dim result = Await userAgent.Call(registeredContact.ToString(), Nothing, Nothing, mediaSession)
            Status = If(result, "TALKING", "Talk call failed")
            Return result
        Catch ex As Exception
            Status = "Talk error: " & ex.Message
            Return False
        End Try
    End Function

    Public Sub StopTalk()
        Try
            If userAgent.IsCallActive Then
                userAgent.Hangup()
            ElseIf userAgent.IsCalling OrElse userAgent.IsRinging Then
                userAgent.Cancel()
            End If
            Status = "Talk stopped"
        Catch ex As Exception
            Status = "Stop error: " & ex.Message
        End Try
    End Sub

    Public Sub Shutdown()
        Try
            If userAgent.IsCallActive Then userAgent.Hangup()
        Catch
        End Try
        Try
            sipTransport.Shutdown()
        Catch
        End Try
    End Sub
End Class
