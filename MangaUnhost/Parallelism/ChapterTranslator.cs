using MangaUnhost.Browser;
using MangaUnhost.Others;
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
    internal class ChapterTranslator : IPacket
    {
        ~ChapterTranslator() {
            Dispose();
        }
        public int PacketID => 0;
        public bool Busy { get; private set; }
        public NamedPipeServerStream PipeStream { get; set; }

        private NamedPipeClientStream PipeClientStream;

        public void Process(BinaryReader Reader, BinaryWriter Writer)
        {
            PipeClientStream = (NamedPipeClientStream)Reader.BaseStream;
            var LastChapter = Reader.ReadNullableString();
            var Chapter = Reader.ReadNullableString();
            var NextChapter = Reader.ReadNullableString();
            var SourceLanguage = Reader.ReadNullableString();
            var TargetLanguage = Reader.ReadNullableString();
            var AllowSkip = Reader.ReadBoolean();

            TranslateChapter(Chapter, LastChapter, NextChapter, SourceLanguage, TargetLanguage, AllowSkip);

            Writer.Write(true);
        }

        private void TranslateChapter(string Chapter, string LastChapter, string NextChapter, string SourceLang, string TargetLang, bool AllowSkip)
        {
            var Pages = ListFiles(Chapter, "*.png", "*.jpg", "*.gif", "*.jpeg", "*.bmp")
                .Where(x => !x.EndsWith(".tl.png"))
                .OrderBy(x => int.TryParse(Path.GetFileNameWithoutExtension(x), out int val) ? val : 0).ToArray();

            var ReadyPages = ListFiles(Chapter, "*.png", "*.jpg", "*.gif", "*.jpeg", "*.bmp")
                .Where(x => x.EndsWith(".tl.png"))
                .OrderBy(x => int.TryParse(Path.GetFileNameWithoutExtension(x), out int val) ? val : 0).ToArray();


            var TlPages = new List<string>();

            string Reader = Chapter.TrimEnd('/', '\\') + ".html";

            if (ReadyPages.Length == Pages.Length && AllowSkip)
                return;

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

                    if (File.Exists(TlPage) && AllowSkip)
                    {
                        using var TLImg = Bitmap.FromFile(TlPage);
                        var TLSize = TLImg.Size;

                        using var Img = Bitmap.FromFile(Page);
                        var ImgSize = Img.Size;

                        if (ImgSize == TLSize)
                        {
                            TlPages.Add(TlPage);
                            continue;
                        }
                    }


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

            ChapterTools.GenerateComicReaderWithTranslation(Main.Language, Pages, TlPages.ToArray(), LastChapter, NextChapter, Chapter);
        }

        private string[] ListFiles(string Dir, params string[] Filters)
        {
            List<string> Files = new List<string>();
            foreach (var Filter in Filters)
                Files.AddRange(Directory.GetFiles(Dir, Filter));
            return Files.ToArray();
        }


        void IPacket.Request(params object[] Args)
        {
            if (Args.Length != 6) throw new ArgumentException("Must be (4-string + 1 boolean) argument list");

            if (!PipeStream.IsConnected) throw new Exception("Pipe Connection Lost");

            var Writer = new BinaryWriter(PipeStream, Encoding.UTF8, true);

            Writer.WriteNullableString((string)Args[0]);//last chapter
            Writer.WriteNullableString((string)Args[1]);//chapter
            Writer.WriteNullableString((string)Args[2]);//nextchapter
            Writer.WriteNullableString((string)Args[3]);//source lang
            Writer.WriteNullableString((string)Args[4]);//target lang
            Writer.Write((bool)Args[5]);//Allow Skip
            Writer.Flush();

            Busy = true;
        }

        public async Task<bool> WaitForEnd(Action<int, int> ProgressChanged)
        {
            var Reader = new BinaryReader(PipeStream, Encoding.UTF8, true);
            var buffer = new byte[1];
            while (PipeStream.IsConnected)
            {
                int readed = await PipeStream.ReadAsync(buffer, 0, buffer.Length);

                if (readed == 0)
                    break;

                if (buffer[0] != 0)
                {
                    Busy = false;
                    return true;
                }

                if (PipeStream.IsConnected)
                {
                    var Current = Reader.ReadInt32();
                    var Total = Reader.ReadInt32();
                    ProgressChanged.Invoke(Current, Total);
                }
            }

            Busy = false;
            return false;
        }

        public void Dispose()
        {
            PipeStream.Dispose();
        }
    }
}
