using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace MangaUnhost.Others {

    internal static class DataTools {

        internal static string GetTextFromSpeech(byte[] WAV) {
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create("https://api.wit.ai/speech");
            Request.Method = "POST";
            Request.Headers[HttpRequestHeader.Authorization] = "Bearer NVYD6ZUJMC26US5XS2ZJJ32EDZZ654TD";
            Request.ContentType = "audio/wav";
            Request.ContentLength = WAV.Length;
            Request.UserAgent = ProxyTools.UserAgent;
            Request.ServicePoint.Expect100Continue = false;

            var POST = Request.GetRequestStream();
            new MemoryStream(WAV).CopyTo(POST);
            POST.Flush();
            POST.Close();

            var RESP = Request.GetResponse();
            var GET = RESP.GetResponseStream();

            var Buffer = new MemoryStream();
            GET.CopyTo(Buffer);
            RESP.Close();

            var JSON = System.Text.Encoding.UTF8.GetString(Buffer.ToArray());

            return ReadJson(JSON, "_text");
        }

        internal static byte[] Mp3ToWav(byte[] MP3) {
            using (MemoryStream INPUT = new MemoryStream(MP3))
            using (Mp3FileReader mp3 = new Mp3FileReader(INPUT))
            using (WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(mp3))
            using (MemoryStream OUTPUT = new MemoryStream()) {
                WaveFileWriter.WriteWavFileToStream(OUTPUT, pcm);
                return OUTPUT.ToArray();
            }
        }

        internal static string ReadJson(string JSON, string Name) {
            string Finding = string.Format("\"{0}\":", Name);
            int Pos = JSON.IndexOf(Finding) + Finding.Length;
            if (Pos - Finding.Length == -1) {
                Finding = Finding.TrimEnd(':');
                Pos = JSON.IndexOf(Finding) + Finding.Length;
                if (Pos - Finding.Length == -1)
                    return null;
                while (JSON[Pos] != ':')
                    Pos++;
                Pos++;
            }

            string Cutted = JSON.Substring(Pos, JSON.Length - Pos).TrimStart(' ', '\n', '\r');
            char[] Close = Cutted.StartsWith("\"") ? new char[] { '"' } : new char[] { ',', '}' };
            Cutted = Cutted.TrimStart('"');
            string Data = string.Empty;
            foreach (char c in Cutted) {
                if (Close.Contains(c))
                    break;
                Data += c;
            }
            if (Data.Contains("\\"))
                throw new Exception("Ops... Unsupported Json Format...");

            return Data;
        }
        public static string GetRawName(string PathName, bool DeupperlizerOnly = false, bool FileNameMode = false) {
            string Name = DeupperlizerOnly ? PathName.ToLower() : PathName.Replace("-", " ").Replace("_", " ");
            bool Spaced = true;
            string ResultName = string.Empty;
            foreach (char c in Name) {
                if (FileNameMode) {
                    if (Path.GetInvalidFileNameChars().Contains(c) || Path.GetInvalidPathChars().Contains(c))
                        continue;
                }
                if (c == ' ') {
                    Spaced = true;
                    ResultName += ' ';
                    continue;
                }
                if (Spaced) {
                    ResultName += c.ToString().ToUpper();
                    Spaced = false;
                } else
                    ResultName += c;
            }

            if (!DeupperlizerOnly) {
                ResultName = ResultName.TrimStart(' ', '0');
                if (string.IsNullOrWhiteSpace(ResultName))
                    ResultName = "0";
            }

            if (FileNameMode)
                ResultName = ResultName.Replace("...", "");

            return ResultName;
        }

        static internal Key ReverseMatch<Key, Value>(this Dictionary<Key, Value> Dictionary, Value ValueToSearch) {
            if (!Dictionary.ContainsValue(ValueToSearch))
                throw new Exception("Value not Present in the Dictionary");

            int Index = Dictionary.Values.Select((value, index) => new { value, index })
                        .SkipWhile(pair => !pair.value.Equals(ValueToSearch)).FirstOrDefault().index;

            return Dictionary.Keys.ElementAt(Index);
        }

        static internal int IndexOfKey<Key, Value>(this Dictionary<Key, Value> Dictionary, Key KeyToSearch) {
            if (!Dictionary.ContainsKey(KeyToSearch))
                throw new Exception("Value not Present in the Dictionary");

            int Index = Dictionary.Keys.Select((key, index) => new { key, index })
                        .SkipWhile(pair => !pair.key.Equals(KeyToSearch)).FirstOrDefault().index;

            return Index;
        }

        public static string PromptOption(string Question, string[] Options) {
            if (Options.Length == 1)
                return Options.Single();

            var Form = new System.Windows.Forms.Form {
                Size = new System.Drawing.Size(270, 120)
            };

            VSContainer ThemeContainer = new VSContainer() {
                Form = Form,
                FormOrWhole = VSContainer.__FormOrWhole.Form,
                AllowMaximize = false,
                AllowMinimize = false,
                Text = Question
            };
            Form.Controls.Add(ThemeContainer);


            VSComboBox ComboBox = new VSComboBox() {
                Size = new System.Drawing.Size(235, 30),
                Location = new System.Drawing.Point(10, 40)
            };

            foreach (string Language in Options)
                ComboBox.Items.Add(Language);

            ComboBox.SelectedIndex = 0;

            ThemeContainer.Controls.Add(ComboBox);

            Form.ShowDialog(Main.Instance);

            return ComboBox.SelectedItem.ToString();        
        }

        public static string GetImageExtension(this System.Drawing.Image img) {
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Jpeg))
                return "jpg";
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Bmp))
                return "bmp";
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Png))
                return "bmp";
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Emf))
                return "emf";
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Exif))
                return "exif";
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Gif))
                return "gif";
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Icon))
                return "ico";
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.MemoryBmp))
                return "raw";
            if (img.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Tiff))
                return "tiff";
            else
                return "wmf";
        }
    }
}