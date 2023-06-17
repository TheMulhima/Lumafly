using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Scarab.Util;

namespace Scarab
{
    [UsedImplicitly]
    internal class Program
    {
        internal static readonly IReadOnlyDictionary<string, string> fontOverrides = new Dictionary<string, string>() {
            ["zh"] = "Source Han Sans SC, Source Han Sans ZH, Noto Sans CJK SC, Noto Sans SC, Microsoft YaHei, Pingfang SC, 苹方-简, 黑体-简, 黑体, Arial"
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
            //Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
            
            SetupLogging();
            
            UrlSchemeHandler.Setup();

            PosixSignalRegistration.Create(PosixSignal.SIGTERM, Handler);
            PosixSignalRegistration.Create(PosixSignal.SIGINT, Handler);
            
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

            // if the log file is too big, archive it
            if (File.Exists(logFile))
            {
                if (new FileInfo(logFile).Length > 5 * 1024 * 1024) // if size > 5 MB
                {
                    var newFile = Path.Combine(Settings.GetOrCreateDirPath(),
                        $"{LoggingFileName} ({DateTime.Now:dd/MM/yyyy, HH-mm-ss}){LoggingFileExtension}");
                    
                    if (File.Exists(newFile)) File.Delete(newFile);
                    
                    // save the old log file
                    File.Move(logFile, newFile);
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

            Trace.WriteLine("Launching...");
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
                // in a try just incase scarab is in a proteced folder
                File.WriteAllText(dir + $"ModInstaller_Error_{date}.log", e.ToString());
            }
            catch (Exception)
            {
                Trace.TraceError("Unable to log to executable folder");
            }
            File.WriteAllText(Path.Combine(Settings.GetOrCreateDirPath(), $"ModInstaller_Error_{date}.log"), e.ToString());

            Trace.Flush();
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
                .UseReactiveUI();

            foreach ((string culture, string fontFamily) in fontOverrides) {
                SetCultureSpecificFontOptions(builder, culture, fontFamily);
            }

            return builder;
        }
    }
}
