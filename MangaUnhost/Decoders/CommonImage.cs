using System.Drawing;
using System.IO;

namespace MangaUnhost.Decoders {
    public class CommonImage : IDecoder {
        public virtual Bitmap Decode(byte[] Data) {
            if (Main.IsWebP(Data))
            {
                Data = Main.DecodeWebP(Data);
            }
            if (Main.IsAvif(Data))
            {
                Data = Main.DecodeAvif(Data);
            }
            MemoryStream Stream = new MemoryStream(Data);
            return Image.FromStream(Stream) as Bitmap;
        }
    }
}
