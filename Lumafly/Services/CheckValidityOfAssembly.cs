using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.IO.Abstractions;
using Mono.Cecil;
using Lumafly.Interfaces;

namespace Lumafly.Util;

public class CheckValidityOfAssembly : ICheckValidityOfAssembly
{
    private readonly IFileSystem _fs;
    private readonly ISettings _settings;
    
    public CheckValidityOfAssembly(IFileSystem fs, ISettings settings)
    {
        _fs = fs;
        _settings = settings;
    }
    
    public int? GetAPIVersion(string asmName, out string? gameVersion)
    {
        gameVersion = null;
        try
        {
            string asm = Path.Combine(_settings.ManagedFolder, asmName);
            if (!File.Exists(asm))
                return null;

            using AssemblyDefinition asmDefinition = AssemblyDefinition.ReadAssembly(asm, new()
            {
                ReadingMode = ReadingMode.Deferred
            });

            var constants = asmDefinition.MainModule.GetType("Constants");
            var gameVerField = constants?.Fields.FirstOrDefault(x => x.Name == "GAME_VERSION");

            if(gameVerField is null || !gameVerField.IsLiteral)
                throw new InvalidOperationException("Invalid Assembly-CSharp file");

            gameVersion = (string) gameVerField.Constant;

            var modhooks = asmDefinition.MainModule.GetType("Modding.ModHooks");
            if (modhooks is null)  
                return null;

            var ver = modhooks.Fields.FirstOrDefault(x => x.Name == "_modVersion");
                
            if (ver is null || !ver.IsLiteral) throw new InvalidOperationException("Invalid ModdingAPI file");
            
            return (int) ver.Constant;
        }
        catch (Exception e) 
        {
            Trace.WriteLine(e);
            return null;
        }
    }

    public bool CheckVanillaFileValidity(string vanillaAssembly, string apiAssembly)
    {
        // check if the file is there and the file doesnt have monomod

        if(!_fs.File.Exists(Path.Combine(_settings.ManagedFolder, vanillaAssembly)) ||
            GetAPIVersion(vanillaAssembly, out var vanillaGameVersion) != null)
        {
            return false;
        }

        if(_fs.File.Exists(Path.Combine(_settings.ManagedFolder, vanillaAssembly)))
        {
            GetAPIVersion(apiAssembly, out var apiGameVersion);
            if(apiGameVersion != null)
            {
                if(vanillaGameVersion != apiGameVersion)
                {
                    return false; // Different versions can cause crashes
                }
            }
        }

        return true;
    }
}