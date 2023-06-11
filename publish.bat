cd Scarab

 dotnet publish -r win-x64 -p:PublishSingleFile=true -p:Configuration=Release --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=embedded
dotnet publish -r linux-x64 -p:Configuration=Release -p:PublishSingleFile=true -p:UseAppHost=true --self-contained true
dotnet publish -r osx-x64 -p:Configuration=Release -p:PublishSingleFile=true -p:UseAppHost=true --self-contained true

cd ..

rm out/*.zip

python make_app.py Scarab.app Scarab/bin/Release/net7.0/osx-x64/publish
7z a "out/windows.zip" "./Scarab/bin/Release/net7.0/win-x64/publish/"
7z a "out/linux.zip" "./Scarab/bin/Release/net7.0/linux-x64/publish/"
