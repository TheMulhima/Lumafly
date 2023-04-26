using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Scarab.Util;

public static class FileAccessLookup
{
    // run once when class is first used
    // remove exe before shutdown
    static FileAccessLookup()
    {
        ((IClassicDesktopStyleApplicationLifetime?) Application.Current?.ApplicationLifetime)!.ShutdownRequested += (_, _) =>
        {
            try
            {
                if (File.Exists(HandleExePath))
                {
                    File.Delete(HandleExePath);
                }
            }
            catch (Exception)
            {
                //ignored
            }
        };
    }

    public static string HandleExePath => Path.Combine(Environment.CurrentDirectory, "handle.exe");
    
    public static Task<string> GetProcessesThatAreLocking(string path)
    {
        if (!OperatingSystem.IsWindows())
            throw new Exception("Can't check if not on windows");
        
        if (!File.Exists(HandleExePath))
        {
            LoadExeFromResources();
        }
        
        return GetNamesOfProcessUsingHandleExe(path);
    }
    
    private static async Task<string> GetNamesOfProcessUsingHandleExe(string path)
    {
        var handleProcess = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "handle.exe",
                Arguments = $"\"{path}\" -nobanner",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            }
        };
        
        handleProcess.Start();           
        await handleProcess.WaitForExitAsync();
        string output = await handleProcess.StandardOutput.ReadToEndAsync();
        
        Debug.WriteLine(output);
        
        string processes = String.Empty;

        foreach (var line in output.Split("\n"))
        {
            if (line.Trim() == "No matching handles found.")
            {
                return string.Empty;
            }

            if (line.Contains("pid"))
            {
                var processName = line.Trim().Split(new[] { "pid" }, StringSplitOptions.None)[0];
                processes += processName + "\n";
            }
        }

        return processes;
    }

    private static void LoadExeFromResources()
    {
        using Stream? s = AvaloniaLocator.Current
            .GetService<Avalonia.Platform.IAssetLoader>()?
            .Open(new Uri("avares://Scarab/Assets/handle.exe"));
        
        if (s == null) throw new Exception("Unable to get handle.exe");
        
        byte[] buffer = new byte[s.Length];
        
        // ReSharper disable once MustUseReturnValue
        s.Read(buffer, 0, buffer.Length);
        s.Dispose(); 
        File.WriteAllBytes(HandleExePath, buffer);
    }
}