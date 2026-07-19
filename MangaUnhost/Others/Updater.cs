using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Ionic.Zip;
using System.Collections.Generic;

class Updater {

    string UpdateIniUrl = "https://raw.githubusercontent.com/marcussacana/MangaUnhost/data/update.ini";
    string BaseZipUrl = "https://raw.githubusercontent.com/marcussacana/MangaUnhost/data/update.zip";
    
    string cache = null;
    public static string MainExecutable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
    public static string TempUpdateDir = Path.GetDirectoryName(MainExecutable) + "\\GitHubRelease\\";
    public static string CurrentVersion {
        get {
            var Version = FileVersionInfo.GetVersionInfo(MainExecutable);
            return Version.FileMajorPart + "." + Version.FileMinorPart + "." + Version.FileBuildPart;
        }
    }

    public Updater() {
        if (!File.Exists(MainExecutable))
            throw new Exception("Failed to Catch the Executable Path");
    }

    // Constructor to allow overriding the repo URL for sandbox testing
    public Updater(string BaseUrl) {
        UpdateIniUrl = BaseUrl.TrimEnd('/') + "/update.ini";
        BaseZipUrl = BaseUrl.TrimEnd('/') + "/update.zip";
    }

    public string FinishUpdate() {
        if (FinishUpdatePending()) {
            int Len = MainExecutable.IndexOf("\\GitHubRelease\\");

            string OriginalPath = MainExecutable.Substring(0, Len);
            string RunningDir = Path.GetDirectoryName(MainExecutable);

            if (!RunningDir.EndsWith("\\"))
                RunningDir += '\\';
            if (!OriginalPath.EndsWith("\\"))
                OriginalPath += '\\';

            foreach (string File in Directory.GetFiles(RunningDir, "*.*", SearchOption.AllDirectories)) {
                string Base = File.Substring(RunningDir.Length).TrimStart('\\');
                string UpPath = RunningDir + Base;
                string OlPath = OriginalPath + Base;

                Delete(OlPath);
                System.IO.File.Copy(UpPath, OlPath, true);
            }

            return OriginalPath + Path.GetFileName(MainExecutable);
        } else {
            new Thread(() => {
                int i = 0;
                while (Directory.Exists(TempUpdateDir) && i < 5) {
                    try {
                        Directory.Delete(TempUpdateDir, true);
                    } catch { Thread.Sleep(1000); i++; }
                }
            }).Start();
            return null;
        }
    }

    private void Delete(string File) {
        for (int Tries = 0; Tries < 10; Tries++) {
            try {
                string ProcName = Path.GetFileNameWithoutExtension(MainExecutable);
                Process[] Procs = Process.GetProcessesByName(ProcName);
                int ID = Process.GetCurrentProcess().Id;
                foreach (var Proc in Procs) {
                    if (Proc.Id == ID)
                        continue;

                    try {
                        Proc.Kill();
                        Thread.Sleep(100);
                    } catch { }
                }

                if (System.IO.File.Exists(File))
                    System.IO.File.Delete(File);
            } catch {
                Thread.Sleep(100);
                continue;
            }

            break;
        }
    }

    public bool HaveUpdate() {
        try {
            if (Debugger.IsAttached)
                return false;

            string CurrentVersionStr = CurrentVersion.Trim();
            string LatestVersionStr = GetLastestVersion().Trim();
            int[] CurrArr = CurrentVersionStr.Split('.').Select(x => int.Parse(x)).ToArray();
            int[] LastArr = LatestVersionStr.Split('.').Select(x => int.Parse(x)).ToArray();
            int Max = CurrArr.Length < LastArr.Length ? CurrArr.Length : LastArr.Length;
            for (int i = 0; i < Max; i++) {
                if (LastArr[i] > CurrArr[i])
                    return true;
                if (LastArr[i] == CurrArr[i])
                    continue;
                return false;
            }
            return false;
        } catch (Exception) { return false; }
    }

    public bool FinishUpdatePending() {
        if (MainExecutable.Contains("\\GitHubRelease\\"))
            return true;
        return false;
    }

    public void Update() {
        if (!HaveUpdate())
            return;

        string Result = FinishUpdate();
        if (Result != null) {
            Process.Start(new ProcessStartInfo { FileName = Result, UseShellExecute = true });
            Environment.Exit(0);
        }

        try {
            if (Directory.Exists(TempUpdateDir))
                Directory.Delete(TempUpdateDir, true);
        } catch { }

        Directory.CreateDirectory(TempUpdateDir);
        
        using (MemoryStream updateStream = new MemoryStream())
        {
            DownloadSplitArchive(BaseZipUrl, updateStream);
            updateStream.Position = 0;

            using (var Zip = ZipFile.Read(updateStream))
            {
                Zip.ExtractAll(TempUpdateDir, ExtractExistingFileAction.OverwriteSilently);
            }
        }

        Process.Start(new ProcessStartInfo { FileName = TempUpdateDir + Path.GetFileName(MainExecutable), UseShellExecute = true });
        Environment.Exit(0);
    }

    private void DownloadSplitArchive(string baseUrl, Stream outputStream)
    {
        BypassSLL();
        int part = 0;
        while (true)
        {
            string url = $"{baseUrl}.{part:D3}";
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
                using (var resp = req.GetResponse())
                using (var stream = resp.GetResponseStream())
                {
                    stream.CopyTo(outputStream);
                }
                part++;
            }
            catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
            {
                break;
            }
            catch (Exception ex)
            {
                if (part == 0) throw new Exception("Failed to download update: " + ex.Message);
                break;
            }
        }
        if (part == 0) throw new Exception("No update files found.");
    }

    private string GetLastestVersion() {
        string ini = GetApiResult();
        foreach (var line in ini.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
            if (line.StartsWith("Version=")) {
                return line.Substring(8).Trim();
            }
        }
        return "0.0.0";
    }

    private string GetApiResult() {
        if (cache != null)
            return cache;

        BypassSLL();

        WebClient Client = new WebClient();
        Client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        cache = Client.DownloadString(UpdateIniUrl);
        return cache;
    }

    public void BypassSLL() {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
        ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
    }
}
