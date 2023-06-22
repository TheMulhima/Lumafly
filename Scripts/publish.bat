if not exist "%~dp0\..\out\*" (
    mkdir "%~dp0\..\out\*"
) else (
    del /Q /F "%~dp0\..\out\*"
)

cd ../Scarab

dotnet publish -r win-x64 -p:PublishSingleFile=true -p:Configuration=Release --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=embedded
copy "%~dp0\..\Scarab\bin\Release\net7.0\win-x64\publish\Scarab.exe" "%~dp0\..\out\Scarab.exe"

dotnet publish -r linux-x64 -p:Configuration=Release -p:PublishSingleFile=true -p:UseAppHost=true --self-contained true
dotnet publish -r osx-x64 -p:Configuration=Release -p:PublishSingleFile=true -p:UseAppHost=true --self-contained true

cd ..

python Scripts/make_mac_app.py Scripts/Scarab.app Scarab/bin/Release/net7.0/osx-x64/publish
7z a "out/windows.zip" "./Scarab/bin/Release/net7.0/win-x64/publish/*"
7z a "out/linux.zip" "./Scarab/bin/Release/net7.0/linux-x64/publish/*"
