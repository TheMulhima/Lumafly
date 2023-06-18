using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Models;
using Mono.Cecil.Cil;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;
using NetSparkleUpdater.UI.Avalonia;
using Scarab.Util;
using Scarab.ViewModels;

namespace Scarab.Services;

public static class Updater
{
    public static async Task CheckUpToDate()
    {
        RemoveOldAUs();
        
        Version? current_version = Assembly.GetExecutingAssembly().GetName().Version;

        Debug.WriteLine($"Current version of installer is {current_version}");

         if (MainWindowViewModel._Debug) return;

        if (OperatingSystem.IsWindows())
            HandleWindowsUpdate();
        else
            await HandleManualUpdate(current_version);
    }

    private static void HandleWindowsUpdate()
    {
        try
        {
            var _sparkle = new SparkleUpdater("https://raw.githubusercontent.com/TheMulhima/Scarab/master/appcast.xml",
                new DSAChecker(SecurityMode.Unsafe))
            {
                UIFactory = new UIFactory(null)
                {
                    HideSkipButton = true,
                    AdditionalReleaseNotesHeaderHTML = """
                    <style> 
                    html {background: #282828; background-color: #282828; color: #dedede;}
                    </style>
                    """,
                    ReleaseNotesHTMLTemplate = """
                    <div style="border-color: #000000; border-width: 3px ">
                        <div style="font-size: 20px; padding: 5px; padding-top: 4px; padding-bottom: 0;">
                            {0} ({1})
                        </div>
                        <div style="padding: 5px; font-size: 16px;">
                            {2}
                        </div>
                    </div>
                    """,
                },
                ShowsUIOnMainThread = true,
                TmpDownloadFilePath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]),
                CustomInstallerArguments = Path.GetFileName(Environment.GetCommandLineArgs()[0]),
                SecurityProtocolType = SecurityProtocolType.Tls12,
                CheckServerFileName = false,
            };

            _sparkle.DownloadHadError += OnDownloadError;
            _sparkle.StartLoop(true, true);
        }
        catch (Exception e)
        {
            Trace.WriteLine(e);
            OnDownloadError(null, "", e);
        }
    }

    private static void OnDownloadError(AppCastItem? item, string path, Exception exception)
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var links = GetUpdateLinks();
            
            await DisplayErrors.DisplayGenericError(Resources.MWVM_UpdateDownloadError_Message, exception);

            var updateLink = (await links)?.updateLink ?? "https://github.com/TheMulhima/Scarab/releases/latest";

            Process.Start(new ProcessStartInfo(updateLink) { UseShellExecute = true });

            ((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)?.Shutdown();
        });
    }

    private static void RemoveOldAUs()
    {
        try
        {
            if (File.Exists(Path.Combine(Environment.CurrentDirectory, "Scarab.AU.exe")))
                File.Delete(Path.Combine(Environment.CurrentDirectory, "Scarab.AU.exe"));
        }
        catch (Exception e)
        {
            Trace.WriteLine(e);
        }
    }

    private static async Task<(string latestReleaseInfo, string updateLink, string changelog)?> GetUpdateLinks()
    {
        try
        {
            const string LatestReleaseLinkJson =
                "https://raw.githubusercontent.com/TheMulhima/Scarab/static-resources/UpdateLinks.json";
            string? latestRelease, updateLink, changelog;
            
            var hc = new HttpClient();
            hc.DefaultRequestHeaders.Add("User-Agent", "Scarab");
            
            CancellationTokenSource cts = new CancellationTokenSource(Timeout);
            var links = await hc.GetStringAsync(new Uri(LatestReleaseLinkJson), cts.Token);
            
            JsonDocument linksDoc = JsonDocument.Parse(links);
            if (!linksDoc.RootElement.TryGetProperty(nameof(latestRelease), out JsonElement latestReleaseLinkElem)) 
                return null;
            if (!linksDoc.RootElement.TryGetProperty(nameof(updateLink), out JsonElement updateLinkElem)) 
                return null;
            if (!linksDoc.RootElement.TryGetProperty(nameof(changelog), out JsonElement changeLogElem)) 
                return null;
            
            latestRelease = latestReleaseLinkElem.GetString();
            updateLink = updateLinkElem.GetString();
            changelog = changeLogElem.GetString();
            if (latestRelease is null || updateLink is null || changelog is null)
                return null;
            
            return (latestRelease, updateLink, changelog);
        }
        catch (Exception e) 
        {
            Trace.WriteLine(e);
            return null;
        }
    }

    private static async Task HandleManualUpdate(Version? current_version)
    {
        try
        {
            var hc = new HttpClient();
            hc.DefaultRequestHeaders.Add("User-Agent", "Scarab");
            
            var links = await GetUpdateLinks();
            if (links is null)
                return;
            
            var cts = new CancellationTokenSource(Timeout);
            var latestRepoInfo = await hc.GetStringAsync(new Uri(links.Value.latestReleaseInfo), cts.Token);

            JsonDocument doc = JsonDocument.Parse(latestRepoInfo);
            if (!doc.RootElement.TryGetProperty("tag_name", out JsonElement tag_elem))
                return;
            string? tag = tag_elem.GetString();
            if (tag is null)
                return;
            if (tag.StartsWith("v"))
                tag = tag[1..];
            if (!Version.TryParse(tag.Length == 1 ? tag + ".0.0.0" : tag, out Version? version))
                return;
            if (version <= current_version)
                return;
            
            string? res = await MessageBoxUtil.GetMessageBoxCustomWindow
            (
                new MessageBoxCustomParams {
                    ButtonDefinitions = new []
                    {
                        new ButtonDefinition { IsDefault = true, IsCancel = true, Name = Resources.MWVM_OutOfDate_GetLatest },
                        new ButtonDefinition { Name = Resources.MWVM_OutOfDate_ContinueAnyways },
                    },
                    ContentTitle = Resources.MWVM_OutOfDate_Title,
                    ContentMessage = string.Format(Resources.MWVM_OutOfDate_Message, version),
                    SizeToContent = SizeToContent.WidthAndHeight
                }
            ).Show();
            if (res == Resources.MWVM_OutOfDate_GetLatest)
            {
                Process.Start(new ProcessStartInfo(links.Value.updateLink) { UseShellExecute = true });
                
                ((IClassicDesktopStyleApplicationLifetime?) Application.Current?.ApplicationLifetime)?.Shutdown();
            }
            else
            {
                Trace.WriteLine($"Installer out of date! Version {current_version} with latest {version}!");
            }
        }
        catch (Exception e)
        {
            Trace.WriteLine(e);
        }
    }
    
    const int Timeout = 15_000;
}