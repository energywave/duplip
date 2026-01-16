Imports System.Net.NetworkInformation
Imports System.Threading
Imports SharpPcap

Public Class PingRunner
    Inherits MonitorRunner
    Friend Overloads Shared Function Run(Options As PingOptions) As Integer
        If Not Options.Check() Then Return 2
        Out = New OutputHelper

        Dim Pinger As New Ping
        Dim SW As New Stopwatch
        Try
            Dim LastPingStatus As IPStatus? = Nothing
            Dim Devices As CaptureDeviceList = CaptureDeviceList.Instance
            If Devices.Count = 0 Then Throw New ApplicationException("No network devices found. Make sure you have the necessary permissions.")

            CaptureDev = Utils.GetBindToInterface(Options.BindTo, Devices)

            Out.WriteLines({$"Monitor IP: {Options.ArgTargetIP}, interface: {CaptureDev.Description} [{MacAddressToString(CaptureDev.MacAddress)}]",
                            $"Ping interval: {Options.PingInterval} msec, Ping timeout: {Options.PingTimeout} msec",
                            ""},
                           False, ConsoleColor.Blue)

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

            Out.WriteLine("Pinging and monitoring ARP replies... Press ANY KEY to stop.")

            RunnerUtils.OnRunnerStart("ping")

            AddHandler Console.CancelKeyPress, Sub(sender, e)
                                                   e.Cancel = True
                                                   EndRequest = True
                                               End Sub

            'If monitoring a specific IP and specified to ping it: periodically ping it to stimulate ARP replies
            Dim RemainingInterval As Integer
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
                SW.Restart()
                Dim Reply = Pinger.Send(Options.TargetIP, Options.PingTimeout)
                Stats.OnPingResult(Reply)
                If Not LastPingStatus.HasValue OrElse Reply.Status <> LastPingStatus.Value Then
                    'Stato del ping cambiato
                    If Reply.Status = IPStatus.Success Then
                        Out.WriteLine($"PING {Options.TargetIP} is ONLINE  - {Reply.RoundtripTime:N0} msec", True, ConsoleColor.Green)
                    Else
                        Out.WriteLine($"PING {Options.TargetIP} is OFFLINE - timeout: {Options.PingTimeout:N0} msec", True, ConsoleColor.Yellow)
                    End If
                    LastPingStatus = Reply.Status
                End If
                If Options.Verbose Then
                    If Reply.Status = IPStatus.Success Then
                        Out.WriteLine($"PING {Options.TargetIP}: reply in {Reply.RoundtripTime:N0} msec", True, ConsoleColor.DarkGray)
                    Else
                        Out.WriteLine($"PING {Options.TargetIP}: {Reply.Status}", True, ConsoleColor.DarkYellow)
                    End If
                End If
                RemainingInterval = Options.PingInterval - SW.ElapsedMilliseconds
                If RemainingInterval > 0 Then Threading.Thread.Sleep(RemainingInterval)
            Loop

            CaptureDev.StopCapture()

            Return If(WriteFinalReport(Stats), 0, -1)
        Catch ex As Exception
            Out.WriteError(ex)
        Finally
            CaptureDev?.StopCapture()
            CaptureDev?.Close()
            CaptureDev?.Dispose()
            Pinger.Dispose()
            Out.Close()
        End Try
        Return 0
    End Function
End Class
