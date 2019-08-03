﻿using System.Drawing;
using System.IO;

namespace MangaUnhost.Decoders {
    class CommonImage : IDecoder {
        public Bitmap Decode(byte[] Data) {
            MemoryStream Stream = new MemoryStream(Data);
            return Image.FromStream(Stream) as Bitmap;
        }
    }
}
