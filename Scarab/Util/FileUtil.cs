using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Scarab.Util;

public static class FileUtil
{
     /// <summary>
     /// Deletes a directory if it exist 
     /// </summary>
    public static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path)) 
            Directory.Delete(path, true);
    }
    
    /// <summary>
    /// Creates a directory if it doesnt exist
    /// </summary>
    public static void CreateDirectory(string path)
    {
        if (!Directory.Exists(path)) 
            Directory.CreateDirectory(path);
    }
    
    /// <summary>
    /// Gets a list of all the files in a directory
    /// </summary>
    public static List<FileInfo> GetAllFilesInDirectory(string path, List<FileInfo>? files = null) => GetAllFilesInDirectory(new DirectoryInfo(path), files);

    private static List<FileInfo> GetAllFilesInDirectory(DirectoryInfo directory, List<FileInfo>? files = null)
    {
        files ??= new List<FileInfo>();
        
        files.AddRange(directory.GetFiles());
        
        var subDirs = directory.GetDirectories();
        subDirs.ToList().ForEach(subDir => GetAllFilesInDirectory(subDir, files));
        
        return files;
    }
    
    /// <summary>
    /// Helper function to copy a directory.
    /// From: https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
    /// </summary>
    public static void CopyDirectory(string sourceDir, string destinationDir, string? excludeDir = null)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        // copy subdirectories by recursively call this method

        foreach (DirectoryInfo subDir in dirs)
        {
            if (subDir.Name == excludeDir)
                continue;
            
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
        
    }
    
}