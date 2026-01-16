Imports CommandLine
Imports SharpPcap
Module MainModule
    Friend IPMacs As New Dictionary(Of String, HashSet(Of String))()
    Friend ReadOnly ConsoleLock As New Object()
    Public CaptureDev As ILiveDevice
    Public CurrentOptions As BaseOptions
    Public Out As OutputHelper
    Function Main(Args() As String) As Integer
        Return Parser.Default.ParseArguments(Of InterfacesOptions, MonitorOptions, PingOptions)(Args) _
                  .MapResult(Of PingOptions, MonitorOptions, InterfacesOptions, Integer)(
                      AddressOf PingRunner.Run,
                      AddressOf MonitorRunner.Run,
                      AddressOf InterfacesRunner.Run,
                      Function(errs) 1)
    End Function
End Module
