using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.IO.Abstractions;
using Mono.Cecil;

namespace Scarab.Util;

public static class CheckValidityOfAssemblies
{
    public static int? GetAPIVersion(IFileSystem _fs, string managedFolder, string asmName)
    {
        try
        {
            //TODO: Remove
            var s = Stopwatch.StartNew();
            string asm = Path.Combine(managedFolder, asmName);
            if (!_fs.File.Exists(asm)) return null;
            
            using AssemblyDefinition asmDefinition = AssemblyDefinition.ReadAssembly(asm);

            var modhooks = asmDefinition.MainModule.GetType("Modding.ModHooks");
            if (modhooks is null) return null;
                
            FieldDefinition? ver = modhooks.Fields.FirstOrDefault(x => x.Name == "_modVersion");
                
            if (ver is null || !ver.IsLiteral) throw new InvalidOperationException("Invalid ModdingAPI file");
                
            s.Stop();
            Debug.WriteLine(s.ElapsedMilliseconds);
            return (int) ver.Constant;
        }
        catch (InvalidOperationException e) 
        {
            Trace.WriteLine(e);
            return null;
        }
    }

    public static bool CheckVanillaFileValidity(IFileSystem _fs, string managedFolder, string vanillaAssembly)
    {
        // check if the file is there and the file doesnt have monomod
        return _fs.File.Exists(Path.Combine(managedFolder, vanillaAssembly)) && 
               GetAPIVersion(_fs, managedFolder, vanillaAssembly) == null;
    }
}