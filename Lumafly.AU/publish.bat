dotnet publish -r win-x64 -c Release --self-contained true

if not exist "%~dp0out\*" (
    mkdir "%~dp0out\*"
) else (
    del /Q /F "%~dp0out\*"
)

copy "%~dp0bin\Release\net7.0\win-x64\publish\Lumafly.AU.exe" "%~dp0out\Lumafly.AU.exe"