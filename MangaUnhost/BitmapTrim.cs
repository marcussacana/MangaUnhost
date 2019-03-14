using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace MangaUnhost {
    internal class BitmapTrim {
        Bitmap Texture;
        public BitmapTrim(Bitmap Image) => Texture = Image;



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

        Bitmap CropOff(Bitmap Original, int BeginY, int EndY) {
            int OutHeight = Original.Height - (EndY - BeginY);
            if (OutHeight == 0)
                return Original;

            Bitmap Result = new Bitmap(Original.Width, OutHeight);
            using (Graphics g = Graphics.FromImage(Result)) {
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                if (BeginY > 0) {
                    Bitmap CloneA = Original.Clone(new Rectangle(0, 0, Original.Width, BeginY), System.Drawing.Imaging.PixelFormat.Format32bppArgb) as Bitmap;
                    g.DrawImageUnscaled(CloneA, 0, 0);
                    CloneA.Dispose();
                }
                int SufixHeight = Original.Height - EndY;
                if (SufixHeight > 0) {
                    Bitmap CloneB = Original.Clone(new Rectangle(0, EndY, Original.Width, SufixHeight), System.Drawing.Imaging.PixelFormat.Format32bppArgb) as Bitmap;
                    g.DrawImageUnscaled(CloneB, 0, BeginY);
                    g.Flush();
                    CloneB.Dispose();
                }

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
    }
}
