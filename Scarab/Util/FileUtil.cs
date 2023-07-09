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
    
}