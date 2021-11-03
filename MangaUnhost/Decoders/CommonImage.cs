using System.Drawing;
using System.IO;

namespace MangaUnhost.Decoders {
    public class CommonImage : IDecoder {
        public virtual Bitmap Decode(byte[] Data) {
            MemoryStream Stream = new MemoryStream(Data);
            return Image.FromStream(Stream) as Bitmap;
        }
    }
}
