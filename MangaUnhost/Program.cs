using Ionic.Zip;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace MangaUnhost {
    static class Program {
        public static bool Debug => Debugger.IsAttached;
        public static string CurrentAssembly => Assembly.GetExecutingAssembly().Location;
        public static string CefDir => Path.Combine(Path.GetDirectoryName(CurrentAssembly), (Environment.Is64BitProcess ? "x64" : "x86"));

        public static string BrowserSubprocessPath => Path.Combine(CefDir, "CefSharp.BrowserSubprocess.exe");

        public static GitHub Updater = new GitHub("Marcussacana", "MangaUnhost");

        /// <summary>
        /// Ponto de entrada principal para o aplicativo.
        /// </summary>
        [STAThread]
        static void Main() {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromPlatformFolder);

            string PluginDir = Path.Combine(Path.GetDirectoryName(CurrentAssembly), "Plugins");
            if (Directory.Exists(PluginDir)) {
                foreach (string PluginPath in Directory.EnumerateFiles(PluginDir, "*.dll", SearchOption.TopDirectoryOnly)) {
                    Assembly.LoadFrom(PluginPath);
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            FinishUpdate();
            CefUpdater();

            Application.Run(new Main());
        }

        private static void FinishUpdate() {
            if (Debug)
                return;

            string Result = Updater.FinishUpdate();
            if (Result != null) {
                Process.Start(Result);
                Environment.Exit(0);
            }
        }

        private static void CefUpdater() {
             string CefRepo = "https://raw.githubusercontent.com/marcussacana/MangaUnhost/data/";

            bool Outdated = false;
            if (!File.Exists(BrowserSubprocessPath))
                Outdated = true;
            if (!Outdated){
                var VerStr = FileVersionInfo.GetVersionInfo(BrowserSubprocessPath).FileVersion;
                if (new Version(VerStr) < new Version(73, 1, 130, 0)) {
                    Outdated = true;
                }
            }

            if (!Outdated)
                return;

            string CEFName = $"CEF{(Environment.Is64BitProcess ? "x64" : "x86")}.zip";
            string Url = $"{CefRepo}{CEFName}";
            string SaveAs = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), CEFName);

            DownloadingWindow Window = new DownloadingWindow(Url, SaveAs);
            Application.Run(Window);

            ZipFile Zip = new ZipFile(SaveAs);
            Zip.ExtractAll(Path.GetDirectoryName(CurrentAssembly), ExtractExistingFileAction.OverwriteSilently);
            Zip.Dispose();

            File.Delete(SaveAs);
        }

        static Assembly LoadFromPlatformFolder(object sender, ResolveEventArgs args) {
            string folderPath = Path.GetDirectoryName(CurrentAssembly);
            string assemblyPath = Path.Combine(folderPath, (Environment.Is64BitProcess ? "x64" : "x86"), new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }
    }
}
