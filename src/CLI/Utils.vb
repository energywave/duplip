Imports System.IO
Imports System.Net.NetworkInformation
Imports CommandLine.Text
Imports SharpPcap
Imports MacAddressVendorLookup
Imports System.Text
Module Utils
    Private MACVendorInfoProvider As MacVendorBinaryReader = Nothing
    Private MACAddressMatcher As AddressMatcher = Nothing

    ''' <summary>
    ''' Format a MAC address as a string like 00:00:00:00:00:00. If the MAC address matches the binded interface's MAC address, returns "LOCAL".
    ''' </summary>
    ''' <param name="Address">The MAC address to format</param>
    ''' <param name="Dev">Binded devices used to capture packets</param>
    ''' <returns>Returns the formatted MAC address</returns>
    Friend Function MacAddressToString(Address As PhysicalAddress, Optional Dev As ILiveDevice = Nothing) As String
        If Address Is Nothing Then Return ""
        Dim MacBytes As Byte() = Address.GetAddressBytes()
        If Dev IsNot Nothing AndAlso AreMacEqual(MacBytes, Dev.MacAddress.GetAddressBytes) Then
            Return "LOCAL"
        Else
            Return String.Join(":", MacBytes.Select(Function(b) b.ToString("X2")))
        End If
    End Function

    ''' <summary>
    ''' Convert a MAC address from a <see cref="String"/> to a <see cref="PhysicalAddress"/>
    ''' </summary>
    ''' <param name="Address">MAC address in any common form</param>
    ''' <returns>Return a <see cref="PhysicalAddress"/> object</returns>
    ''' <exception cref="FormatException">If the MAC address string is not representing a valid MAC address</exception>
    Friend Function StringToMacAddress(Address As String) As PhysicalAddress
        If Address.Length = 17 Then
            '00:11:22:33:44:55
            Return PhysicalAddress.Parse((Address.Substring(0, 2) & Address.Substring(3, 2) & Address.Substring(6, 2) & Address.Substring(9, 2) & Address.Substring(12, 2) & Address.Substring(15, 2)).ToUpper)
        ElseIf Address.Length = 12 Then
            '001122334455
            Return PhysicalAddress.Parse(Address.ToUpper)
        Else
            Throw New FormatException($"Incorrect MAC address format: {Address}")
        End If
    End Function

    ''' <summary>
    ''' Compare two MAC addresses for equality.
    ''' </summary>
    ''' <param name="MAC1">First MAC address</param>
    ''' <param name="MAC2">Second MAC address</param>
    ''' <returns>Returns if the two mac addresses are equal</returns>
    Friend Function AreMacEqual(MAC1 As Byte(), MAC2 As Byte()) As Boolean
        If (MAC1 Is Nothing) <> (MAC2 Is Nothing) Then Return False
        If (MAC1 Is Nothing) AndAlso (MAC2 Is Nothing) Then Return True
        If MAC1.Length <> MAC2.Length Then Return False
        For I As Integer = 0 To MAC1.Length - 1
            If MAC1(I) <> MAC2(I) Then Return False
        Next
        Return True
    End Function

    ''' <summary>
    ''' Writes a report of duplicate IP addresses and statistics detected during monitoring and returns true if no duplicates were found.
    ''' </summary>
    ''' <param name="Stats">Statistics object to use for the report</param>
    ''' <returns>True if no duplicates where found</returns>
    Friend Function WriteFinalReport(Stats As StatsInfo) As Boolean
        Dim AtLeastOneDuplicate As Boolean = False
        SyncLock ConsoleLock
            Out.WriteLines({"============ FINAL REPORT ============",
                           $"Current date/time: {FormatTimeStamp()}",
                           $"Total monitoring time: {Stats.TotalElapsed.Elapsed:d\.hh\:mm\:ss}"},
                           False, ConsoleColor.Yellow)

            For Each kvp In MainModule.IPMacs
                If kvp.Value.Count > 1 Then
                    Out.WriteLine($"- {FormatIPAddress(kvp.Key)} has the following mac addresses:" & ControlChars.NewLine & FormatMACAddresses(kvp.Value.ToArray, 4), False, ConsoleColor.Red)
                    AtLeastOneDuplicate = True
                End If
            Next
            If Not AtLeastOneDuplicate Then
                Out.WriteLine("No duplicate IP addresses detected.", False, ConsoleColor.Green)
            End If
            Out.WriteLines({$"Total unique IPs detected: {Stats.UniqueIPCount:N0}",
                            $"Total ARP packets processed: {Stats.TotalArpPackets:N0}"})

            If Stats.TotalPingCount > 0 Then
                Out.WriteLines({"Ping Statistics:",
                               $"  Total Pings Sent:        {Stats.TotalPingCount}",
                               $"  Successful Pings:        {Stats.TotalPingSuccess}",
                               $"  Failed Pings:            {Stats.TotalPingFailed}"})
                If Stats.TotalPingSuccess > 0 Then
                    Out.WriteLines({$"  Minimum Ping Time:       {Stats.MinPingelapsed:N0} msec",
                                    $"  Maximum Ping Time:       {Stats.MaxPingElapsed:N0} msec",
                                    $"  Average Ping Time:       {Stats.AveragePingTime.TotalMilliseconds:N2} msec"})
                End If
            End If
            If CurrentOptions.ShowArpTable Then
                Out.WriteLines({"-------------- ARP TABLE -------------",
                                "IP Address      MAC Addresses detected"})
                For Each kvp In MainModule.IPMacs
                    Out.WriteLine($"{kvp.Key,-15} {FormatMACAddresses(kvp.Value.ToArray, 16, 0)}", False, If(kvp.Value.Count > 1, New ConsoleColor?(ConsoleColor.Red), Nothing))
                Next
                Out.WriteLine("------------ END ARP TABLE -----------")
            End If
        End SyncLock
        Return Not AtLeastOneDuplicate
    End Function

    ''' <summary>
    ''' Executes a function that return a Boolean value, catching any exception and printing an error message.
    ''' The result will be the function result or False in case of error.
    ''' </summary>
    ''' <param name="Funct">Function to execute</param>
    ''' <returns>Returns the function result or False in case of error</returns>
    Friend Function HandleErrors(Funct As Func(Of Boolean)) As Boolean
        Try
            Return Funct()
        Catch ex As Exception
            Out.WriteError(ex)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' The log timestamp format used in console outputs.
    ''' </summary>
    Public Function FormatTimeStamp() As String
        Return Date.Now.ToString("yyyy-MM-dd HH:mm:ss ")
    End Function
    ''' <summary>
    ''' Returns a number of space equal to the number of characters of <see cref="FormatTimeStamp()"/> to align content
    ''' </summary>
    Public Function FormatEmptyTimestamp() As String
        Return New String(" "c, 20)
    End Function
    Public Function GetBindToInterface(BindTo As String, Devices As CaptureDeviceList) As ILiveDevice
        Dim InterfaceIndex As Integer
        Dim InterfaceMAC As Byte()
        If Integer.TryParse(BindTo, InterfaceIndex) Then
            ' Bind by index
            If InterfaceIndex < 0 Or InterfaceIndex >= Devices.Count Then
                Throw New ArgumentOutOfRangeException($"The specified interface index is out of range: {BindTo} (0 to {Devices.Count - 1})")
            End If
            Return Devices(InterfaceIndex)
        ElseIf BindTo.Length = 12 AndAlso BindTo.All(Function(c) "0123456789ABCDEFabcdef".Contains(c)) Then
            ' Bind by MAC Address (without separators)
            InterfaceMAC = Enumerable.Range(0, BindTo.Length \ 2).Select(Function(i) Convert.ToByte(BindTo.Substring(i * 2, 2), 16)).ToArray()
            For Each Dev As ILiveDevice In Devices
                If Dev.MacAddress IsNot Nothing AndAlso Dev.MacAddress.GetAddressBytes().SequenceEqual(InterfaceMAC) Then
                    Return Dev
                End If
            Next
            Throw New ArgumentException($"No interface found with the specified MAC Address: {BindTo}")
        ElseIf BindTo.Length = 17 AndAlso (BindTo.Split(":"c).Length = 6 OrElse BindTo.Split("-"c).Length = 6) AndAlso BindTo.Replace(":", "").All(Function(c) "0123456789ABCDEFabcdef".Contains(c)) Then
            ' Bind by MAC Address (with ":" separators)
            BindTo &= ":" 'Per il parsing successivo
            InterfaceMAC = Enumerable.Range(0, BindTo.Length \ 3).Select(Function(i) Convert.ToByte(BindTo.Substring(i * 3, 2), 16)).ToArray()
            For Each Dev As ILiveDevice In Devices
                If Dev.MacAddress IsNot Nothing AndAlso Dev.MacAddress.GetAddressBytes().SequenceEqual(InterfaceMAC) Then
                    Return Dev
                End If
            Next
            Throw New ArgumentException($"No interface found with the specified MAC Address: {BindTo}")
        Else
            'Per nome
            For Each Dev As ILiveDevice In Devices
                If String.Compare(Dev.Name, BindTo, True) = 0 Then Return Dev
            Next
        End If
        Throw New ArgumentException($"No interface found with the specified index, MAC or name: {BindTo}")
    End Function

    ''' <summary>
    ''' Get MAC address vendor info from a <see cref="PhysicalAddress"/> object
    ''' </summary>
    ''' <param name="MACAddress">MAC address to resolve</param>
    ''' <returns>Returns a <see cref="MacVendorInfo"/> object or null if no info is found</returns>
    Public Function GetMACVendorInfo(MACAddress As PhysicalAddress) As MacVendorInfo
        If MACVendorInfoProvider Is Nothing Then
            MACVendorInfoProvider = New MacVendorBinaryReader
            Using ResourceStream = ManufBinResource.GetStream().Result
                MACVendorInfoProvider.Init(ResourceStream).Wait()
            End Using
            MACAddressMatcher = New AddressMatcher(MACVendorInfoProvider)
        End If
        Return MACAddressMatcher.FindInfo(MACAddress)
    End Function
    ''' <summary>
    ''' Get MAC address vendor info from a <see cref="String"/> object
    ''' </summary>
    ''' <param name="MACAddress">MAC address to resolve</param>
    ''' <returns>Returns a <see cref="MacVendorInfo"/> object or null if no info is found</returns>
    Public Function GetMACVendorInfo(MACAddress As String) As MacVendorInfo
        Return GetMACVendorInfo(StringToMacAddress(MACAddress))
    End Function

    ''' <summary>
    ''' Return the representation of a MAC address with the corresponding vendor
    ''' </summary>
    ''' <param name="MACAddress">MAC address to format</param>
    ''' <returns>String formatted to be printed</returns>
    Public Function FormatMACAddress(MACAddress As String) As String
#If anonymize Then
        Static MacAnonymizer As Byte() = Nothing
        If MacAnonymizer Is Nothing Then
            Dim Rnd As New Random()
            ReDim MacAnonymizer(5)
            Rnd.NextBytes(MacAnonymizer)
        End If
        Dim Parts() As String = MACAddress.Split(":")
        Dim MacBytes(5) As Byte
        For Index As Integer = 0 To MacBytes.Length - 1
            MacBytes(Index) = Byte.Parse(Parts(Index), Globalization.NumberStyles.AllowHexSpecifier) Xor MacAnonymizer(Index)
        Next
        Return $"{MacBytes(0):X2}:{MacBytes(1):X2}:{MacBytes(2):X2}:{MacBytes(3):X2}:{MacBytes(4):X2}:{MacBytes(5):X2} {GetMACVendorInfo(MACAddress)?.Organization}"
#Else
        Return $"{MACAddress} {GetMACVendorInfo(MACAddress)?.Organization}"
#End If

    End Function
    ''' <summary>
    ''' Format the IP as a standard string with a fixed length of 15 characters
    ''' </summary>
    ''' <param name="IP">String representing the IP address to be formatted</param>
    ''' <returns>A fixed size string that allow the IP to be aligned</returns>
    Public Function FormatIPAddress(IP As String) As String
        Return $"{IP,-15}"
    End Function
    ''' <summary>
    ''' Format a string containing the IP address, the MAC address and the vendor
    ''' </summary>
    ''' <param name="IP">IP address</param>
    ''' <param name="MACAddress">MAC address (it will be searched for the vendor)</param>
    ''' <returns>Return a standard formatted string</returns>
    Public Function FormatIPAndMACAddress(IP As String, MACAddress As String) As String
        Return $"{FormatIPAddress(IP)} {FormatMACAddress(MACAddress)}"
    End Function
    ''' <summary>
    ''' Format a list of mac addresses by adding the specified number of spaces in front and terminated with new line on each mac address
    ''' </summary>
    ''' <param name="MACS">Array of MAC address strings</param>
    ''' <param name="SpaceChars">The number of blank characters to be put on the left of each MAC address</param>
    ''' <param name="FirstSpaceChars">The number of black characters to be put on the left of the first MAC address. If not set the first will be as <paramref name="SpaceChars"/></param>
    ''' <returns>A list to be used in the log</returns>
    Public Function FormatMACAddresses(MACS() As String, Optional SpaceChars As Integer = 0, Optional FirstSpaceChars As Integer = -1) As String
        Dim Str As New StringBuilder
        Dim IsFirst As Boolean = True
        For Each MAC As String In MACS
            If Str.Length > 0 Then Str.Append(ControlChars.NewLine)
            If IsFirst Then
                If FirstSpaceChars > -1 Then
                    If FirstSpaceChars > 0 Then Str.Append(New String(" "c, FirstSpaceChars))
                ElseIf SpaceChars > 0 Then
                    Str.Append(New String(" "c, SpaceChars))
                End If
                IsFirst = False
            Else
                If SpaceChars > 0 Then Str.Append(New String(" "c, SpaceChars))
            End If
            Str.Append(FormatMACAddress(MAC))
        Next
        Return Str.ToString
    End Function
End Module
