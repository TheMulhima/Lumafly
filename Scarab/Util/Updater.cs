using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.Interfaces;
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
            var _sparkle = new SparkleUpdater($"https://raw.githubusercontent.com/TheMulhima/Scarab/master/appcast.xml", new DSAChecker(SecurityMode.Unsafe))
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

            _sparkle.StartLoop(true, true);

            _sparkle.DownloadHadError += OnDownloadError;
        }
        catch(Exception e)
        {
            Trace.WriteLine(e);
            OnDownloadError(null, "", e);
        }
    }

    private static void OnDownloadError(AppCastItem? item, string path, Exception exception)
    {
        Dispatcher.UIThread.InvokeAsync(() => DisplayErrors.DisplayGenericError("Updating Scarab", 
            $"Please try to get release manually", exception));
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

    private static async Task HandleManualUpdate(Version? current_version)
    {
        const int Timeout = 15_000;
        const string LatestReleaseLinkJson =
            "https://raw.githubusercontent.com/TheMulhima/Scarab/static-resources/UpdateLinks.json";
        string json, updateLink, changelog;
        
        try
        {
            var hc = new HttpClient();
            
            hc.DefaultRequestHeaders.Add("User-Agent", "Scarab");
            CancellationTokenSource cts = new CancellationTokenSource(Timeout);
            var links = await hc.GetStringAsync(new Uri(LatestReleaseLinkJson), cts.Token);
            
            JsonDocument linksDoc = JsonDocument.Parse(links);
            if (!linksDoc.RootElement.TryGetProperty("latestRelease", out JsonElement latestReleaseLinkElem)) 
                return;
            if (!linksDoc.RootElement.TryGetProperty(nameof(updateLink), out JsonElement updateLinkElem)) 
                return;
            if (!linksDoc.RootElement.TryGetProperty(nameof(changelog), out JsonElement changeLogElem)) 
                return;
            
            string? latestReleaseLink = latestReleaseLinkElem.GetString();
            string? _updateLink = updateLinkElem.GetString();
            string? _changelog = changeLogElem.GetString();
            if (latestReleaseLink is null || _updateLink is null || _changelog is null)
                return;
            
            cts = new CancellationTokenSource(Timeout);
            json = await hc.GetStringAsync(new Uri(latestReleaseLink), cts.Token);
            updateLink = _updateLink;
        }
        catch (Exception) {
            return;
        }
        JsonDocument doc = JsonDocument.Parse(json);
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
            Process.Start(new ProcessStartInfo(updateLink) { UseShellExecute = true });
            
            ((IClassicDesktopStyleApplicationLifetime?) Application.Current?.ApplicationLifetime)?.Shutdown();
        }
        else
        {
            Trace.WriteLine($"Installer out of date! Version {current_version} with latest {version}!");
        }
    }
}