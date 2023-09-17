using System.Diagnostics;
using System.Reflection;
using System.Web;

try
{
    // set defaults to allow AU.exe to be run manually just incase
    string LumaflyExeName = "Lumafly.exe"; 
    string LumaflyPath = Environment.CurrentDirectory;
    bool shouldLaunchUpdated = true; // default to launching the updated app

    string fullPath = string.Empty;
    
    // concat all cli args (except first which is the path of this app) to form the full path of lumafly which is passed
    // in by the installer. This is done because the path may contain spaces which would be split into multiple args
    for (int i = 1; i < Environment.GetCommandLineArgs().Length; i++)
    {
        Console.WriteLine($"Arg passed {i}: '{Environment.GetCommandLineArgs()[i]}'");
        fullPath += Environment.GetCommandLineArgs()[i] + " ";
    }

    fullPath = fullPath.Trim(); // trim the last space

    // see if full path is a valid file
    if (File.Exists(fullPath))
    {
        LumaflyExeName = Path.GetFileName(fullPath);
        LumaflyPath = Path.GetDirectoryName(fullPath) ?? throw new Exception("Invalid path given in arguments.");
        shouldLaunchUpdated = false; // don't launch the updated app as it'll be handled by NetSparkle
    }

    var originalLumaflyExe = Path.Combine(LumaflyPath, LumaflyExeName);
    
    var updatedLumaflyExeBytes = GetLumaflyExe() ?? throw new Exception("Unable to get updated Lumafly.");
    var updatedLumaflyFile = Path.Combine(LumaflyPath, "Lumafly-Update.exe");

    // these actions shouldn't fail as Lumafly is running as admin
    File.WriteAllBytes(updatedLumaflyFile, updatedLumaflyExeBytes); // create the file
    if (File.Exists(originalLumaflyExe)) File.Delete(originalLumaflyExe); // delete the old file
    File.Move(updatedLumaflyFile, originalLumaflyExe); // move the new file to the old file's location
    
    Console.WriteLine("Successfully updated Lumafly.");
    
    Task.Delay(500).Wait(); // wait a second to show message

    // only relaunch app if it was launched manually
    if (shouldLaunchUpdated)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = LumaflyExeName,
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

// gets Lumafly.exe from embedded resources
static byte[]? GetLumaflyExe()
{
    var asm = Assembly.GetExecutingAssembly();
    foreach (string res in asm.GetManifestResourceNames())
    {
        if (res.EndsWith("Lumafly.exe"))
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