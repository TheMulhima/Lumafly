using System.Diagnostics;
using System.Reflection;

try
{
    // we accept 2 optional parameters, the app name and the path to the app
    string ScarabExeName = "Scarab.exe"; // default filename
    string ScarabPath = Environment.CurrentDirectory; // default path to place scarab. Just in case someone opens the AU
    bool shouldLaunchUpdated = true; // default to launching the updated app

    // if provided file name use that
    if (Environment.GetCommandLineArgs().Length >= 2)
    {
        // just in case someone renamed their exe, we'll replace the provided name
        ScarabExeName = Environment.GetCommandLineArgs()[1];
    }
    
    // if provided path use that
    if (Environment.GetCommandLineArgs().Length >= 3)
    {
        ScarabPath = Environment.GetCommandLineArgs()[2];
        // if path is provided is is 99.99% likely that its opened from the app (and not manually)
        // so we can let NetSparkleUpdater handle the relaunch. 
        shouldLaunchUpdated = false;
    }

    var originalScarabExe = Path.Combine(ScarabPath, ScarabExeName);
    
    var updatedScarabExeBytes = GetScarabExe() ?? throw new Exception("Unable to get updated Scarab.");
    var updatedScarabFile = Path.Combine(ScarabPath, "Scarab-Update.exe");

    // these actions shouldn't fail as Scarab is running as admin
    File.WriteAllBytes(updatedScarabFile, updatedScarabExeBytes); // create the file
    if (File.Exists(originalScarabExe)) File.Delete(originalScarabExe); // delete the old file
    File.Move(updatedScarabFile, originalScarabExe); // move the new file to the old file's location
    
    // only relaunch app if it was launched manually
    if (shouldLaunchUpdated)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = ScarabExeName,
            WorkingDirectory = Environment.CurrentDirectory,
            UseShellExecute = true,
        });
    }
}
catch (Exception e)
{
    Console.WriteLine($"Press any key to continue. Unable to complete autoupdate because {e.Message}");
    Console.ReadKey();
}

// gets Scarab.exe from embedded resources
static byte[]? GetScarabExe()
{
    var asm = Assembly.GetExecutingAssembly();
    foreach (string res in asm.GetManifestResourceNames())
    {
        if (res.EndsWith("Scarab.exe"))
        {
            var s = asm.GetManifestResourceStream(res);
            if (s == null) continue;
            var buffer = new byte[s.Length];
            _ = s.Read(buffer, 0, buffer.Length);
            s.Dispose();
            return buffer;
        }
    }
    return null;
}