using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Ionic.Zip;

class AppVeyor {
    const string UpdateSufix = "-NewAppUpdate.exe";
    const string Info = "build";

    string API = "https://ci.appveyor.com/api/projects/{0}/{1}/";
    string Artifact = string.Empty;
    public static string MainExecutable = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath;
    public AppVeyor(string Username, string Project, string Artifact) {
        API = string.Format(API, Username, Project);
        this.Artifact = Artifact;

        if (!File.Exists(MainExecutable))
            throw new Exception("Failed to Catch the Executable Path");
    }

    public string FinishUpdate() {
        if (MainExecutable.EndsWith(UpdateSufix)) {
            string OriginalPath = MainExecutable.Substring(0, MainExecutable.Length - UpdateSufix.Length);
            for (int Tries = 0; Tries < 10; Tries++) {
                Process[] Procs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(OriginalPath));
                foreach (var Proc in Procs) {
                    try {
                        Proc.Kill();
                        Thread.Sleep(100);
                    } catch { }
                }
                try {
                    if (File.Exists(OriginalPath))
                        File.Delete(OriginalPath);
                } catch {
                    Thread.Sleep(100);
                    continue;
                }
                break;
            }
            File.Copy(MainExecutable, OriginalPath);
            return OriginalPath;
        } else return null;
    }
    public bool HaveUpdate() {
        try {
            string CurrentVersion = FileVersionInfo.GetVersionInfo(MainExecutable).FileVersion.Trim();
            string LastestVersion = GetLastestVersion().Trim();
            int[] CurrArr = CurrentVersion.Split('.').Select(x => int.Parse(x)).ToArray();
            int[] LastArr = LastestVersion.Split('.').Select(x => int.Parse(x)).ToArray();
            for (int i = 0; i < 0; i++) {
                if (LastArr[i] > CurrArr[i])
                    return true;
                if (LastArr[i] == CurrArr[i])
                    continue;
                return false;//Lst<Curr
            }
            return false;
        } catch { return false; }
    }

    public void Update() {
        if (!HaveUpdate())
            return;
        string Result = FinishUpdate();
        if (Result != null) {
            Process.Start(Result);
            Environment.Exit(0);
        }

        MemoryStream Update = new MemoryStream(Download(API + "artifacts\\" +  Artifact));
        var Zip = ZipFile.Read(Update);
        string TMP = Path.GetTempFileName();
        if (File.Exists(TMP))
            File.Delete(TMP);
        TMP += "\\";
        if (!Directory.Exists(TMP))
            Directory.CreateDirectory(TMP);
        Zip.ExtractAll(TMP, ExtractExistingFileAction.OverwriteSilently);

        foreach (string File in Directory.GetFiles(TMP, "*.*", SearchOption.AllDirectories)) {
            string Base = File.Substring(TMP.Length, File.Length - TMP.Length);
            string Output = AppDomain.CurrentDomain.BaseDirectory + Base;
            if (Output == MainExecutable) {
                Output += UpdateSufix;
            }

            if (System.IO.File.Exists(Output))
                System.IO.File.Delete(Output);
            System.IO.File.Move(File, Output);
        }
        Directory.Delete(TMP);

        Process.Start(MainExecutable + UpdateSufix);
        Environment.Exit(0);
    }

    private byte[] Download(string URL) {
        MemoryStream MEM = new MemoryStream();
        Download(URL, MEM);
        byte[] DATA = MEM.ToArray();
        MEM.Close();
        return DATA;
    }

    private void Download(string URL, Stream Output, int tries = 4) {
        try {
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(URL);
            //Bypass a fucking bug in the fucking .net framework
            if (Request.Address.AbsoluteUri != URL && tries <= 2) {
                /*
                WebClient WC = new WebClient();
                WC.QueryString.Add("action", "shorturl");
                WC.QueryString.Add("format", "simple");
                WC.QueryString.Add("url", URL);
                URL = WC.DownloadString("https://u.nu/api.php");*/

                Request = (HttpWebRequest)WebRequest.Create("http://proxy-it.nordvpn.com/browse.php?u=" + URL);
                Request.Referer = "http://proxy-it.nordvpn.com";
            }

            Request.UseDefaultCredentials = true;
            Request.Method = "GET";
            WebResponse Response = Request.GetResponse();
            byte[] FC = new byte[0];
            using (Stream Reader = Response.GetResponseStream()) {
                byte[] Buffer = new byte[1024];
                int bytesRead;
                do {
                    bytesRead = Reader.Read(Buffer, 0, Buffer.Length);
                    Output.Write(Buffer, 0, bytesRead);
                } while (bytesRead > 0);
            }
        } catch (Exception ex) {
            if (tries < 0)
                throw new Exception(string.Format("Connection Error: {0}", ex.Message));

            Thread.Sleep(1000);
            Download(URL, Output, tries - 1);
        }
    }

    private string GetLastestVersion() {
        string Reg = "<Version>([0-9.]*)<\\/Version>";
        string XML = new WebClient().DownloadString(API);
        var a = System.Text.RegularExpressions.Regex.Match(XML, Reg);
        return a.Groups[1].Value;
    }

}