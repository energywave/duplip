
Public Class BaseOptions
    <CommandLine.Option("v"c, "verbose", Default:=False, HelpText:="Enable verbose output")>
    Public Property Verbose As Boolean
    <CommandLine.Option("l"c, "logfile", [Default]:="", HelpText:="Set the log complete path to mirror screen output")>
    Public Property LogFile As String
    <CommandLine.Option("a"c, "logappend", [Default]:=True, HelpText:="Define if the log has to be appended to the existing file (True) or overwrite it (False)")>
    Public Property LogAppend As Boolean
    Public Overridable Function Check() As Boolean
        MainModule.CurrentOptions = Me
        Return True
    End Function

    Public Overridable Property ShowArpTable As Boolean
End Class
