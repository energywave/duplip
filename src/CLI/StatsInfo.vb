Imports System.Net.NetworkInformation

Public Class StatsInfo
    Public TotalElapsed As New Stopwatch
    Public TotalArpPackets As Long = 0
    Public UniqueIPCount As Long = 0
    Public TotalPingCount As Long = 0
    Public TotalPingSuccess As Long = 0
    Public TotalPingFailed As Long = 0
    Public TotalPingElapsed As TimeSpan = TimeSpan.Zero
    Public MaxPingElapsed As Long = 0
    Public MinPingelapsed As Long = Long.MaxValue

    Public Sub OnSuccessfulPing(elapsedmSec As Long)
        TotalPingCount += 1
        TotalPingSuccess += 1
        TotalPingElapsed = TotalPingElapsed.Add(TimeSpan.FromMilliseconds(elapsedmSec))
        If elapsedmSec > MaxPingElapsed Then MaxPingElapsed = elapsedmSec
        If elapsedmSec < MinPingelapsed Then MinPingelapsed = elapsedmSec
    End Sub
    Public Sub OnFailedPing()
        TotalPingCount += 1
        TotalPingFailed += 1
    End Sub
    Public Sub OnPingResult(Reply As PingReply)
        If Reply.Status = IPStatus.Success Then
            OnSuccessfulPing(Reply.RoundtripTime)
        Else
            OnFailedPing()
        End If
    End Sub
    Public Sub OnArpPacket()
        TotalArpPackets += 1
    End Sub
    Public Sub OnNewIpDetected()
        UniqueIPCount += 1
    End Sub
    Public ReadOnly Property AveragePingTime As TimeSpan
        Get
            If TotalPingSuccess = 0 Then
                Return TimeSpan.Zero
            Else
                Return TimeSpan.FromMilliseconds(TotalPingElapsed.TotalMilliseconds / TotalPingSuccess)
            End If
        End Get
    End Property
End Class
