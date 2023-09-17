using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using Lumafly.Enums;
using Lumafly.Util;

namespace Lumafly
{
    [UsedImplicitly]
    internal class Program
    {
        internal static readonly IReadOnlyDictionary<string, string> fontOverrides = new Dictionary<string, string>() 
        {
            // the avalonia way of specifying embedded fonts
            ["zh"] = "fonts:Noto Sans SC#Noto Sans SC",
            ["en"] = "fonts:Noto Sans#Noto Sans",
            ["es"] = "fonts:Noto Sans#Noto Sans",
            ["pt"] = "fonts:Noto Sans#Noto Sans",
            ["fr"] = "fonts:Noto Sans#Noto Sans",
            ["ru"] = "fonts:Noto Sans#Noto Sans",
        };

        private static TextWriterTraceListener _traceFile = null!;
        internal const string LoggingFile = LoggingFileName + LoggingFileExtension; 
        private const string LoggingFileName = "ModInstaller";
        internal const string LoggingFileExtension = ".log";

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting Lumafly...");
            SetupLogging();
            
            Console.WriteLine("Finding preferred language...");
            SetPreferredLanguage();
            
            Console.WriteLine("Logging sucessfully setup...");
            UrlSchemeHandler.Setup();
            
            PosixSignalRegistration.Create(PosixSignal.SIGTERM, Handler);
            PosixSignalRegistration.Create(PosixSignal.SIGINT, Handler);
            
            Console.WriteLine("Starting Avalonia Setup...");
            
            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception e)
            {
                WriteExceptionToLog(e);
            }
        }

        private static void Handler(PosixSignalContext? c) => Environment.Exit(-1);

        private static void SetupLogging()
        {
            var logFile = Path.Combine
            (
                Settings.GetOrCreateDirPath(),
                LoggingFile
            );
            
            try
            {
                // if the log file is too big, archive it
                if (File.Exists(logFile) && new FileInfo(logFile).Length > 5 * 1024 * 1024) // if size > 5 MB
                {
                    var newFile = Path.Combine(Settings.GetOrCreateDirPath(),
                        $"{LoggingFileName} ({DateTime.Now:dd/MM/yyyy, HH-mm-ss}){LoggingFileExtension}");

                    if (File.Exists(newFile)) File.Delete(newFile);

                    // save the old log file
                    File.Move(logFile, newFile);
                }
            }
            catch (Exception)
            {
                // if it fails, just delete old file
                try
                {
                    if (File.Exists(logFile)) File.Delete(logFile);
                }
                catch (Exception)
                {
                    // if it fails again, just give up
                }
            }

            _traceFile = new TextWriterTraceListener(logFile)
            {
                TraceOutputOptions = TraceOptions.DateTime,
            };

            Trace.AutoFlush = true;

            Trace.Listeners.Add(_traceFile);

            AppDomain.CurrentDomain.UnhandledException += (_, eArgs) =>
            {
                // Can't open a UI as this is going to crash, so we'll save to a log file.
                WriteExceptionToLog((Exception) eArgs.ExceptionObject);
            };

            TaskScheduler.UnobservedTaskException += (_, eArgs) => { WriteExceptionToLog(eArgs.Exception); };

            Trace.TraceInformation("Launching...");
        }

        public static void CloseTraceFile()
        {
            _traceFile.Flush();
            _traceFile.Close();
            _traceFile.Dispose();
        }

        private static void WriteExceptionToLog(Exception e)
        {
            string date = DateTime.Now.ToString("yy-MM-dd HH-mm-ss");

            string dirName = AppContext.BaseDirectory;

            string dir = dirName switch
            {
                // ModInstaller.app/Contents/MacOS/Executable
                "MacOS" => "../../../",
                _ => string.Empty
            };
            
            if (Debugger.IsAttached)
                Debugger.Break();

            Trace.TraceError(e.ToString());

            try
            {
                // in a try just incase lumafly is in a proteced folder
                File.WriteAllText(dir + $"ModInstaller_Error_{date}.log", e.ToString());
            }
            catch (Exception)
            {
                Trace.TraceError("Unable to log to executable folder");
            }
            File.WriteAllText(Path.Combine(Settings.GetOrCreateDirPath(), $"ModInstaller_Error_{date}.log"), e.ToString());

            Trace.Flush();
        }
        
        private static void SetPreferredLanguage()
        {
            try
            {
                SupportedLanguages preferredLanguage;
                var settings = Settings.Load();
                if (settings is { PreferredLanguage: not null })
                {
                    preferredLanguage = settings.PreferredLanguage.Value; // if user has set a preferred language, use that
                }
                else
                {
                    var culture = Thread.CurrentThread.CurrentUICulture;
                    if (!Enum.TryParse(culture.TwoLetterISOLanguageName, out preferredLanguage)) // if culture is supported, set that as preferred
                        preferredLanguage = SupportedLanguages.en; // default to english
                }
                
                // set the culture to the preferred language
                Thread.CurrentThread.CurrentUICulture =
                    new CultureInfo(SupportedLanguagesInfo.SupportedLangToCulture[preferredLanguage]);
            }
            catch (Exception)
            {
                // ignored, worst case it loads in english, but atleast it loads
            }
        }

        private static void SetCultureSpecificFontOptions(AppBuilder builder, string culture, string fontFamily) {
            if (Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == culture) {
                FamilyNameCollection families = new(fontFamily);
                _ = builder.With(new FontManagerOptions() {
                    DefaultFamilyName = families.PrimaryFamilyName,
                    FontFallbacks = families
                        .Skip(1)
                        .Select(name => new FontFallback() {
                            FontFamily = name
                        })
                        .ToList()
                });
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        private static AppBuilder BuildAvaloniaApp() {
            AppBuilder builder = AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI()
                .ConfigureFonts(manager =>
                {
                    manager.AddFontCollection(new EmbeddedFontCollection(
                        new Uri("fonts:Noto Sans SC", UriKind.Absolute),
                        new Uri("avares://Lumafly/Assets/Fonts/NotoSansSC", UriKind.Absolute)));
                    manager.AddFontCollection(new EmbeddedFontCollection(
                        new Uri("fonts:Noto Sans", UriKind.Absolute),
                        new Uri("avares://Lumafly/Assets/Fonts/NotoSans", UriKind.Absolute)));
                });

            foreach ((string culture, string fontFamily) in fontOverrides) {
                SetCultureSpecificFontOptions(builder, culture, fontFamily);
            }

            return builder;
        }
    }
}
