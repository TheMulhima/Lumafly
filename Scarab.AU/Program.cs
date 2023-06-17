using System.Diagnostics;
using System.Reflection;

try
{
    string ScarabExeName = "Scarab.exe"; // default filename

    // if provided file name use that
    if (Environment.GetCommandLineArgs().Length >= 2)
        ScarabExeName = Environment.GetCommandLineArgs()[1];

    var originalScarabExe = Path.Combine(Environment.CurrentDirectory, ScarabExeName);
    
    var updatedScarabExeBytes = GetScarabExe() ?? throw new Exception("Unable to get updated Scarab.");
    var updatedScarabFile = Path.Combine(Environment.CurrentDirectory, "Scarab-Update.exe");

    File.WriteAllBytes(updatedScarabFile, updatedScarabExeBytes); // create the file
    if (File.Exists(originalScarabExe)) File.Delete(originalScarabExe); // delete the old file
    File.Move(updatedScarabFile, originalScarabExe); // move the new file to the old file's location
    

    Process.Start(new ProcessStartInfo
    {
        FileName = ScarabExeName,
        WorkingDirectory = Environment.CurrentDirectory,
        UseShellExecute = true,
    });
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