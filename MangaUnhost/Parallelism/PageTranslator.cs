using AForge.Math.Metrics;
using CefSharp;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MangaUnhost.Parallelism
{
    internal class PageTranslator : IPacket
    {

        ImageTranslator ImgTranslator = null;

        public PageTranslator()
        {
            //Each instance runs a CEF so...
            GC.AddMemoryPressure(1024 * 1024 * 100);
        }
        ~PageTranslator() {
            Dispose();
        }

        public int PacketID => 1;
        public bool Busy { get; private set; }
        public int ProcessID { get; set; }

        public bool Disposed { get; private set; }
        public NamedPipeServerStream PipeStream { get; set; }

        private NamedPipeClientStream PipeClientStream;

        public void Process(BinaryReader Reader, BinaryWriter Writer)
        {
            PipeClientStream = (NamedPipeClientStream)Reader.BaseStream;

            try
            {
                var Pages = Reader.ReadStringArray();
                var SourceLanguage = Reader.ReadNullableString();
                var TargetLanguage = Reader.ReadNullableString();

                TranslatePages(Pages, SourceLanguage, TargetLanguage);
            }
            finally
            {
                Writer.Write(true);
                Writer.Flush();
            }
        }

        private void TranslatePages(string[] Pages, string SourceLang, string TargetLang)
        {
            var TlPages = new List<string>();

            for (int i = 0; i < Pages.Length; i++)
            {
                var Page = Pages[i];
                var NewPage = Path.Combine(Path.GetDirectoryName(Program.MTLAvailable ? Program.MTLPath : Page), Path.GetFileName(Page));
                var TlPage = NewPage + ".tl.png";

                bool TmpInNewDir = new FileInfo(NewPage).FullName != new FileInfo(Page).FullName;
                if (TmpInNewDir)
                    File.Copy(Page, NewPage, true);


                if (ImgTranslator == null)
                    ImgTranslator = new ImageTranslator(SourceLang, TargetLang);

                var ImgData = File.ReadAllBytes(NewPage);

                for (int x = 3; x >= 0; x--)
                {
                    try
                    {
                        var NewData = AutoSplitAndTranslate(ImgTranslator, ImgData, x);
                        File.WriteAllBytes(TlPage, NewData);
                        break;
                    }
                    catch
                    {
                        ImgTranslator.Reload();
                    }
                }

                var Writer = new BinaryWriter(PipeClientStream, Encoding.UTF8, true);
                Writer.Write(false);
                Writer.Write(i);
                Writer.Write(Pages.Length);
                Writer.Flush();

                TlPages.Add(TlPage);

                if (!File.Exists(TlPage))
                    continue;

                if (TmpInNewDir)
                    File.Delete(NewPage);
            }
        }

        private static byte[] AutoSplitAndTranslate(ImageTranslator ImgTranslator, byte[] ImgData, int TriesLeft = 0)
        {
            bool TooBig = ImgData.Length >= 1024 * 1024 * 10;

            if (!TooBig)
            {
                using MemoryStream ImgStream = new MemoryStream(ImgData);
                using Bitmap img = Bitmap.FromStream(ImgStream) as Bitmap;
                if (img.Width < img.Height / 3)
                    TooBig = true;
            }

            if (TooBig)
            {
                byte[] NewDataA, NewDataB;

                using MemoryStream ImgStream = new MemoryStream(ImgData);
                using Bitmap FullImage = Bitmap.FromStream(ImgStream) as Bitmap;
                {
                    using MemoryStream PartAData = new MemoryStream();
                    using MemoryStream PartBData = new MemoryStream();
                    {
                        using Bitmap PartA = FullImage.Clone(new Rectangle(0, 0, FullImage.Width, FullImage.Height / 2), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        using Bitmap PartB = FullImage.Clone(new Rectangle(0, FullImage.Height / 2, FullImage.Width, FullImage.Height / 2), System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                        PartA.Save(PartAData, System.Drawing.Imaging.ImageFormat.Png);
                        PartB.Save(PartBData, System.Drawing.Imaging.ImageFormat.Png);
                    }


                    NewDataA = AutoSplitAndTranslate(ImgTranslator, PartAData.ToArray(), TriesLeft);
                    NewDataB = AutoSplitAndTranslate(ImgTranslator, PartBData.ToArray(), TriesLeft);
                }

                using MemoryStream NewPartAData = new MemoryStream(NewDataA);
                using Bitmap NewPartA = Bitmap.FromStream(NewPartAData) as Bitmap;
                using MemoryStream NewPartBData = new MemoryStream(NewDataB);
                using Bitmap NewPartB = Bitmap.FromStream(NewPartBData) as Bitmap;

                using Graphics Render = Graphics.FromImage(FullImage);
                Render.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                Render.DrawImage(NewPartA, 0, 0, FullImage.Width, FullImage.Height / 2);
                Render.DrawImage(NewPartB, 0, FullImage.Height / 2, FullImage.Width, FullImage.Height / 2);
                Render.Flush();

                using MemoryStream NewImage = new MemoryStream();
                FullImage.Save(NewImage, System.Drawing.Imaging.ImageFormat.Png);
                return NewImage.ToArray();
            }

            var NewData = ImgTranslator.TranslateImage(ImgData);

            using MemoryStream OriData = new MemoryStream(ImgData);
            using Bitmap OriImage = Bitmap.FromStream(OriData) as Bitmap;

            using Bitmap FinalImage = new Bitmap(OriImage.Width, OriImage.Height);            
            using Graphics graphics = Graphics.FromImage(FinalImage);
            {
                using MemoryStream tmpData = new MemoryStream(NewData);
                using Bitmap tmpImage = Bitmap.FromStream(tmpData) as Bitmap;
                graphics.DrawImage(tmpImage, 0, 0, FinalImage.Width, FinalImage.Height);
            }


            if (OriImage.AreImagesSimilar(FinalImage) && TriesLeft > 0)
            {
                if (TriesLeft > 0)
                {
                    ImgTranslator.Reload();
                    return AutoSplitAndTranslate(ImgTranslator, ImgData, TriesLeft - 1);    
                }

                throw new Exception("Translated Image not Changed");
            }

            using MemoryStream FinalData = new MemoryStream();
            FinalImage.Save(FinalData, System.Drawing.Imaging.ImageFormat.Png);
            return FinalData.ToArray();
        }

        async Task IPacket.Request(params object[] Args)
        {
            if (Disposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (Args.Length != 3) throw new ArgumentException("Must be (1 string array + 2 string) argument list");

            if (!PipeStream.IsConnected) throw new Exception("Pipe Connection Lost");

            if (Busy) throw new Exception("Service Currently in a busy state");

            try
            {
                var Writer = new BinaryWriter(PipeStream, Encoding.UTF8, true);

                await Writer.WriteStringArray((string[])Args[0]);//last chapter
                await Writer.WriteNullableString((string)Args[1]);//source lang
                await Writer.WriteNullableString((string)Args[2]);//target lang
                Writer.Flush();

                Busy = true;
            }
            catch
            {
                Busy = false;
                PipeStream?.Disconnect();
            }
        }

        public async Task<bool> WaitForEnd(Action<int, int> ProgressChanged)
        {
            if (Disposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (!Busy)
                throw new Exception("No task available to wait it.");

            var Reader = new BinaryReader(PipeStream, Encoding.UTF8, true);
            var buffer = new byte[1];
            try
            {
                while (PipeStream.IsConnected)
                {
                    try
                    {
                        var readed = await PipeStream.TimeoutReadAsync(buffer, 0, buffer.Length, TimeSpan.FromSeconds(60 * 3));

                        if (readed <= 0)
                            break;

                        if (buffer[0] != 0)
                        {
                            return true;
                        }

                        if (PipeStream.IsConnected)
                        {
                            var Current = Reader.ReadInt32();
                            var Total = Reader.ReadInt32();
                            ProgressChanged.Invoke(Current, Total);
                        }
                    }
                    catch
                    {
                        break;
                    }
                }

                return false;
            }
            finally {
                Busy = false;
            }
        }
        public void Dispose()
        {
            if (Disposed)
                return;

            Disposed = true;
            Busy = false;
            PipeStream?.Dispose();
            PipeClientStream?.Dispose();
            ImgTranslator?.Dispose();

            GC.RemoveMemoryPressure(1024 * 1024 * 100);

            try
            {
                if (ProcessID != 0)
                    System.Diagnostics.Process.GetProcessById(ProcessID).Kill();
            }
            catch { }
        }
    }
}
