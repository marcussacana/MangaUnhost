using Ionic.Zip;
using MangaUnhost.Browser;
using MangaUnhost.Others;
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
        static string _PyPath = null;
        public static string PythonPath
        {
            get
            {
                if (_PyPath != null)
                    return _PyPath;

                var Def = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs\\Python\\Python39\\Python.exe");
                if (File.Exists(Def))
                    return _PyPath = Def;

                Def = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "py.exe");
                if (File.Exists(Def))
                    return _PyPath = Def;

                return "C:\\Python39\\Python.exe";
            }
        }
        public static string MTLPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MTL", "translate_single.py");
        
        static bool? _MTLAvailable = null;
        public static bool MTLAvailable => _MTLAvailable ??= File.Exists(MTLPath) && File.Exists(PythonPath);

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
        static void Main()
        {
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
            
            
            if (IsRealWindows)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }

            FinishUpdate();
            //WineHelper();
            CefUpdater();

            var PATH = Environment.GetEnvironmentVariable("PATH");
            Environment.SetEnvironmentVariable("PATH", PATH.TrimEnd(';') + ";" + Path.GetDirectoryName(LibWebP));

            UnlockHeaders();

            Application.Run(new Main());

            if (Debug)
                Writer.Flush();
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

        private static void CefUpdater(string CefRepo = "https://github.com/marcussacana/MangaUnhost/raw/data/")
        {
            var VerFilePath = Path.Combine(CefDir, "version.txt");
            bool Outdated = false;
            if (!File.Exists(BrowserSubprocessPath))
                Outdated = true;

            var TargetVer = new Version(107, 1, 120, 0);

            if (!Outdated)
            {
                var VerStr = FileVersionInfo.GetVersionInfo(BrowserSubprocessPath).FileVersion;
                //if (new Version(VerStr) != new Version(75, 1, 143, 0))
                //if (new Version(VerStr) != new Version(79, 1, 360, 0))
                if (new Version(VerStr) != TargetVer)
                {
                    Outdated = true;
                }
            }

            if (File.Exists(VerFilePath) && File.ReadAllText(VerFilePath).Trim() == TargetVer.ToString())
                Outdated = false;

            if (!File.Exists(LibWebP))
                Outdated = true;

            if (!Outdated)
                return;

            string CEFName = $"CEF{(Environment.Is64BitProcess ? "x64" : "x86")}-v{TargetVer}.zip";
            string Url = $"{CefRepo}{CEFName}?raw=true";
            string SaveAs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CEFName);

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
                    var AltRepo = "https://github.com/marcussacana/MangaUnhost/blob/data/";
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

            File.WriteAllText(VerFilePaht, TargetVer.ToString());

            if (!Debugger.IsAttached)
                File.Delete(SaveAs);
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

        static Assembly LoadFromPlatformFolder(object sender, ResolveEventArgs args)
        {
            string folderPath = Path.GetDirectoryName(CurrentAssembly);
            string assemblyPath = Path.Combine(folderPath, (Environment.Is64BitProcess ? "x64" : "x86"), new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }


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
