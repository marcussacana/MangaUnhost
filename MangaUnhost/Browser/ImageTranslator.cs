using CefSharp;
using CefSharp.EventHandler;
using CefSharp.OffScreen;
using MangaUnhost.Others;
using Newtonsoft.Json;
using Nito.AsyncEx;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MangaUnhost.Browser
{
    internal class ImageTranslator : IDisposable
    {
        public static int LOCAL_PORT { get; private set; } = 8021;
        string SourceLang, TargetLang;

        ChromiumWebBrowser Browser;
        public ImageTranslator(string SourceLang, string TargetLang)
        {
            this.SourceLang = SourceLang.ToLowerInvariant();
            this.TargetLang = TargetLang.ToLowerInvariant();

            if (!Program.MTLAvailable) {
                Browser = new ChromiumWebBrowser("about:blank");
                Browser.WaitInitialize();
                Browser.RequestHandler = new RequestEventHandler();
                ((RequestEventHandler)Browser.RequestHandler).OnResourceRequestEvent += ImageTranslator_OnResourceRequestEvent;
                Browser.SetUserAgent(ProxyTools.UserAgent);
                //Browser.BypassGoogleCEFBlock();
                Reload(this.SourceLang, this.TargetLang);
            }
        }

        public static int FindPort()
        {
            LOCAL_PORT = 8000 + new Random().Next(999);
            while (IsServerRunning())
            {
                LOCAL_PORT += 3;
            }
            return LOCAL_PORT;
        }

        private void ImageTranslator_OnResourceRequestEvent(object sender, OnResourceRequestEventArgs e)
        {
            //I'm suspect of the log requests, so, let's block it
            if (e.TargetUrl.Contains("/log?"))
            {
                e.ResourceRequestHandler = new ResourceRequestEventHandler();
                ((ResourceRequestEventHandler)e.ResourceRequestHandler).OnBeforeResourceLoadEvent += ImageTranslator_OnBeforeResourceLoadEvent;
            }
        }

        private void ImageTranslator_OnBeforeResourceLoadEvent(object sender, OnBeforeResourceLoadEventArgs e)
        {
            e.Request.Dispose();
            e.ReturnValue = CefReturnValue.Cancel;
        }

        public void Reload() => Reload(SourceLang, TargetLang);
        private void Reload(string SourceLang, string TargetLang)
        {
            if (Program.MTLAvailable)
                return;

            Browser.WaitForLoad($"https://translate.google.com.br/?sl={SourceLang}&tl={TargetLang}&op=images");
            Browser.InjectXPATH();
            Browser.EvaluateScriptUnsafe(Properties.Resources.toDataURL);
        }

        private static bool DevVisible = false;

        public byte[] TranslateImage(byte[] Image, bool CompatibleMode)
        {
            if (Program.MTLAvailable)
                return LocalTranslateImage(Image, CompatibleMode);

            var tmpPath = Path.ChangeExtension(Path.GetTempFileName(), "png");
            File.WriteAllBytes(tmpPath, Image);

            try
            {
#if DEBUG && FALSE
                if (true || !Debugger.IsAttached)
                {
                    if (!DevVisible)
                    {
                        DevVisible = true;
                        Debugger.Launch();
                        Browser.ShowDevTools();

                        var test = new BrowserPopup(Browser, () => { return false; });
                        test.Show();
                    }
                }
                
#endif
                var ID = Browser.EvaluateScriptUnsafe<string>("XPATH('//input[contains(@accept, \\'image\\')]', false).id = 'imagePicker'");

                if (string.IsNullOrEmpty(ID))
                    throw new Exception("File Input Not Found");

                AsyncContext.Run(() => Browser.SetInputFile(ID, tmpPath));


                int waiting = 0;

                while (IsLoading())
                {
                    if (waiting++ > 60)
                        throw new Exception("Image Translation Failed");

                    ThreadTools.Wait(1000, true);
                }

                waiting = 0;

                while (true)
                {
                    if (hasFailed())
                        throw new Exception("Image Translation Failed");

                    if (waiting++ > 30)
                        throw new Exception("Image Translation Failed");

                    ThreadTools.Wait(1000, true);
                    var Url = Browser.EvaluateScriptUnsafe<string>("XPATH(\"(//img[starts-with(@src, 'blob:')])[last()]\", false).src");

                    if (Url == null || !Url.StartsWith("blob:"))
                        continue;

                    Browser.EvaluateScriptUnsafe<string>($"toDataURL('{Url}', function(dataUrl) {{ globalThis.currentImage = dataUrl; }})");
                    break;
                }

                waiting = 0;

                string Data = null;

                do
                {
                    Data = Browser.EvaluateScriptUnsafe<string>("currentImage");


                    if (Data == null)
                    {
                        if (waiting++ > 5)
                            throw new Exception("Image Translation Failed");

                        ThreadTools.Wait(1000, true);
                        Data = "";
                    }

                } while (!Data.StartsWith("data:"));

                Data = Data.Substring(";base64,");

                var NewData = Convert.FromBase64String(Data);

                Reload();

                return NewData;
            }
            finally
            {
                File.Delete(tmpPath);
            }
        }

        private byte[] LocalTranslateImage(byte[] image, bool Compatible)
        {
            var root = new Root()
            {
                Image = $"data:image/png;base64,{Convert.ToBase64String(image)}",
                Config = new Config()
                {
                    Render = new Render()
                    {
                        Alignment = "auto",
                        Direction = "auto",
                        DisableFontBorder = false,
                        FontSizeMinimum = 14,
                        FontSizeOffset = 0,
                        GimpFont = "Sans-serif",
                        Lowercase = false,
                        NoHyphenation = true,
                        Renderer = "default",
                        Rtl = true,
                        Uppercase = false
                    },
                    Upscale = new Upscale()
                    {
                        RevertUpscaling = false,
                        Upscaler = "esrgan"
                    },
                    Translator = new Translator()
                    {
                        EnablePostTranslationCheck = true,
                        NoTextLangSkip = false,
                        PostCheckMaxRetryAttempts = 3,
                        PostCheckRepetitionThreshold = 20,
                        PostCheckTargetLangThreshold = 0.5,
                        TargetLang = MapLang(TargetLang),
                        TranslatorName = "custom_openai"
                    },
                    Detector = new Detector()
                    {
                        BoxThreshold = 0.7,
                        DetAutoRotate = false,
                        DetGammaCorrect = false,
                        DetInvert = false,
                        DetRotate = false,
                        DetectionSize = 2048,
                        DetectorName = Compatible ? "paddle" : "ctd",
                        TextThreshold = 0.5,
                        UnclipRatio = 2.3
                    },
                    Colorizer = new Colorizer()
                    {
                        ColorizationSize = 576,
                        ColorizerName = "none",
                        DenoiseSigma = 30
                    },
                    Inpainter = new Inpainter()
                    {
                        InpainterName = "lama_large",
                        InpaintingPrecision = "bf16",
                        InpaintingSize = 2048
                    },
                    Ocr = new Ocr()
                    {
                        IgnoreBubble = 0,
                        MinTextLength = 0,
                        OcrName = "48px",
                        UseMocrMerge = true
                    },
                    ForceSimpleSort = false,
                    KernelSize = 3,
                    MaskDilationOffset = 20
                }
            };

            var request = JsonConvert.SerializeObject(root);

            StartServer();

            try {
                return UrlTools.Upload($"http://127.0.0.1:{LOCAL_PORT}/translate/image", Encoding.UTF8.GetBytes(request));
            }
            catch {
                Thread.Sleep(1000);
                throw;
            }
        }

        private static Process p = null;
        public static void StartServer()
        {
            if (IsServerRunning())
                return;

            var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MTL", "server", "run.bat");

            if (!File.Exists(exePath))
                throw new FileNotFoundException("MTL Startup Script Not Found");

            if (p != null && !p.HasExited)
                closeServer();

            p = new Process();
            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
            p.StartInfo.FileName = exePath;
            p.StartInfo.Arguments = $"--port {LOCAL_PORT}";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();

            int tries = 30;
            while (!IsServerRunning() && tries-- > 0)
                Thread.Sleep(1000);
        }

        public static bool IsServerRunning()
        {
            try
            {
                var response = UrlTools.Download($"http://127.0.0.1:{LOCAL_PORT}/manual");
                return Encoding.UTF8.GetString(response).Contains("Translation");
            }
            catch {
                return false;
            }
        }

        public static int FindProcessByCmdLine(string CmdLine)
        {
            int rst = -1;

            var thread = new Thread(() =>
            {
                foreach (var process in Process.GetProcessesByName("python").Concat(Process.GetProcessesByName("cmd")))
                {
                    try
                    {
                        string query = $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}";

                        using (var searcher = new ManagementObjectSearcher(query) { Options = new EnumerationOptions() { Timeout = TimeSpan.FromSeconds(1) } })
                        using (var results = searcher.Get())
                        {
                            foreach (ManagementObject obj in results)
                            {
                                string cmdline = obj["CommandLine"] as string;

                                if (cmdline != null && cmdline.IndexOf(CmdLine, StringComparison.InvariantCultureIgnoreCase) >= 0)
                                {
                                    rst = process.Id;
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            });

            thread.Start();

            try
            {
                thread.Join(TimeSpan.FromSeconds(1));
                thread.Abort();
            } 
            catch { }

            return rst;
        }

        public static int GetSocketsForProcess(int port)
        {
            const int ERROR_INSUFFICIENT_BUFFER = 0x7A;

            var size = 0;
            var result = GetTcpTable2(IntPtr.Zero, ref size, false);
            if (result != ERROR_INSUFFICIENT_BUFFER)
                throw new Win32Exception(result);

            var ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                result = GetTcpTable2(ptr, ref size, false);
                if (result != 0)
                    throw new Win32Exception(result);

                var list = new List<IPEndPoint>();
                var count = Marshal.ReadInt32(ptr);
                var curPtr = ptr + Marshal.SizeOf<MIB_TCPTABLE>();
                var length = Marshal.SizeOf<MIB_TCPROW2>();
                for (var i = 0; i < count; i++)
                {
                    var row = Marshal.PtrToStructure<MIB_TCPROW2>(curPtr);
                    if ((row.localPort1 << 8 | row.localPort2) == port)
                        return row.dwOwningPid;
                    curPtr += length;
                }
                return -1;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        [DllImport("Iphlpapi.dll", ExactSpelling = true)]
        static extern int GetTcpTable2(
          IntPtr TcpTable,
          ref int SizePointer,
          bool Order
        );

        [StructLayout(LayoutKind.Sequential)]
        struct MIB_TCPTABLE
        {
            public int dwNumEntries;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MIB_TCPROW2
        {
            public MIB_TCP_STATE dwState;
            public int dwLocalAddr;
            public byte localPort1;
            public byte localPort2;
            // Ports are only 16 bit values (in network WORD order, 3,4,1,2).
            // There are reports where the high order bytes have garbage in them.
            public byte ignoreLocalPort3;
            public byte ignoreLocalPort4;
            public int dwRemoteAddr;
            public byte remotePort1;
            public byte remotePort2;
            // Ports are only 16 bit values (in network WORD order, 3,4,1,2).
            // There are reports where the high order bytes have garbage in them.
            public byte ignoreremotePort3;
            public byte ignoreremotePort4;
            public int dwOwningPid;
            public TCP_CONNECTION_OFFLOAD_STATE dwOffloadState;
        }

        public enum MIB_TCP_STATE
        {
            Closed = 1,
            Listen,
            SynSent,
            SynRcvd,
            Established,
            FinWait1,
            FinWait2,
            CloseWait,
            Closing,
            LastAck,
            TimeWait,
            DeleteTcb
        }

        enum TCP_CONNECTION_OFFLOAD_STATE
        {
            TcpConnectionOffloadStateInHost,
            TcpConnectionOffloadStateOffloading,
            TcpConnectionOffloadStateOffloaded,
            TcpConnectionOffloadStateUploading,
            TcpConnectionOffloadStateMax
        }

        public static string MapLang(string lang)
        {
            return lang.Split('-').First().ToLower() switch
            {
                "pt" => "PTB", // Portuguese (Brazilian)
                "en" => "ENG", // English
                "zh" => "CHS", // Simplified Chinese (default se não especificar zh-tw)
                "tw" => "CHT", // Traditional Chinese (quando quiser diferenciar zh-tw)
                "cs" => "CSY", // Czech
                "nl" => "NLD", // Dutch
                "fr" => "FRA", // French
                "de" => "DEU", // German
                "hu" => "HUN", // Hungarian
                "it" => "ITA", // Italian
                "ja" => "JPN", // Japanese
                "ko" => "KOR", // Korean
                "pl" => "POL", // Polish
                "ro" => "ROM", // Romanian
                "ru" => "RUS", // Russian
                "es" => "ESP", // Spanish
                "tr" => "TRK", // Turkish
                "uk" => "UKR", // Ukrainian
                "vi" => "VIN", // Vietnamese
                "ar" => "ARA", // Arabic
                "sr" => "SRP", // Serbian
                "hr" => "HRV", // Croatian
                "th" => "THA", // Thai
                "id" => "IND", // Indonesian
                "fil" => "FIL", // Filipino
                _ => "ENG" // fallback padrão
            };
        }

        private bool hasFailed()
        {
            return Browser.EvaluateScriptUnsafe<bool>("XPATH(\"//div[@role='status']//button\", true).length > 0");
        }

        private bool IsLoading()
        {
            return Browser.EvaluateScriptUnsafe<bool>("XPATH(\"(//c-wiz//div[@role='progressbar'])[last()]\", false)?.outerHTML.indexOf('div role') == 1");
        }

        public void Dispose()
        {
            closeServer();
            Browser.Dispose();
        }

        private static void closeServer()
        {
            try
            {
                while (true)
                {
                    int pid = FindProcessByCmdLine($"--port {LOCAL_PORT}");
                    if (pid >= 0)
                        Process.GetProcessById(pid).Kill();
                    else
                        break;
                }

                while (true)
                {

                    try
                    {
                        int pid = GetSocketsForProcess(LOCAL_PORT);
                        if (pid >= 0)
                            Process.GetProcessById(pid).Kill();
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }

            try
            {
                while (true)
                {
                    int pid = FindProcessByCmdLine($"--port {(LOCAL_PORT + 1)}");
                    if (pid >= 0)
                        Process.GetProcessById(pid).Kill();
                    else
                        break;
                }
            }
            catch
            {
            }
        }


        public struct Root
        {
            [JsonProperty("image")]
            public string Image { get; set; }

            [JsonProperty("config")]
            public Config Config { get; set; }
        }

        public struct Config
        {
            [JsonProperty("render")]
            public Render Render { get; set; }

            [JsonProperty("upscale")]
            public Upscale Upscale { get; set; }

            [JsonProperty("translator")]
            public Translator Translator { get; set; }

            [JsonProperty("detector")]
            public Detector Detector { get; set; }

            [JsonProperty("colorizer")]
            public Colorizer Colorizer { get; set; }

            [JsonProperty("inpainter")]
            public Inpainter Inpainter { get; set; }

            [JsonProperty("ocr")]
            public Ocr Ocr { get; set; }

            [JsonProperty("force_simple_sort")]
            public bool ForceSimpleSort { get; set; }

            [JsonProperty("kernel_size")]
            public int KernelSize { get; set; }

            [JsonProperty("mask_dilation_offset")]
            public int MaskDilationOffset { get; set; }
        }

        public struct Render
        {
            [JsonProperty("alignment")]
            public string Alignment { get; set; }

            [JsonProperty("direction")]
            public string Direction { get; set; }

            [JsonProperty("disable_font_border")]
            public bool DisableFontBorder { get; set; }

            [JsonProperty("font_size_minimum")]
            public int FontSizeMinimum { get; set; }

            [JsonProperty("font_size_offset")]
            public int FontSizeOffset { get; set; }

            [JsonProperty("gimp_font")]
            public string GimpFont { get; set; }

            [JsonProperty("lowercase")]
            public bool Lowercase { get; set; }

            [JsonProperty("no_hyphenation")]
            public bool NoHyphenation { get; set; }

            [JsonProperty("renderer")]
            public string Renderer { get; set; }

            [JsonProperty("rtl")]
            public bool Rtl { get; set; }

            [JsonProperty("uppercase")]
            public bool Uppercase { get; set; }
        }

        public struct Upscale
        {
            [JsonProperty("revert_upscaling")]
            public bool RevertUpscaling { get; set; }

            [JsonProperty("upscaler")]
            public string Upscaler { get; set; }
        }

        public struct Translator
        {
            [JsonProperty("enable_post_translation_check")]
            public bool EnablePostTranslationCheck { get; set; }

            [JsonProperty("no_text_lang_skip")]
            public bool NoTextLangSkip { get; set; }

            [JsonProperty("post_check_max_retry_attempts")]
            public int PostCheckMaxRetryAttempts { get; set; }

            [JsonProperty("post_check_repetition_threshold")]
            public int PostCheckRepetitionThreshold { get; set; }

            [JsonProperty("post_check_target_lang_threshold")]
            public double PostCheckTargetLangThreshold { get; set; }

            [JsonProperty("target_lang")]
            public string TargetLang { get; set; }

            [JsonProperty("translator")]
            public string TranslatorName { get; set; }
        }

        public struct Detector
        {
            [JsonProperty("box_threshold")]
            public double BoxThreshold { get; set; }

            [JsonProperty("det_auto_rotate")]
            public bool DetAutoRotate { get; set; }

            [JsonProperty("det_gamma_correct")]
            public bool DetGammaCorrect { get; set; }

            [JsonProperty("det_invert")]
            public bool DetInvert { get; set; }

            [JsonProperty("det_rotate")]
            public bool DetRotate { get; set; }

            [JsonProperty("detection_size")]
            public int DetectionSize { get; set; }

            [JsonProperty("detector")]
            public string DetectorName { get; set; }

            [JsonProperty("text_threshold")]
            public double TextThreshold { get; set; }

            [JsonProperty("unclip_ratio")]
            public double UnclipRatio { get; set; }
        }

        public struct Colorizer
        {
            [JsonProperty("colorization_size")]
            public int ColorizationSize { get; set; }

            [JsonProperty("colorizer")]
            public string ColorizerName { get; set; }

            [JsonProperty("denoise_sigma")]
            public int DenoiseSigma { get; set; }
        }

        public struct Inpainter
        {
            [JsonProperty("inpainter")]
            public string InpainterName { get; set; }

            [JsonProperty("inpainting_precision")]
            public string InpaintingPrecision { get; set; }

            [JsonProperty("inpainting_size")]
            public int InpaintingSize { get; set; }
        }

        public struct Ocr
        {
            [JsonProperty("ignore_bubble")]
            public int IgnoreBubble { get; set; }

            [JsonProperty("min_text_length")]
            public int MinTextLength { get; set; }

            [JsonProperty("ocr")]
            public string OcrName { get; set; }

            [JsonProperty("use_mocr_merge")]
            public bool UseMocrMerge { get; set; }
        }

    }
}
