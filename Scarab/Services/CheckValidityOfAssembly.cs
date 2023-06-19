using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.IO.Abstractions;
using Mono.Cecil;
using Scarab.Interfaces;

namespace Scarab.Util;

public class CheckValidityOfAssembly : ICheckValidityOfAssembly
{
    private readonly IFileSystem _fs;
    private readonly ISettings _settings;
    
    public CheckValidityOfAssembly(IFileSystem fs, ISettings settings)
    {
        _fs = fs;
        _settings = settings;
    }
    
    public int? GetAPIVersion(string asmName)
    {
        try
        {
            string asm = Path.Combine(_settings.ManagedFolder, asmName);
            if (!File.Exists(asm)) 
                return null;

            using AssemblyDefinition asmDefinition = AssemblyDefinition.ReadAssembly(asm);

            var modhooks = asmDefinition.MainModule.GetType("Modding.ModHooks");
            if (modhooks is null)  
                return null;

            FieldDefinition? ver = modhooks.Fields.FirstOrDefault(x => x.Name == "_modVersion");
                
            if (ver is null || !ver.IsLiteral) throw new InvalidOperationException("Invalid ModdingAPI file");
            
            return (int) ver.Constant;
        }
        catch (Exception e) 
        {
            Trace.WriteLine(e);
            return null;
        }
    }

    public bool CheckVanillaFileValidity(string vanillaAssembly)
    {
        // check if the file is there and the file doesnt have monomod
        return _fs.File.Exists(Path.Combine(_settings.ManagedFolder, vanillaAssembly)) && 
               GetAPIVersion(vanillaAssembly) == null;
    }
}