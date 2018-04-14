@echo off
REM This Framework-dependent deployment (FDD) relies on an installed .NET Core on the target system
REM But you can also create a Self-contained deployment (SCD). See:
REM https://docs.microsoft.com/en-us/dotnet/core/deploying/index#portable-applications

dotnet SymSpell.CommandLine.dll load frequency_dictionary_en_82_765.txt lookup < lookup_input.txt > lookup_output.txt
pause
