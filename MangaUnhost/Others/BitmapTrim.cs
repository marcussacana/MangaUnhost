using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms.VisualStyles;

namespace MangaUnhost {
    internal unsafe class BitmapTrim : IDisposable {
        Bitmap Texture;
        public BitmapTrim(Bitmap Image) => Texture = Image;

        public static void DoTrim(string ImagePath, int BufferFactor = 1) {
            var IMG = File.ReadAllBytes(ImagePath);
            IMG = DoTrim(IMG, BufferFactor);
            File.WriteAllBytes(ImagePath, IMG);
        }
        public static byte[] DoTrim(byte[] ImageData, int BufferFactor = 1) {
            BitmapTrim Cropper;
            Bitmap Result;
            ImageFormat InputFormat;
            int Height;
            using (MemoryStream Buffer = new MemoryStream(ImageData))
            using (Bitmap Source = Image.FromStream(Buffer) as Bitmap) {
                InputFormat = Source.RawFormat;
                Height = Source.Height;
                using (Cropper = new BitmapTrim(Source)) {
                    Cropper.BufferLenght /= BufferFactor;
                    Result = Cropper.Trim();
                    Source.Dispose();
                    Cropper.Dispose();
                }
            }

            using (MemoryStream Buffer = new MemoryStream())
            using (Cropper = new BitmapTrim(Result)) {
                Cropper.BufferLenght /= BufferFactor;
                Result = Cropper.Trim(false);

                if (Height == Result.Height) {
                    Result.Dispose();
                    return ImageData;
                }

                Result.Save(Buffer, InputFormat);
                Result.Dispose();
                return Buffer.ToArray();
            }
        }


        /// <summary>
        /// Trim the whitespace or blackspace in the image
        /// </summary>
        /// <param name="White">When true, trim the white space, when false, trim the blackspace.</param>
        /// <returns>The cropped image</returns>
        public Bitmap Trim(bool White = true) {
            int BY = -1;
            int Y = 0;
            Bitmap Result = new Bitmap(Texture);
            while (Y < Result.Height) {
                if (White ? !IsWhiteLine(Result, Y) : !IsBlackLine(Result, Y)) {
                    if (BY == -1) {
                        Y++;
                        continue;
                    }

                    int Lines = Y - BY;
                    if (Lines > 3) {
                        Result = CropOff(Result, BY, Y);
                        Y = BY;
                        continue;
                    }
                    BY = -1;
                    continue;
                }

                if (BY == -1)
                    BY = Y;
                Y++;
            }

            if (BY != -1 && Y - BY > 3) {
                Result = CropOff(Result, BY, Y);
            }

            return Result;
        }

        public static List<Rectangle> ExtractContentBands(Bitmap Image, int MinSeparatorHeight = 4) {
            List<Rectangle> Bands = new List<Rectangle>();
            int ContentStart = 0;
            int SeparatorStart = -1;
            int MinBlockHeightForMonotoneSplit = (int)Math.Ceiling(Image.Width * 1.2d);

            for (int Y = 0; Y < Image.Height; Y++) {
                bool CanUseMonotoneLine = (Y - ContentStart) > MinBlockHeightForMonotoneSplit;
                bool IsSeparator = IsWhiteLine(Image, Y) || IsBlackLine(Image, Y) || (CanUseMonotoneLine && IsMonotoneLine(Image, Y));
                if (IsSeparator) {
                    if (SeparatorStart == -1)
                        SeparatorStart = Y;
                    continue;
                }

                if (SeparatorStart == -1)
                    continue;

                if (Y - SeparatorStart >= MinSeparatorHeight) {
                    if (SeparatorStart > ContentStart)
                        Bands.Add(new Rectangle(0, ContentStart, Image.Width, SeparatorStart - ContentStart));

                    ContentStart = Y;
                }

                SeparatorStart = -1;
            }

            if (SeparatorStart != -1) {
                if (Image.Height - SeparatorStart >= MinSeparatorHeight && SeparatorStart > ContentStart)
                    Bands.Add(new Rectangle(0, ContentStart, Image.Width, SeparatorStart - ContentStart));
            }
            else if (Image.Height > ContentStart) {
                Bands.Add(new Rectangle(0, ContentStart, Image.Width, Image.Height - ContentStart));
            }

            if (Bands.Count == 0 && Image.Height > 0)
                Bands.Add(new Rectangle(0, 0, Image.Width, Image.Height));

            return Bands;
        }



        public int BufferLenght = 2000;
        Bitmap CropOff(Bitmap Original, int BeginY, int EndY) {
            Bitmap Result = null;
            int OutHeight = Original.Height - (EndY - BeginY);
            if (OutHeight == 0)
                return Original;

            Result = new Bitmap(Original.Width, OutHeight);
            using (Graphics g = Graphics.FromImage(Result)) {
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                if (BeginY > 0) {
                    for (int y = 0; y < BeginY; y += BufferLenght) {
                        int Reaming = y + BufferLenght > BeginY ? BeginY - y : BufferLenght;
                        using (Bitmap CloneA = Original.Clone(new Rectangle(0, y, Original.Width, Reaming), System.Drawing.Imaging.PixelFormat.DontCare) as Bitmap) {
                            g.DrawImageUnscaled(CloneA, 0, y);
                            g.Flush();
                            CloneA.Dispose();
                        }
                    }
                }
                int SufixHeight = Original.Height - EndY;
                if (SufixHeight > 0) {
                    for (int y = 0; y < SufixHeight; y += BufferLenght) {
                        int Reaming = y + BufferLenght > SufixHeight ? SufixHeight - y : BufferLenght;
                        using (Bitmap CloneB = Original.Clone(new Rectangle(0, y + EndY, Original.Width, Reaming), System.Drawing.Imaging.PixelFormat.DontCare) as Bitmap) {
                            g.DrawImage(CloneB, 0, BeginY + y);
                            g.Flush();
                            CloneB.Dispose();
                        }
                    }
                }

                g.Dispose();

                Original.Dispose();

                return Result;
            }
        }
        public static bool IsWhiteLine(Bitmap image, int y)
        {
            return Scan(image, y, IsWhiteLineInternal);
        }

        public static bool IsBlackLine(Bitmap image, int y)
        {
            return Scan(image, y, IsBlackLineInternal);
        }

        public static bool IsMonotoneLine(Bitmap image, int y, int tolerance = 5)
        {
            return Scan(image, y, (row, width) => IsMonotoneLineInternal((byte*)row.ToPointer(), width, tolerance));
        }

        private static bool Scan(Bitmap image, int y, Func<IntPtr, int, bool> predicate)
        {
            if (y < 0 || y >= image.Height)
                return false;

            BitmapData data = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                byte* row = (byte*)data.Scan0 + (y * data.Stride);
                return predicate(new IntPtr(row), image.Width);
            }
            finally
            {
                image.UnlockBits(data);
            }
        }


        private static bool IsWhiteLineInternal(IntPtr row, int width) => IsWhiteLineInternal((byte*)row.ToPointer(), width);
        private static bool IsWhiteLineInternal(byte* row, int width)
        {
            for (int x = 0; x < width; x++)
            {
                byte b = row[x * 4 + 0];
                byte g = row[x * 4 + 1];
                byte r = row[x * 4 + 2];

                if (r < 240 || g < 240 || b < 240)
                    return false;
            }

            return true;
        }

        private static bool IsBlackLineInternal(IntPtr row, int width) => IsBlackLineInternal((byte*)row.ToPointer(), width);
        private static bool IsBlackLineInternal(byte* row, int width)
        {
            for (int x = 0; x < width; x++)
            {
                byte b = row[x * 4 + 0];
                byte g = row[x * 4 + 1];
                byte r = row[x * 4 + 2];

                if (r > 30 || g > 30 || b > 30)
                    return false;
            }

            return true;
        }

        private static bool IsMonotoneLineInternal(byte* row, int width, int tolerance)
        {
            long totalLum = 0;

            // primeira passada: média
            for (int x = 0; x < width; x++)
            {
                byte b = row[x * 4 + 0];
                byte g = row[x * 4 + 1];
                byte r = row[x * 4 + 2];

                totalLum += (r + g + b) / 3;
            }

            int avg = (int)(totalLum / width);

            // segunda passada: validar desvio
            for (int x = 0; x < width; x++)
            {
                byte b = row[x * 4 + 0];
                byte g = row[x * 4 + 1];
                byte r = row[x * 4 + 2];

                int lum = (r + g + b) / 3;

                if (Math.Abs(lum - avg) > tolerance)
                    return false;
            }

            return true;
        }

        public void Dispose() {
            Texture.Dispose();
        }
    }
}
