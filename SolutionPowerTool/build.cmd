@ECHO OFF
setlocal

SET ScriptsDir=%~dp0\Scripts
SET BinDir=.\Bin
SET Pause=true
SET Register=true

IF "%1"=="/?" GOTO HELP

IF DEFINED DevEnvDir GOTO BUILD

IF NOT DEFINED VS140COMNTOOLS GOTO VSNOTFOUND

ECHO.
ECHO ------------------------------------------
ECHO Setting the build environment
ECHO ------------------------------------------
ECHO.
CALL "%VS140COMNTOOLS%\vsvars32.bat" > NUL 
IF ERRORLEVEL 1 GOTO ERROR


:BUILD

msbuild /t:build /p:Configuration=Debug SolPowerTool\SolPowerTool.csproj
msbuild /t:build /p:Configuration=Release SolPowerTool\SolPowerTool.csproj
GOTO STOP

:VSNOTFOUND
ECHO Visual Studio 2015 not found.
GOTO STOP

:HELP
ECHO Run this batch file to build Debug and Release configurations.
GOTO STOP


:STOP
endlocal
PAUSE
