Imports SharpPcap

Public Class InterfacesRunner

    Friend Shared Function Run(Options As InterfacesOptions) As Integer
        If Not Options.Check() Then Return 2
        Out = New OutputHelper

        Dim Index As Integer
        Try
            Dim devices As CaptureDeviceList = CaptureDeviceList.Instance
            If devices.Count = 0 Then
                Out.WriteLine("Interfaces: No network devices found. Make sure you have the necessary permissions.")
                Return 0
            End If
            Out.WriteLine($"Interfaces: {devices.Count}")
            For Each Dev As ILiveDevice In devices
                Out.WriteLines({$"- INTERFACE {Index} [{MacAddressToString(Dev.MacAddress)}]: {Dev.Description}",
                                $"  Name: {Dev.Name}",
                                ""})

                Index += 1
            Next

        Catch ex As Exception
            Out.WriteError(ex)
        Finally
            Out.Close()
        End Try
        Return 0

    End Function

End Class
