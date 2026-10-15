using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Lumafly.Interfaces;
using Lumafly.Models;
using Lumafly.Services;
using Lumafly.Util;
using Mono.Cecil;
using Xunit;

namespace Lumafly.Tests;

public class GameVersionTest : IDisposable
{
    private const string OldGame = "1.5.78.11833";
    private const string NewGame = "1.5.12620";

    private readonly string _managed = Path.Combine(Path.GetTempPath(), "LumaflyTests", Guid.NewGuid().ToString("N"));
    private readonly Settings _settings;

    public GameVersionTest()
    {
        Directory.CreateDirectory(_managed);
        _settings = new Settings(_managed);
        _settings.GameVersion = new(NewGame);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_managed, true);
        }
        // Installer's constructor starts a background CheckAPI that may still hold an assembly open
        catch (IOException) { }
    }

    [Theory]
    [InlineData(NewGame, NewGame, true)]
    [InlineData(NewGame, "1.5.12620.0", true)]
    [InlineData(OldGame, NewGame, false)]
    [InlineData(OldGame, null, false)]
    [InlineData(null, null, false)]
    public void Equal(string a, string b, bool expected)
    {
        Assert.Equal(expected, GameVersion.Equal(a, b));
    }

    /// <summary>
    /// After the game updates, the .v and .m backups still belong to the previous game version.
    /// Swapping either of them into place would break the game.
    /// </summary>
    [Fact]
    public async Task CheckAPIIgnoresBackupsFromAnotherGameVersion()
    {
        WriteAssembly(Path.Combine(_managed, Installer.Current), NewGame);
        WriteAssembly(Path.Combine(_managed, Installer.Vanilla), OldGame);
        WriteAssembly(Path.Combine(_managed, Installer.Modded), OldGame, apiVersion: 77);

        var mods = new InMemoryModSource();
        var installer = CreateInstaller(mods, new HttpClient(), api: ("https://example.invalid/api.zip", 77, ""));

        Assert.False(await installer.CheckAPI());
        Assert.False(mods.HasVanilla);
        Assert.IsType<NotInstalledState>(mods.ApiInstall);
    }

    [Fact]
    public async Task CheckAPIAcceptsBackupsFromSameGameVersion()
    {
        WriteAssembly(Path.Combine(_managed, Installer.Current), NewGame);
        WriteAssembly(Path.Combine(_managed, Installer.Vanilla), NewGame);
        WriteAssembly(Path.Combine(_managed, Installer.Modded), NewGame, apiVersion: 78);

        var mods = new InMemoryModSource();
        var installer = CreateInstaller(mods, new HttpClient(), api: ("https://example.invalid/api.zip", 78, ""));

        Assert.True(await installer.CheckAPI());
        Assert.True(mods.HasVanilla);
        Assert.Equal(new InstalledState(false, new Version(78, 0, 0), false), mods.ApiInstall);
    }

    [Fact]
    public async Task InstallApiRejectsApiBuiltForAnotherGameVersion()
    {
        string current = Path.Combine(_managed, Installer.Current);
        WriteAssembly(current, NewGame);

        byte[] vanillaBytes = await File.ReadAllBytesAsync(current);

        byte[] apiZip = CreateApiZip(OldGame, apiVersion: 77);
        string sha = Convert.ToHexString(SHA256.HashData(apiZip));

        var mods = new InMemoryModSource();
        var hc = new HttpClient(new StaticResponseHandler(apiZip));
        var installer = CreateInstaller(mods, hc, api: ("https://example.invalid/api.zip", 77, sha));

        var error = await Assert.ThrowsAsync<ReadableError>(installer.InstallApi);

        Assert.Contains(OldGame, error.Message);
        Assert.Contains(NewGame, error.Message);
        Assert.Equal(vanillaBytes, await File.ReadAllBytesAsync(current));
        Assert.False(File.Exists(Path.Combine(_managed, Installer.Vanilla)));
        Assert.IsType<NotInstalledState>(mods.ApiInstall);
    }

    private Installer CreateInstaller(IModSource mods, HttpClient hc, (string, int, string) api)
    {
        var fs = new FileSystem();
        return new Installer(_settings, mods, new StubModDatabase(api), fs, hc, 
            new CheckValidityOfAssembly(fs, _settings));
    }

    private static byte[] CreateApiZip(string gameVersion, int apiVersion)
    {
        string asm = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dll");
        try
        {
            WriteAssembly(asm, gameVersion, apiVersion);

            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                archive.CreateEntryFromFile(asm, Installer.Current);

            return zipStream.ToArray();
        }
        finally
        {
            File.Delete(asm);
        }
    }

    private static void WriteAssembly(string path, string gameVersion, int? apiVersion = null)
    {
        using var asm = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("Assembly-CSharp", new Version(0, 0, 0, 0)), "Assembly-CSharp", ModuleKind.Dll);
        ModuleDefinition module = asm.MainModule;

        AddConstant(module, "", "Constants", "GAME_VERSION", module.TypeSystem.String, gameVersion);
        if (apiVersion is not null)
            AddConstant(module, "Modding", "ModHooks", "_modVersion", module.TypeSystem.Int32, apiVersion.Value);

        asm.Write(path);
    }

    private static void AddConstant(ModuleDefinition module, string ns, string typeName, string fieldName,
        TypeReference fieldType, object value)
    {
        var type = new TypeDefinition(ns, typeName, TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
        type.Fields.Add(new FieldDefinition(fieldName,
            FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault,
            fieldType) { Constant = value });
        module.Types.Add(type);
    }

    private sealed class StaticResponseHandler(byte[] body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) });
    }

    private sealed class StubModDatabase((string Url, int Version, string SHA256) api) : IModDatabase
    {
        public List<ModItem> Items { get; } = new();
        public (string Url, int Version, string SHA256) Api { get; } = api;
    }

    private sealed class InMemoryModSource : IModSource
    {
        private readonly object _lock = new();
        private ModState _apiInstall = new NotInstalledState();

        public ModState ApiInstall
        {
            get { lock (_lock) return _apiInstall; }
        }

        public Dictionary<string, InstalledState> Mods { get; } = new();
        public Dictionary<string, NotInModLinksState> NotInModlinksMods { get; } = new();
        public bool HasVanilla { get; set; }

        public Task RecordApiState(ModState st)
        {
            lock (_lock) _apiInstall = st;
            return Task.CompletedTask;
        }

        public ModState FromManifest(Manifest manifest) => new NotInstalledState();
        public Task RecordInstalledState(ModItem item) => Task.CompletedTask;
        public Task RecordUninstall(ModItem item) => Task.CompletedTask;
        public Task Reset() => Task.CompletedTask;

        public Task SetMods(Dictionary<string, InstalledState> mods, Dictionary<string, NotInModLinksState> notInModlinksMods) =>
            Task.CompletedTask;
    }
}
