﻿using System;
using System.Drawing;

namespace MangaUnhost {
    internal class BitmapTrim : IDisposable {
        Bitmap Texture;
        public BitmapTrim(Bitmap Image) => Texture = Image;

        public static void DoTrim(string ImagePath, int BufferFactor = 1) {
            BitmapTrim Cropper;
            Bitmap Result;
            int Height;
            using (Bitmap Source = Image.FromFile(ImagePath) as Bitmap) {
                Height = Source.Height;
                using (Cropper = new BitmapTrim(Source)) {
                    Cropper.BufferLenght /= BufferFactor;
                    Result = Cropper.Trim();
                    Source.Dispose();
                    Cropper.Dispose();
                }
            }

            using (Cropper = new BitmapTrim(Result)) {
                Cropper.BufferLenght /= BufferFactor;
                Result = Cropper.Trim(false);

                if (Height == Result.Height) {
                    Result.Dispose();
                    return;
                }

                Result.Save(ImagePath);
                Result.Dispose();
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

        bool IsWhiteLine(Bitmap Image, int Y) {
            if (Y > Image.Height)
                return false;

            for (int X = 0; X < Image.Width; X++) {
                Color Pixel = Image.GetPixel(X, Y);
                if (Pixel == Color.White)
                    continue;

                if (Pixel.R >= 240 && Pixel.G >= 240 && Pixel.B >= 240)
                    continue;

                return false;
            }
            return true;
        }

        bool IsBlackLine(Bitmap Image, int Y) {
            if (Y > Image.Height)
                return false;

            for (int X = 0; X < Image.Width; X++) {
                Color Pixel = Image.GetPixel(X, Y);
                if (Pixel == Color.White)
                    continue;

                if (Pixel.R <= 40 && Pixel.G <= 40 && Pixel.B <= 40)
                    continue;

                return false;
            }
            return true;
        }

        public void Dispose() {
            Texture.Dispose();
        }
    }
}