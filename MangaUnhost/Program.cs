using Ionic.Zip;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using MangaUnhost.Parallelism;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MangaUnhost
{
    static class Program
    {

        public static string MTLPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MTL", "server", "run.bat");
        
        static bool? _MTLAvailable = null;
        public static bool MTLAvailable => _MTLAvailable ??= File.Exists(MTLPath);

        public static TextWriter Writer = null;
        public static bool Debug = Debugger.IsAttached || File.Exists("DEBUG");
        public static string CurrentAssembly => Assembly.GetExecutingAssembly().Location;
        public static string CefDir => Path.Combine(Path.GetDirectoryName(CurrentAssembly), $"runtimes\\win-{(Environment.Is64BitProcess ? "x64" : "x86")}\\native");
        public static string SettingsPath = AppDomain.CurrentDomain.BaseDirectory + "MangaUnhost.ini";


        public static string BrowserSubprocessPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CefSharp.BrowserSubprocess.exe");

        public static Updater Updater = new Updater();

        static string LibWebP => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Environment.Is64BitProcess ? "x64" : "x86", "libwebp.dll");

        /// <summary>
        /// Ponto de entrada principal para o aplicativo.
        /// </summary>
        [STAThread]
        static void Main(string[] Args)
        {
#if DEBUG
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
#endif

            if (Debug)
                Writer = File.CreateText(Path.Combine(Path.GetDirectoryName(CurrentAssembly), "Debug.log"));                
            
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromPlatformFolder);

            string PluginDir = Path.Combine(Path.GetDirectoryName(CurrentAssembly), "Plugins");
            if (Directory.Exists(PluginDir))
            {
                foreach (string PluginPath in Directory.EnumerateFiles(PluginDir, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    Assembly.LoadFrom(PluginPath);
                }
            }

            ServicePointManager.MaxServicePoints = 100;
            ServicePointManager.DefaultConnectionLimit = 100;

            if (Args?.Length > 0)
            {
                foreach(var Arg in Args)
                {
                    
                    var fArg = Arg.TrimStart('/', '\\', '-', '\"').Trim().Trim('\"');
                    var Name = fArg.Contains("=") ? fArg.Split('=').First() : fArg;
                    var Value = fArg.Contains("=") ? fArg.Substring("=") : null;

                    switch (Name.ToLower())
                    {
                        case "parallel":
                            Server.Connect(Value);
                            return;
                        case "updatepath":
                            FinishUpdate(Value);
                            return;
                    }
                }
            }

            new Thread(() => {
                string tempUpdateDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GitHubRelease");
                int i = 0;
                while (Directory.Exists(tempUpdateDir) && i < 5) {
                    try { Directory.Delete(tempUpdateDir, true); } catch { Thread.Sleep(1000); i++; }
                }
            }).Start();

            var PATH = Environment.GetEnvironmentVariable("PATH");
            Environment.SetEnvironmentVariable("PATH", PATH.TrimEnd(';') + ";" + AppDomain.CurrentDomain.BaseDirectory + ";" + CefDir + ";" + Path.GetDirectoryName(LibWebP));


            if (IsRealWindows)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
#if NETCOREAPP
                Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
                Application.SetDefaultFont(new System.Drawing.Font(new System.Drawing.FontFamily("Microsoft Sans Serif"), 8.25f));
#endif

                //new Main();
                //Application.Run(new ImageTest());
            }

            FinishUpdate();
            //WineHelper();
            DependencyUpdater();

            Application.Run(new Main());

            if (Debug)
                Writer.Flush();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            File.WriteAllText("MangaUnhost-FatalError.log", e.ExceptionObject.ToString());
        }

        private static void FinishUpdate()
        {
            if (IsRealWindows)
                Updater.BypassSLL();

            if (Debug)
                return;

            string Result = Updater.FinishUpdate();
            if (Result != null)
            {
                Process.Start(new ProcessStartInfo { FileName = Result, UseShellExecute = true });
                Environment.Exit(0);
            }
        }
        private static void DependencyUpdater(string DataRepo = "https://raw.githubusercontent.com/marcussacana/MangaUnhost/data/")
        {
            if (Debugger.IsAttached) return;

            var TargetVer = new Version("149.0.60.0");
            string CEFName = $"CEFx64-v149.0.60.zip"; // As configured in the GitHub Action
            string CefUrl = $"{DataRepo}{CEFName}";
            string NativeLibsUrl = $"{DataRepo}NativeLibs.zip";

            var OutdatedCef = false;
            if (!File.Exists(BrowserSubprocessPath)) OutdatedCef = true;
            else if (new Version(FileVersionInfo.GetVersionInfo(BrowserSubprocessPath).FileVersion) != TargetVer) OutdatedCef = true;
            
            var OutdatedNative = false;
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LibAPNG.dll"))) OutdatedNative = true;
            if (!File.Exists(LibWebP)) OutdatedNative = true;
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cvextern.dll"))) OutdatedNative = true;
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dxcompiler.dll"))) OutdatedNative = true;
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "opencv_videoio_ffmpeg481_64.dll"))) OutdatedNative = true;

            if (OutdatedCef || OutdatedNative) {
                if (Updater.HaveUpdate()) {
                    Updater.Update();
                    Environment.Exit(0);
                    return;
                }

                long CefSize = 0;
                long NativeSize = 0;
                try {
                    using (var client = new WebClient()) {
                        string updateIni = client.DownloadString(DataRepo + "update.ini");
                        var matchCef = System.Text.RegularExpressions.Regex.Match(updateIni, @"CefSize=(\d+)");
                        if (matchCef.Success) CefSize = long.Parse(matchCef.Groups[1].Value);
                        var matchNative = System.Text.RegularExpressions.Regex.Match(updateIni, @"NativeSize=(\d+)");
                        if (matchNative.Success) NativeSize = long.Parse(matchNative.Groups[1].Value);
                    }
                } catch { }

                if (OutdatedCef) DownloadAndExtract(CefUrl, AppDomain.CurrentDomain.BaseDirectory, CefSize);
                if (OutdatedNative) DownloadAndExtract(NativeLibsUrl, AppDomain.CurrentDomain.BaseDirectory, NativeSize);
            }
        }

        private static void DownloadAndExtract(string Url, string OutDir, long Size = 0)
        {
            var zipPath = Path.Combine(OutDir, Path.GetFileName(Url));
            try {
                using (var client = new WebClient()) {
                    client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                    
                    DownloadingWindow Window = new DownloadingWindow(Url, zipPath, Size);
                    Application.Run(Window);
                }
                using (var Zip = Ionic.Zip.ZipFile.Read(zipPath)) {
                    Zip.ExtractAll(OutDir, Ionic.Zip.ExtractExistingFileAction.OverwriteSilently);
                }
            } catch { }
            finally {
                if (!Debugger.IsAttached && File.Exists(zipPath)) File.Delete(zipPath);
            }
        }

        static string WCRLastCommit = null;
        public static void EnsureWCR()
        {
            try
            {
                string API = "https://api.github.com/repos/marcussacana/WebComicReader/branches";

                string WCR = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WCR");
                string Version = Path.Combine(WCR, "version");

                bool Outdated = false;
                if (!File.Exists(Version))
                    Outdated = true;

                if (WCRLastCommit == null)
                {
                    var Resp = Encoding.UTF8.GetString(API.TryDownload(UserAgent: ProxyTools.UserAgent) ?? new byte[0]);
                    WCRLastCommit = DataTools.ReadJson(Resp.Substring("gh-pages", "protected"), "url").Split('/').Last();
                }

                if (!Outdated)
                {
                    var LocalCommit = File.ReadAllText(Version).Trim();
                    Outdated = WCRLastCommit != LocalCommit;
                }

                if (!Outdated)
                    return;

                string Url = "https://github.com/marcussacana/WebComicReader/archive/gh-pages.zip";
                string SaveAs = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "WCR.zip");
                string ExtractedDir = Path.Combine(Path.GetDirectoryName(CurrentAssembly), "WebComicReader-gh-pages");

                string DbgPath = AppDomain.CurrentDomain.BaseDirectory;


                DownloadingWindow Window = new DownloadingWindow(Url, SaveAs);
                Window.ShowDialog();

                ZipFile Zip = new ZipFile(SaveAs);
                Zip.ExtractAll(Path.GetDirectoryName(CurrentAssembly), ExtractExistingFileAction.OverwriteSilently);
                Zip.Dispose();

                if (Directory.Exists(WCR))
                    Directory.Delete(WCR, true);

                Directory.Move(ExtractedDir, WCR);

                File.Delete(SaveAs);

                File.WriteAllText(Version, WCRLastCommit);
            }
            catch
            {
                return;
            }
        }

        private static void FinishUpdate(string OriginalPath)
        {
            string RunningDir = Path.GetDirectoryName(CurrentAssembly);

            if (!RunningDir.EndsWith("\\")) RunningDir += '\\';
            if (!OriginalPath.EndsWith("\\")) OriginalPath += '\\';

            // Wait for the old process to exit
            while (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(CurrentAssembly)).Count() > 1) {
                Thread.Sleep(500);
            }

            foreach (string File in Directory.GetFiles(RunningDir, "*.*", SearchOption.AllDirectories)) {
                string Base = File.Substring(RunningDir.Length).TrimStart('\\');
                string UpPath = RunningDir + Base;
                string OlPath = OriginalPath + Base;

                if (System.IO.File.Exists(OlPath)) {
                    try { System.IO.File.Delete(OlPath); } catch { }
                }
                
                try {
                    // Ensure the target directory exists
                    string targetDir = Path.GetDirectoryName(OlPath);
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                    
                    System.IO.File.Copy(UpPath, OlPath, true);
                } catch { }
            }

            Process.Start(new ProcessStartInfo { 
                FileName = Path.Combine(OriginalPath, "MangaUnhost.exe"), 
                UseShellExecute = true 
            });
            Environment.Exit(0);
        }

        public static void WineHelper()
        {
            if (IsRealWindows)
                return;

            if (IntPtr.Size == 4)
                return;

            var CMD = Ini.GetConfig("Settings", "WineLauncher", SettingsPath, false);
            if (string.IsNullOrWhiteSpace(CMD))
            {
                MessageBox.Show("The 64bit prefix isn't supported by this program\nPlease, Press OK and type the absolute path to a 32bit prefix.", "MangaUnhost - WINE", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                Form Tmp = new Form();
                Tmp.Size = new System.Drawing.Size(330, 55);
                Tmp.FormBorderStyle = FormBorderStyle.FixedDialog;
                Tmp.StartPosition = FormStartPosition.CenterScreen;
                TextBox TbInput = new TextBox();
                TbInput.Size = new System.Drawing.Size(305, 30);
                Tmp.Controls.Add(TbInput);
                TbInput.Location = new System.Drawing.Point(10, 5);
                TbInput.Text = $"/home/{Environment.UserName}/.win32";
                Tmp.Text = "Type the Prefix Path";

                Tmp.ShowDialog();

                MessageBox.Show("You can change this manually in the MangaUnhost.ini later.", "MangaUnhost - WINE", MessageBoxButtons.OK, MessageBoxIcon.Information);

                CMD = $"export WINEPREFIX=\"{TbInput.Text}\" && nohup wine \"{Path.GetFileName(Application.ExecutablePath)}\" &";
                Ini.SetConfig("Settings", "WineLauncher", CMD, SettingsPath);
            }

            UnixGate.UnixGate.Initialize();
            var hModule = UnixGate.UnixGate.dlopen("libc.so.6", UnixGate.UnixGate.RTLD_NOW);
            var hProc = UnixGate.UnixGate.dlsym(hModule, "system");
            UnixGate.UnixGate.UnixFastCall(hProc, CMD);
            Environment.Exit(0);
        }

        public static bool FirstInstance
        {
            get
            {
                var ProcName = Process.GetCurrentProcess().ProcessName;
                return Process.GetProcessesByName(ProcName).Count() == 1;
            }
        }

        static Assembly LoadFromPlatformFolder(object sender, ResolveEventArgs args)
        {
            string folderPath = Path.GetDirectoryName(CurrentAssembly);
            string assemblyPath = Path.Combine(folderPath, (Environment.Is64BitProcess ? "x64" : "x86"), new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }

        internal static int? RandSeed = null;


        #region Non-Windows Support

        [DllImport(@"kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport(@"kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport(@"kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibraryW(string lpLibrary);

        static bool? isRWin;

        internal static bool IsRealWindows
        {
            get
            {
                if (isRWin.HasValue)
                    return isRWin.Value;

                IntPtr hModule = GetModuleHandle(@"ntdll.dll");
                if (hModule == IntPtr.Zero)
                    isRWin = false;
                else
                {
                    IntPtr fptr = GetProcAddress(hModule, @"wine_get_version");
                    isRWin = fptr == IntPtr.Zero;
                }

                return isRWin.Value;
            }
        }
        #endregion
    }
}
