Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices

' Le informazioni generali relative a un assembly sono controllate dal seguente 
' set di attributi. Modificare i valori di questi attributi per modificare le informazioni
' associate a un assembly.

' Controllare i valori degli attributi degli assembly

<Assembly: AssemblyTitle("Duplip")>
<Assembly: AssemblyDescription("Duplicate IP detector console application")>
<Assembly: AssemblyCompany("Henrik Sozzi")>
<Assembly: AssemblyProduct("Duplip")>
<Assembly: AssemblyCopyright("Copyright © Henrik Sozzi 2026")>
<Assembly: AssemblyTrademark("")>

<Assembly: ComVisible(False)>

'Se il progetto viene esposto a COM, il GUID seguente verrà usato come ID del typelib
<Assembly: Guid("bfb5635c-5fea-4d4c-b12d-615c60d27c11")>

' Le informazioni sulla versione di un assembly sono costituite dai seguenti quattro valori:
'
'      Versione principale
'      Versione secondaria
'      Numero di build
'      Revisione
'

<Assembly: AssemblyVersion("1.2.1.6")>
<Assembly: AssemblyFileVersion("1.2.1.6")>
'CHANGELOG
'1.0.0.0 - 12/01/2026
' - Initial release
'1.0.1.2 - 13/01/2026
' - Added logfile, logappend and showarptable options
' - Various output refinements
'1.0.1.3 - 14/01/2026
' - Added an output when a new IP is detected using ARP packets
' - Reformatted event lines
'1.1.0.4 - 14/01/2026
' - Added MAC Address vendor lookup
'1.2.0.5 - 15/01/2026
' - Created the OutputHelper class to streamline the output to be on a single writer thread
' - Refinements in code and output format
'1.2.1.6 - 16/01/2026
' - Added Debug Anonymized solution configuration for making screenshots
' - Added CTRL+D to insert fictious duplicate IP for making screenshots and debug
' - Corrected some alignments