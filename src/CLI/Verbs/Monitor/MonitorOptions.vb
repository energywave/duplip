Imports System.Net
Imports System.Net.NetworkInformation
Imports System.Threading
Imports CommandLine
Imports CommandLine.Text
Imports SharpPcap

<Verb("monitor", HelpText:="Constantly monitor an IP address")>
Public Class MonitorOptions
    Inherits BaseOptions

    <Value(0, MetaName:="Target IP", HelpText:="IP address you're investigating or ALL (only in monitor verb) to monitor all IP addresses", Required:=True)>
    Public Overridable Property ArgTargetIP As String
    Public Property TargetIP As IPAddress

    <CommandLine.Option("b"c, "bind", Required:=True, HelpText:="Specify the network interface to bind for monitoring. Can be specified by index, MAC Address or name (use interfaces verb to list usable interfaces)")>
    Public Property BindTo As String
    <CommandLine.Option("s", "showarptable", [Default]:=False, HelpText:="Show resulting ARP table at the end of the monitoring")>
    Public Overrides Property ShowArpTable As Boolean = False
    Public Overrides Function Check() As Boolean
        Return Utils.HandleErrors(Function()
                                      If String.IsNullOrEmpty(ArgTargetIP) Then Throw New ArgumentException("TargetIP cannot be null or empty")
                                      If String.Compare(ArgTargetIP, "ALL", True) = 0 Then
                                          TargetIP = Nothing
                                      Else
                                          If Not IPAddress.TryParse(ArgTargetIP, TargetIP) Then Throw New FormatException($"The IP is not in a correct format: {ArgTargetIP}")
                                      End If
                                      Return MyBase.Check
                                  End Function)
    End Function

    <Usage()>
    Public Shared ReadOnly Property Examples As IEnumerable(Of Example)
        Get
            Dim Res As New List(Of Example) From {
                New Example("Monitor for all duplicates IP using the first interface",
                            New MonitorOptions() With {.ArgTargetIP = "all", .BindTo = "0"}),
                New Example("Monitor for duplicate IP of a specific address providing the MAC Address of the interface to bind",
                            New MonitorOptions() With {.ArgTargetIP = "192.168.0.100", .BindTo = "00:11:22:33:44:55"}),
                New Example("Monitor for all duplicates IP using the interface with specified name and show all events",
                            New MonitorOptions() With {.ArgTargetIP = "all", .BindTo = "\Device\NPF_{F5A59C5B-00D2-4E8E-8287-DB3BD8BE4B12}", .Verbose = True})
            }
            Return Res
        End Get
    End Property
End Class
