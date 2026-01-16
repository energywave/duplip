Imports System.Collections.Concurrent
Imports System.IO
Imports System.Linq.Expressions
Imports System.Threading
Imports CommandLine.Text

Public Class OutputEntry
    Public Lines() As String
    Public ForegroundColor As ConsoleColor?
    Public BackgroundColor As ConsoleColor?

    Public Sub New(Line As String,
                   Optional ForegroundColor As ConsoleColor? = Nothing,
                   Optional BackgroundColor As ConsoleColor? = Nothing)
        Lines = {Line}
        Me.ForegroundColor = ForegroundColor
        Me.BackgroundColor = BackgroundColor
    End Sub
    Public Sub New(Lines() As String,
                   Optional ForegroundColor As ConsoleColor? = Nothing,
                   Optional BackgroundColor As ConsoleColor? = Nothing)
        Me.Lines = Lines
        Me.ForegroundColor = ForegroundColor
        Me.BackgroundColor = BackgroundColor
    End Sub
End Class
Public Class OutputHelper
    Protected MessageQueue As New ConcurrentQueue(Of OutputEntry)
    Protected DequeueThread As Thread
    Protected Log As IO.StreamWriter = Nothing
    Protected LogFilename As String
    Protected ExitRequest As Boolean = False
    Public Sub New()
        If Not String.IsNullOrEmpty(CurrentOptions.LogFile) Then
            LogFilename = Path.GetFullPath(CurrentOptions.LogFile)
            Log = New IO.StreamWriter(LogFilename, CurrentOptions.LogAppend)
        End If

        DequeueThread = New Thread(Sub()
                                       Try
                                           Dim Entry As OutputEntry = Nothing
                                           Do Until ExitRequest AndAlso MessageQueue.TryPeek(Entry) = False
                                               'Skip if we don't have anything to write
                                               If Not MessageQueue.TryDequeue(Entry) Then
                                                   Thread.Sleep(100)
                                                   Continue Do
                                               End If
                                               'From here we have something to write
                                               If Not Entry.ForegroundColor.HasValue AndAlso Not Entry.BackgroundColor.HasValue Then
                                                   Console.ResetColor()
                                               Else
                                                   If Entry.ForegroundColor.HasValue Then Console.ForegroundColor = Entry.ForegroundColor.Value
                                                   If Entry.BackgroundColor.HasValue Then Console.BackgroundColor = Entry.BackgroundColor.Value
                                               End If
                                               If Entry.Lines IsNot Nothing Then
                                                   For Each Line As String In Entry.Lines
                                                       Console.WriteLine(Line)
                                                       Log?.WriteLine(Line)
                                                       Log?.Flush()
                                                   Next
                                               End If
                                           Loop
                                       Catch

                                       End Try
                                   End Sub) With {.IsBackground = True}
        DequeueThread.Start()
        WriteHeader()
    End Sub

    Public Sub WriteLine(Line As String,
                         Optional AddTimeStamp As Boolean = False,
                         Optional ForegroundColor As ConsoleColor? = Nothing,
                         Optional BackgroundColor As ConsoleColor? = Nothing)
        If AddTimeStamp Then
            Line = Line.Replace(ControlChars.NewLine, ControlChars.NewLine & Utils.FormatEmptyTimestamp)
        End If
        Try
            MessageQueue.Enqueue(New OutputEntry(If(AddTimeStamp, FormatTimeStamp() & Line, Line), ForegroundColor, BackgroundColor))
        Catch ex As InvalidOperationException
            'Queue closed
        End Try
    End Sub
    Public Sub WriteLines(Lines() As String,
                          Optional AddTimeStamp As Boolean = False,
                          Optional ForegroundColor As ConsoleColor? = Nothing,
                          Optional BackgroundColor As ConsoleColor? = Nothing)
        If AddTimeStamp Then
            Lines(0) = FormatTimeStamp() & Lines(0)
            For I As Integer = 1 To Lines.Length - 1
                Lines(I) = FormatEmptyTimestamp() & Lines(I)
            Next
        End If
        Try
            MessageQueue.Enqueue(New OutputEntry(Lines, ForegroundColor, BackgroundColor))
        Catch ex As InvalidOperationException
            'Queue closed
        End Try
    End Sub

    ''' <summary>
    ''' Writes the application header
    ''' </summary>
    Public Sub WriteHeader()
        WriteLines({HeadingInfo.Default, CopyrightInfo.Default, ""})
    End Sub

    ''' <summary>
    ''' Write the specified error to the output. If the <see cref="CurrentOptions"/> has the Verbose options set the complete errore will be printed
    ''' </summary>
    ''' <param name="Ex"></param>
    Public Sub WriteError(Ex As Exception)
        WriteLine("ERROR: " & Ex.Message, True, ConsoleColor.Red)
        If CurrentOptions.Verbose Then
            WriteLine(Ex.ToString, False, ConsoleColor.DarkRed)
        End If
    End Sub
    ''' <summary>
    ''' Close the log and wait the thread to end. To be called before to terminate
    ''' </summary>
    Public Sub Close()
        ExitRequest = True
        DequeueThread.Join(2000)
        If Log IsNot Nothing Then
            Log.Flush()
            Log.Dispose()
            Log = Nothing
        End If
    End Sub
End Class
