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
        public static string CefDir => Path.Combine(Path.GetDirectoryName(CurrentAssembly), (Environment.Is64BitProcess ? "x64" : "x86"));
        public static string SettingsPath = AppDomain.CurrentDomain.BaseDirectory + "MangaUnhost.ini";


        public static string BrowserSubprocessPath => Path.Combine(CefDir, "CefSharp.BrowserSubprocess.exe");

        public static GitHub Updater = new GitHub("Marcussacana", "MangaUnhost");

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
                    }
                }
            }

            var PATH = Environment.GetEnvironmentVariable("PATH");
            Environment.SetEnvironmentVariable("PATH", PATH.TrimEnd(';') + ";" + OCVDir + ";" + Path.GetDirectoryName(LibWebP));


            if (IsRealWindows)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                //new Main();
                //Application.Run(new ImageTest());
            }

            TrampolineUpdate();
            FinishUpdate();
            //WineHelper();
            CefUpdater();
            OcvUpdater();

            UnlockHeaders();

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
                Process.Start(Result);
                Environment.Exit(0);
            }
        }

        private static void TrampolineUpdate()
        {
            if (Debugger.IsAttached) return;

            // 1. Checar e Instalar .NET 10
            if (!CheckDotNet10Installed())
            {
                // Silently download and install .NET 10
                string installerUrl = "https://download.visualstudio.microsoft.com/download/pr/9dbda88c-10bc-4bb5-bb26-d6b38c2084c7/582bf4ed2e31e5f52fba4a51e59df1f0/windowsdesktop-runtime-10.0.0-preview.6.24328.2-win-x64.exe"; // Exemplo, pode ser a URL real do .NET 10 desktop runtime
                string installerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dotnet_installer.exe");
                try {
                    new WebClient().DownloadFile(installerUrl, installerPath);
                    var p = Process.Start(installerPath, "/install /quiet /norestart");
                    p.WaitForExit();
                } catch { }
                finally {
                    if (File.Exists(installerPath)) File.Delete(installerPath);
                }
            }

            // 2. Limpar lixo antigo (CEF x86/x64)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] folders = { "x86", "x64" };
            foreach (var folder in folders)
            {
                string path = Path.Combine(baseDir, folder);
                if (Directory.Exists(path))
                {
                    try { Directory.Delete(path, true); } catch { }
                }
            }

            // 3. Baixar zip moderno em pedaços do data repo
            string baseUrl = "http://127.0.0.1:8000/update.zip";
            string zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update_modern.zip");

            try {
                using (var outputStream = File.Create(zipPath))
                {
                    int part = 0;
                    while (true)
                    {
                        string url = string.Format("{0}.{1:D3}", baseUrl, part);
                        try
                        {
                            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                            req.Method = "GET";
                            using (var resp = req.GetResponse())
                            using (var stream = resp.GetResponseStream())
                            {
                                stream.CopyTo(outputStream);
                            }
                            part++;
                        }
                        catch (WebException ex)
                        {
                            var httpResponse = ex.Response as HttpWebResponse;
                            if (httpResponse != null && httpResponse.StatusCode == HttpStatusCode.NotFound)
                                break;
                            break;
                        }
                        catch
                        {
                            break;
                        }
                    }
                }

                if (new FileInfo(zipPath).Length > 100) { // Pelo menos 1KB baixado
                    using (var Zip = Ionic.Zip.ZipFile.Read(zipPath)) {
                        Zip.ExtractAll(AppDomain.CurrentDomain.BaseDirectory, Ionic.Zip.ExtractExistingFileAction.OverwriteSilently);
                    }
                    Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    Environment.Exit(0);
                }
            } catch { }
            finally {
                if (File.Exists(zipPath)) File.Delete(zipPath);
            }
        }

        static bool CheckDotNet10Installed()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string desktopRuntimePath = Path.Combine(programFiles, "dotnet", "shared", "Microsoft.WindowsDesktop.App");
            if (Directory.Exists(desktopRuntimePath))
            {
                foreach (var dir in Directory.GetDirectories(desktopRuntimePath))
                {
                    if (Path.GetFileName(dir).StartsWith("10.0.")) return true;
                }
            }
            return false;
        }

        private static string OCVDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Environment.Is64BitProcess ? "x64" : "x86");
        private static void OcvUpdater(string OcvRepo = "https://github.com/marcussacana/MangaUnhost/raw/data/")
        {
            var AltRepo = "https://github.com/marcussacana/MangaUnhost/blob/data/";

            var VerFlag = Path.Combine(OCVDir, "cvextern.dll");

            var Outdated = false;
            if (!File.Exists(VerFlag) || (new Version(FileVersionInfo.GetVersionInfo(VerFlag).FileVersion) != new Version(4, 8, 1, 5350)))
                Outdated = true;

            if (!Outdated)
                return;

            var OCVName = Environment.Is64BitProcess ? "opencv-x64.zip" : "opencv-x86.zip";
            string Url = $"{OcvRepo}{OCVName}?raw=true";
            string SaveAs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, OCVName);

            if (!File.Exists(SaveAs))
            {
                try
                {

                    DownloadingWindow Window = new DownloadingWindow(Url, SaveAs);
                    Application.Run(Window);
                }
                catch
                {
                    if (OcvRepo != AltRepo)
                    {
                        CefUpdater(AltRepo);
                        return;
                    }
                    throw;
                }
            }

            if (!Directory.Exists(OCVDir))
                Directory.CreateDirectory(OCVDir);

            ZipFile Zip = new ZipFile(SaveAs);
            Zip.ExtractAll(OCVDir, ExtractExistingFileAction.OverwriteSilently);
            Zip.Dispose();

            if (!Debugger.IsAttached)
                File.Delete(SaveAs);
        }

        private static void CefUpdater(string CefRepo = "https://github.com/marcussacana/MangaUnhost/raw/data/")
        {
            if (Debugger.IsAttached)
                return;

            var AltRepo = "https://github.com/marcussacana/MangaUnhost/blob/data/";

            var VerFilePath = Path.Combine(CefDir, "version.txt");
            bool Outdated = false;
            if (!File.Exists(BrowserSubprocessPath))
                Outdated = true;

            var TargetVer = new Version(144, 0, 120, 0);

            if (!Outdated)
            {
                var VerStr = FileVersionInfo.GetVersionInfo(BrowserSubprocessPath).FileVersion;
                if (new Version(VerStr) != TargetVer)
                {
                    Outdated = true;
                }
            }

            if (File.Exists(VerFilePath) && File.ReadAllText(VerFilePath).Trim() == TargetVer.ToString())
                Outdated = false;

			var WebP = false;
			
            if (!File.Exists(LibWebP))
				WebP = true;

            if (!Outdated && !WebP)
                return;

            string CEFName = $"CEF{(Environment.Is64BitProcess ? "x64" : "x86")}-v{TargetVer}.zip";
            string Url = $"{CefRepo}{CEFName}?raw=true";
            string SaveAs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CEFName);
			
			if (!Outdated && WebP){
				Url = $"{CefRepo}libWebp{(Environment.Is64BitProcess ? "X64" : "X86")}.zip?raw=true";
				CEFName = "libWebp.zip";
				SaveAs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CEFName);
			}

            string DbgPath = AppDomain.CurrentDomain.BaseDirectory;

            for (int i = 0; i < 4; i++)
            {
                DbgPath = Path.GetDirectoryName(DbgPath);

                if (Debug && File.Exists(Path.Combine(DbgPath, CEFName)))
                {
                    try
                    {
                        ZipFile DZip = new ZipFile(Path.Combine(DbgPath, CEFName));
                        DZip.ExtractAll(Path.GetDirectoryName(CurrentAssembly), ExtractExistingFileAction.OverwriteSilently);
                        DZip.Dispose();
                        return;
                    }
                    catch
                    {
                        if (!Debugger.IsAttached)
                            File.Delete(Path.Combine(DbgPath, CEFName));
                    }
                }
            }

            if (!File.Exists(SaveAs))
            {
                try
                {

                    DownloadingWindow Window = new DownloadingWindow(Url, SaveAs);
                    Application.Run(Window);
                }
                catch
                {
                    if (CefRepo != AltRepo)
                    {
                        CefUpdater(AltRepo);
                        return;
                    }
                    throw;
                }
            }

            ZipFile Zip = new ZipFile(SaveAs);
            Zip.ExtractAll(Path.GetDirectoryName(CurrentAssembly), ExtractExistingFileAction.OverwriteSilently);
            Zip.Dispose();

			if (Outdated)
				File.WriteAllText(VerFilePath, TargetVer.ToString());

            if (!Debugger.IsAttached)
                File.Delete(SaveAs);
			
			if (Outdated && WebP){
                CefUpdater(AltRepo);
				return;
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

        /// <summary>
        /// We aren't kids microsoft, we shouldn't need this
        /// </summary>
        public static void UnlockHeaders()
        {
            var tHashtable = typeof(WebHeaderCollection).Assembly.GetType("System.Net.HeaderInfoTable")
                            .GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                            .Where(x => x.FieldType.Name == "Hashtable").Single();

            var Table = (Hashtable)tHashtable.GetValue(null);
            foreach (var Key in Table.Keys.Cast<string>().ToArray())
            {
                var HeaderInfo = Table[Key];
                HeaderInfo.GetType().GetField("IsRequestRestricted", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(HeaderInfo, false);
                HeaderInfo.GetType().GetField("IsResponseRestricted", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(HeaderInfo, false);
                Table[Key] = HeaderInfo;
            }

            tHashtable.SetValue(null, Table);
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
