dotnet publish -r win-x64 -c Release --self-contained true

del /Q /F "%~dp0out\*"

copy "%~dp0bin\Release\net7.0\win-x64\publish\Scarab.AU.exe" "%~dp0out\Scarab.AU.exe"