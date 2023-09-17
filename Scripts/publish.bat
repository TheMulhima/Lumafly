if not exist "%~dp0\..\out\*" (
    mkdir "%~dp0\..\out"
) else (
    rmdir /S /Q "%~dp0\..\out"
)

cd ../Lumafly

dotnet publish -r win-x64 -p:PublishSingleFile=true -p:Configuration=Release --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=embedded
copy "%~dp0\..\Lumafly\bin\Release\net7.0\win-x64\publish\Lumafly.exe" "%~dp0\..\out\Lumafly.exe"

dotnet publish -r linux-x64 -p:Configuration=Release -p:PublishSingleFile=true -p:UseAppHost=true --self-contained true -p:AppendTargetFrameworkToOutputPath=false -p:OutputPath=bin\$(Configuration)\$(Platform)\ControlCatalog.NetCore.app/Contents/MacOS
dotnet publish -r osx-x64 -p:Configuration=Release -p:PublishSingleFile=true -p:UseAppHost=true --self-contained true

cd ..

cd Scripts
python make_mac_app.py Lumafly.app ../Lumafly/bin/Release/net7.0/osx-x64/publish ../out
cd ..

7z a "out/windows.zip" "./Lumafly/bin/Release/net7.0/win-x64/publish/*"
7z a "out/linux.zip" "./Lumafly/bin/Release/net7.0/linux-x64/publish/*"
