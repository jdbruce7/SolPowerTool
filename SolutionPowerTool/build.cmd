setlocal
call "C:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\vcvarsall.bat" x86

msbuild /t:build /p:Configuration=Debug SolPowerTool\SolPowerTool.csproj
msbuild /t:build /p:Configuration=Release SolPowerTool\SolPowerTool.csproj

endlocal
