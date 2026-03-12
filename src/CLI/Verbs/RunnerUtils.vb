Imports System.Net
Imports PacketDotNet
Imports SharpPcap

Public Class RunnerUtils
    Friend Shared Sub HandleArpEvent(Capture As RawCapture, TargetIP As IPAddress, Verbose As Boolean, Stats As StatsInfo)
        Dim p As Packet = Packet.ParsePacket(Capture.LinkLayerType, Capture.Data)

        If p IsNot Nothing AndAlso TypeOf p.PayloadPacket Is ArpPacket Then
            Dim arp As ArpPacket = DirectCast(p.PayloadPacket, ArpPacket)
            Dim SenderIP As String = arp.SenderProtocolAddress.ToString
            Dim SenderMAC As String = MacAddressToString(arp.SenderHardwareAddress, CaptureDev)

            'If the sender IP is 0.0.0.0 we discard the packet as it is a normal condition in some cases (e.g. when a device is not connected to the network yet)
            If SenderIP = "0.0.0.0" Then Return

            'If we have a target IP and the IP is not the wanted one: exit
            If TargetIP IsNot Nothing AndAlso TargetIP.ToString <> SenderIP Then Return
            'Ignores packets sent from the local interface
            If AreMacEqual(arp.SenderHardwareAddress.GetAddressBytes(), CaptureDev.MacAddress.GetAddressBytes()) Then Exit Sub

            Select Case arp.Operation
                Case ArpOperation.Response
                    SyncLock ConsoleLock
                        If Verbose Then
                            Out.WriteLine($"ARP REP {SenderIP,-15} {FormatMACAddress(SenderMAC)}", True, ConsoleColor.Cyan)
                        End If
                        OnNewIpMac(SenderIP, SenderMAC, Stats)
                    End SyncLock
                Case ArpOperation.Request
                    If arp.SenderProtocolAddress.Equals(arp.TargetProtocolAddress) Then
                        ' Gratuitous ARP
                        SyncLock ConsoleLock
                            If Verbose Then
                                Out.WriteLine($"ARP GRA {SenderIP,-15} {FormatMACAddress(SenderMAC)}", True, ConsoleColor.Cyan)
                            End If
                            OnNewIpMac(SenderIP, SenderMAC, Stats)
                        End SyncLock
                    Else
                        'ARP Request
                        SyncLock ConsoleLock
                            If Verbose Then
                                Out.WriteLine($"ARP REQ {SenderIP,-15} {FormatMACAddress(SenderMAC)} --> {arp.TargetProtocolAddress}", True, ConsoleColor.Cyan)
                            End If
                            OnNewIpMac(SenderIP, SenderMAC, Stats)
                        End SyncLock
                    End If
            End Select
        End If
    End Sub

    ''' <summary>
    ''' Handle a new IP-MAC mapping and check for duplicates. Must be called inside a lock.
    ''' If a duplicate is found, an alert is printed to the console and the return value is True.
    ''' </summary>
    ''' <param name="SenderIP">The Sender IP address</param>
    ''' <param name="SenderMAC">The sender MAC address</param>
    ''' <returns>Returns True if a duplicate is found</returns>
    Friend Shared Function OnNewIpMac(SenderIP As String, SenderMAC As String, Stats As StatsInfo) As Boolean
        Dim Macs As HashSet(Of String)

        Stats.OnArpPacket()

        If Not IPMacs.ContainsKey(SenderIP) Then
            Stats.OnNewIpDetected()
            IPMacs.Add(SenderIP, New HashSet(Of String)())
            Out.WriteLine($"New IP: {FormatIPAndMACAddress(SenderIP, SenderMAC)}", True)
        End If
        Macs = IPMacs(SenderIP)

        If Macs.Add(SenderMAC) AndAlso Macs.Count > 1 Then
            'No lock as it's called inside a lock
            Out.WriteLine($"=== ALERT DUPLICATE IP {SenderIP} ==={ControlChars.NewLine}{FormatMACAddresses(Macs.ToArray, 4)}",
                          True, ConsoleColor.Red)
            Return True
        End If
        Return False
    End Function

    Friend Shared Sub OnRunnerStart(OperationName As String)
        Out.WriteLine($"{OperationName} started", True)
#If anonymize Then
        Out.WriteLine("MAC address are anonymized for screenshot purposes", False, ConsoleColor.Magenta)
#End If
    End Sub
End Class
