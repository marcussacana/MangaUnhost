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
using System.Threading.Tasks;

namespace MangaUnhost.Parallelism
{
    internal class PageTranslator : IPacket
    {
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
            var Pages = Reader.ReadStringArray();
            var SourceLanguage = Reader.ReadNullableString();
            var TargetLanguage = Reader.ReadNullableString();

            TranslatePages(Pages, SourceLanguage, TargetLanguage);

            Writer.Write(true);
        }

        private void TranslatePages(string[] Pages, string SourceLang, string TargetLang)
        {
            var TlPages = new List<string>();

            ImageTranslator ImgTranslator = null;

            try
            {

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
                            var NewData = ImgTranslator.TranslateImage(ImgData);

                            using var OriData = new MemoryStream(ImgData);
                            using var OriImage = Bitmap.FromStream(OriData);
                            using var OriImageRef = Bitmap.FromStream(OriData);

                            using var NewDataStream = new MemoryStream(NewData);
                            using var NewImage = Bitmap.FromStream(NewDataStream);

                            using var g = Graphics.FromImage(OriImage);
                            g.DrawImage(NewImage, 0, 0, OriImage.Width, OriImage.Height);
                            g.Flush();

                            if (OriImageRef.AreImagesSimilar(OriImage) && x > 0)
                            {
                                throw new Exception("Translated Image not Changed");
                            }

                            OriImage.Save(TlPage);

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
            finally
            {
                ImgTranslator?.Dispose();
            }
        }

        void IPacket.Request(params object[] Args)
        {
            if (Disposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (Args.Length != 3) throw new ArgumentException("Must be (1 string array + 2 string) argument list");

            if (!PipeStream.IsConnected) throw new Exception("Pipe Connection Lost");

            if (Busy) throw new Exception("Service Currently in a busy state");

            try
            {
                var Writer = new BinaryWriter(PipeStream, Encoding.UTF8, true);

                Writer.WriteStringArray((string[])Args[0]);//last chapter
                Writer.WriteNullableString((string)Args[1]);//source lang
                Writer.WriteNullableString((string)Args[2]);//target lang
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

            PipeStream.ReadTimeout = 1000;
            var Reader = new BinaryReader(PipeStream, Encoding.UTF8, true);
            var buffer = new byte[1];
            try
            {
                while (PipeStream.IsConnected)
                {
                    try
                    {

                        int readed = await PipeStream.ReadAsync(buffer, 0, buffer.Length);

                        if (readed == 0)
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

            try
            {
                if (ProcessID != 0)
                    System.Diagnostics.Process.GetProcessById(ProcessID).Kill();
            }
            catch { }
        }
    }
}
