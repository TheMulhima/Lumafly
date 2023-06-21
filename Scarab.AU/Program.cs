using System.Diagnostics;
using System.Reflection;
using System.Web;

try
{
    // set defaults to allow AU.exe to be run manually just incase
    string ScarabExeName = "Scarab.exe"; 
    string ScarabPath = Environment.CurrentDirectory;
    bool shouldLaunchUpdated = true; // default to launching the updated app

    string fullPath = string.Empty;
    
    // concat all cli args (except first which is the path of this app) to form the full path of scarab which is passed
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
        ScarabExeName = Path.GetFileName(fullPath);
        ScarabPath = Path.GetDirectoryName(fullPath) ?? throw new Exception("Invalid path given in arguments.");
        shouldLaunchUpdated = false; // don't launch the updated app as it'll be handled by NetSparkle
    }

    var originalScarabExe = Path.Combine(ScarabPath, ScarabExeName);
    
    var updatedScarabExeBytes = GetScarabExe() ?? throw new Exception("Unable to get updated Scarab.");
    var updatedScarabFile = Path.Combine(ScarabPath, "Scarab-Update.exe");

    // these actions shouldn't fail as Scarab is running as admin
    File.WriteAllBytes(updatedScarabFile, updatedScarabExeBytes); // create the file
    if (File.Exists(originalScarabExe)) File.Delete(originalScarabExe); // delete the old file
    File.Move(updatedScarabFile, originalScarabExe); // move the new file to the old file's location
    
    Console.WriteLine("Successfully updated Scarab.");
    
    Task.Delay(500).Wait(); // wait a second to show message

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