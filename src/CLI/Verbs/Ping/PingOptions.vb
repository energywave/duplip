Imports System.Net
Imports CommandLine
Imports CommandLine.Text

<Verb("ping", HelpText:="Ping an IP address and monitor ARP reply from it")>
Public Class PingOptions
    Inherits BaseOptions

    <Value(0, MetaName:="Target IP", HelpText:="IP address you're investigating or ALL (only in monitor verb) to monitor all IP addresses", Required:=True)>
    Public Overridable Property ArgTargetIP As String
    Public Property TargetIP As IPAddress

    <CommandLine.Option("b"c, "bind", Required:=True, HelpText:="Specify the network interface to bind for monitoring. Can be specified by index, MAC Address or name (use interfaces verb to list usable interfaces)")>
    Public Property BindTo As String

    <CommandLine.Option("t"c, "pingtimeout", [Default]:=1000, HelpText:="Timeout in milliseconds for each ping request")>
    Public Property PingTimeout As Integer = 1000
    <CommandLine.Option("i"c, "pinginterval", [Default]:=1000, HelpText:="Interval in milliseconds between each ping request")>
    Public Property PingInterval As Integer = 1000
    <CommandLine.Option("s", "showarptable", [Default]:=False, HelpText:="Show resulting ARP table at the end of the monitoring")>
    Public Overrides Property ShowArpTable As Boolean = False

    Public Overrides Function Check() As Boolean
        Return Utils.HandleErrors(Function()
                                      If String.Compare(ArgTargetIP, "ALL", True) = 0 Then
                                          Throw New ArgumentException("You cannot specify ALL as Target IP for ping verb. Please specify a valid IP address.")
                                      End If
                                      If String.IsNullOrEmpty(ArgTargetIP) Then Throw New ArgumentException("TargetIP cannot be null or empty")
                                      If Not IPAddress.TryParse(ArgTargetIP, TargetIP) Then Throw New FormatException($"The IP is not in a correct format: {ArgTargetIP}")
                                      Return MyBase.Check
                                  End Function)
    End Function

    <Usage>
    Public Shared ReadOnly Property Examples As IEnumerable(Of Example)
        Get
            Dim Res As New List(Of Example) From {
                New Example("Ping and monitor an IP address using the first interface",
                            New PingOptions() With {.ArgTargetIP = "192.168.0.100", .BindTo = "0"}),
                New Example("Ping and monitor an IP address providing the MAC Address of the interface to bind",
                            New PingOptions() With {.ArgTargetIP = "192.168.0.100", .BindTo = "00:11:22:33:44:55"}),
                New Example("Ping and monitor an IP address using the interface with specified name and show all events",
                            New PingOptions() With {.ArgTargetIP = "192.168.0.100", .BindTo = "\Device\NPF_{F5A59C5B-00D2-4E8E-8287-DB3BD8BE4B12}", .Verbose = True})
            }
            Return Res
        End Get
    End Property
End Class

