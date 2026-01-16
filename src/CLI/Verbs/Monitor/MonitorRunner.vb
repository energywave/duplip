Imports System.Net.NetworkInformation
Imports System.Threading
Imports PacketDotNet
Imports SharpPcap

Public Class MonitorRunner
    Public Shared Stats As New StatsInfo
    Protected Shared CaptureThread As Thread
    Protected Shared EndRequest As Boolean = False
    Friend Shared Function Run(Options As MonitorOptions) As Integer
        If Not Options.Check() Then Return 2
        Out = New OutputHelper

        Try
            Dim LastPingStatus As IPStatus? = Nothing
            Dim Devices As CaptureDeviceList = CaptureDeviceList.Instance
            If Devices.Count = 0 Then Throw New ApplicationException("No network devices found. Make sure you have the necessary permissions.")

            CaptureDev = Utils.GetBindToInterface(Options.BindTo, Devices)

            Out.WriteLine($"Monitor IP: {Options.ArgTargetIP}, interface: {CaptureDev.Description} [{MacAddressToString(CaptureDev.MacAddress)}]", False, ConsoleColor.Blue)

            CaptureDev.Open()
            CaptureDev.Filter = "arp"
            AddHandler CaptureDev.OnPacketArrival,
                Sub(sender, e)
                    Dim rawCapture As RawCapture = e.GetPacket()
                    RunnerUtils.HandleArpEvent(rawCapture, Options.TargetIP, Options.Verbose, Stats)
                End Sub
            Stats.TotalElapsed.Start()
            'Starts PCAP from another thread to not have ContextSwitchDeadlock after 60sec in debug mode
            CaptureThread = New Thread(Sub()
                                           CaptureDev.StartCapture()
                                       End Sub) With {.IsBackground = True}
            CaptureThread.Start()

            Out.WriteLine("Monitoring ARP replies... Press ANY KEY to stop.")

            Out.WriteLine("----------------------------------------------")
            Out.WriteLine("In the meantime use suspected IP addresses by opening another terminal or software to stimulate ARP protocol.", False, ConsoleColor.Cyan)
            Out.WriteLine("----------------------------------------------")

            RunnerUtils.OnRunnerStart("monitor")

            AddHandler Console.CancelKeyPress, Sub(sender, e)
                                                   e.Cancel = True
                                                   EndRequest = True
                                               End Sub

            Do Until EndRequest
#If DEBUG Then
                If Console.KeyAvailable Then
                    If EndRequest Then Exit Do
                    Dim Key As ConsoleKeyInfo = Console.ReadKey(True)
                    If Key.Modifiers = ConsoleModifiers.Control AndAlso Key.Key = ConsoleKey.D Then
                        'Insert a debug duplicate IP
                        RunnerUtils.OnNewIpMac("192.168.68.123", "FC:EC:DA:33:44:55", Stats)
                        RunnerUtils.OnNewIpMac("192.168.68.123", "00:15:5d:31:b7:00", Stats)
                    Else
                        Exit Do
                    End If
                End If
#Else
                If Console.KeyAvailable then Exit Do
#End If
                Thread.Sleep(100)
            Loop

            CaptureDev.StopCapture()

            If WriteFinalReport(Stats) Then
                Return 0
            Else
                Return -1
            End If
        Catch ex As Exception
            Out.WriteError(ex)
        Finally
            CaptureDev?.StopCapture()
            CaptureDev?.Close()
            CaptureDev?.Dispose()
            Out.Close()
        End Try
        Return 0
    End Function
End Class
