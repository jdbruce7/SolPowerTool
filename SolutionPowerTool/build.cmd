@ECHO OFF
setlocal

SET ScriptsDir=%~dp0\Scripts
SET BinDir=.\Bin
SET Pause=true
SET Register=true

IF "%1"=="/?" GOTO HELP

IF DEFINED DevEnvDir GOTO OPTIONS

IF NOT DEFINED VS100COMNTOOLS GOTO VSNOTFOUND

ECHO.
ECHO ------------------------------------------
ECHO Setting the build environment
ECHO ------------------------------------------
ECHO.
CALL "%VS100COMNTOOLS%\vsvars32.bat" > NUL 
IF ERRORLEVEL 1 GOTO ERROR


:OPTIONS

msbuild /t:build /p:Configuration=Debug SolPowerTool\SolPowerTool.csproj
msbuild /t:build /p:Configuration=Release SolPowerTool\SolPowerTool.csproj

endlocal
