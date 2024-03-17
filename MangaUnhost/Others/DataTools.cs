using AForge.Imaging.Filters;
using AForge.Imaging;
using HtmlAgilityPack;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using Image = System.Drawing.Image;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Runtime.InteropServices;
using Emgu.CV.Util;
using CefSharp;

namespace MangaUnhost.Others
{

    public static class DataTools
    {

        public static string GetTextFromSpeech(byte[] WAV)
        {
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create("https://api.wit.ai/speech");
            Request.Method = "POST";
            Request.Headers[HttpRequestHeader.Authorization] = "Bearer NVYD6ZUJMC26US5XS2ZJJ32EDZZ654TD";
            Request.ContentType = "audio/wav";
            Request.ContentLength = WAV.Length;
            Request.UserAgent = ProxyTools.UserAgent;
            Request.ServicePoint.Expect100Continue = false;

            using var POST = Request.GetRequestStream();
            new MemoryStream(WAV).CopyTo(POST);
            POST.Flush();
            POST.Close();

            using var RESP = Request.GetResponse();
            using var GET = RESP.GetResponseStream();

            var Buffer = new MemoryStream();
            GET.CopyTo(Buffer);
            RESP.Close();

            var JSON = System.Text.Encoding.UTF8.GetString(Buffer.ToArray());

            return ReadJson(JSON, "_text");
        }

        public static byte[] Mp3ToWav(byte[] MP3)
        {
            using (MemoryStream INPUT = new MemoryStream(MP3))
            using (Mp3FileReader mp3 = new Mp3FileReader(INPUT))
            using (WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(mp3))
            using (MemoryStream OUTPUT = new MemoryStream())
            {
                WaveFileWriter.WriteWavFileToStream(OUTPUT, pcm);
                return OUTPUT.ToArray();
            }
        }

        public static string ReadJson(string JSON, string Name)
        {
            string Finding = string.Format("\"{0}\":", Name);
            int Pos = JSON.IndexOf(Finding) + Finding.Length;
            if (Pos - Finding.Length == -1)
            {
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
            foreach (char c in Cutted)
            {
                if (Close.Contains(c))
                    break;
                Data += c;
            }
            Data = Data.Replace("\\/", "/");
            if (Data.Contains("\\"))
                throw new Exception("Ops... Unsupported Json Format...");

            return Data;
        }
        public static string GetRawName(string PathName, bool DeupperlizerOnly = false, bool FileNameMode = false)
        {
            string Name = DeupperlizerOnly ? PathName.ToLower() : PathName.Replace("-", " ").Replace("_", " ");
            bool Spaced = true;
            string ResultName = string.Empty;
            foreach (char c in Name)
            {
                if (FileNameMode)
                {
                    if (Path.GetInvalidFileNameChars().Contains(c) || Path.GetInvalidPathChars().Contains(c))
                        continue;
                }
                if (c == ' ')
                {
                    Spaced = true;
                    ResultName += ' ';
                    continue;
                }
                if (Spaced)
                {
                    ResultName += c.ToString().ToUpper();
                    Spaced = false;
                }
                else
                    ResultName += c;
            }

            if (!DeupperlizerOnly)
            {
                ResultName = ResultName.TrimStart(' ', '0');
                if (string.IsNullOrWhiteSpace(ResultName))
                    ResultName = "0";
            }

            if (FileNameMode)
                ResultName = ResultName.Replace("...", "");

            return ResultName;
        }

        static public Key ReverseMatch<Key, Value>(this Dictionary<Key, Value> Dictionary, Value ValueToSearch)
        {
            if (!Dictionary.ContainsValue(ValueToSearch))
                throw new Exception("Value not Present in the Dictionary");

            int Index = Dictionary.Values.Select((value, index) => new { value, index })
                        .SkipWhile(pair => !pair.value.Equals(ValueToSearch)).FirstOrDefault().index;

            return Dictionary.Keys.ElementAt(Index);
        }

        static public int IndexOfKey<Key, Value>(this Dictionary<Key, Value> Dictionary, Key KeyToSearch)
        {
            if (!Dictionary.ContainsKey(KeyToSearch))
                throw new Exception("Value not Present in the Dictionary");

            int Index = Dictionary.Keys.Select((key, index) => new { key, index })
                        .SkipWhile(pair => !pair.key.Equals(KeyToSearch)).FirstOrDefault().index;

            return Index;
        }

        public static string GetImageExtension(this System.Drawing.Image img)
        {
            if (img.RawFormat.Guid == ImageFormat.Jpeg.Guid)
                return "jpg";
            if (img.RawFormat.Guid == ImageFormat.Bmp.Guid)
                return "bmp";
            if (img.RawFormat.Guid == ImageFormat.Png.Guid)
                return "png";
            if (img.RawFormat.Guid == ImageFormat.Emf.Guid)
                return "emf";
            if (img.RawFormat.Guid == ImageFormat.Exif.Guid)
                return "exif";
            if (img.RawFormat.Guid == ImageFormat.Gif.Guid)
                return "gif";
            if (img.RawFormat.Guid == ImageFormat.Icon.Guid)
                return "ico";
            if (img.RawFormat.Guid == ImageFormat.MemoryBmp.Guid)
                return "raw";
            if (img.RawFormat.Guid == ImageFormat.Tiff.Guid)
                return "tiff";
            if (img.RawFormat.Guid == ImageFormat.Wmf.Guid)
                return "wmf";
            return "png";
        }
        public static ImageFormat GetImageFormat(this string Format)
        {
            Format = Format.TrimStart('.').ToLower();

            if (Format == "jpg")
                return ImageFormat.Jpeg;
            if (Format == "bmp")
                return ImageFormat.Bmp;
            if (Format == "png")
                return ImageFormat.Png;
            if (Format == "emf")
                return ImageFormat.Emf;
            if (Format == "exif")
                return ImageFormat.Exif;
            if (Format == "gif")
                return ImageFormat.Gif;
            if (Format == "ico")
                return ImageFormat.Icon;
            if (Format == "tiff")
                return ImageFormat.Tiff;
            if (Format == "wmf")
                return ImageFormat.Wmf;

            return ImageFormat.Png;
        }

        public static List<string> ExtractHtmlLinks(string HTML, string Domain, bool BruteMode = false)
        {
            bool Https = Domain.Trim().ToLower().StartsWith("https");

            Domain = Domain.TrimEnd('/');
            if (!Domain.StartsWith("http"))
                Domain += "http://";

            List<string> Links = new List<string>();
            if (!BruteMode)
            {
                var Document = new HtmlAgilityPack.HtmlDocument();
                Document.LoadHtml(HTML);

                var Attributes = new[] { "data-href", "href", "data-url", "url", "data-src", "src" };

                var Nodes = new HtmlNode[0];
                foreach (var Attribute in Attributes)
                {
                    var Result = Document.DocumentNode.SelectNodes($"//*[@{Attribute}]");
                    if (Result != null)
                        Nodes = Nodes.Concat(Result).ToArray();
                }

                foreach (var Node in Nodes)
                {
                    string Link = string.Empty;

                    foreach (var Attribute in Attributes)
                    {
                        if (Node.GetAttributeValue(Attribute, null) != null)
                        {
                            Link = Node.GetAttributeValue(Attribute, null);
                            if (!string.IsNullOrWhiteSpace(Link))
                                break;
                        }
                    }

                    if (Link.StartsWith("//"))
                        Link = $"{(Https ? "https" : "http")}:{Link}";
                    if (Link.StartsWith("/"))
                        Link = Domain + Link;
                    Links.Add(Link);
                }
            }
            else
            {
                int Ind;
                do
                {
                    Ind = HTML.IndexOf("http");
                    if (Ind == -1)
                        break;

                    char? Prefix = null;
                    if (Ind > 0)
                        Prefix = HTML[Ind - 1];

                    HTML = HTML.Substring(Ind);

                    if (HTML.StartsWith("https:\\/\\/") || HTML.StartsWith("http:\\/\\/"))
                        HTML = HTML.Replace("\\/", "/").Replace("\\\"", "\"");

                    if (!HTML.StartsWith("https://") && !HTML.StartsWith("http://"))
                        continue;

                    var End = HTML.IndexOf(Prefix ?? ' ');

                    if (End == -1)
                        End = HTML.Length;

                    Links.Add(HTML.Substring(0, End));

                    HTML = HTML.Substring(End);

                } while (Ind >= 0);
            }


            return Links;
        }

        public static bool AreImagesSimilar(this Image ImgA, Image ImgB)
        {
            var ComparsionFactor = Math.Max(Main.Config.ComparsionFactor, 0.003);

            return Main.Config.UseAForge ?
                ImgA.AForge_AreImagesSimilar(ImgB, 1 - ComparsionFactor) :
                ImgA.OpenCV_AreImagesSimilar(ImgB, ComparsionFactor);
        }

        public static bool OpenCV_AreImagesSimilar(this Image ImgA, Image ImgB, double similarity = 0.039)
        {
            if (similarity > 1)
                throw new ArgumentOutOfRangeException(nameof(similarity));

            Mat image1 = null;
            Mat image2 = null;
            try
            {
                EnsurePairSize(ImgA, ImgB, out image1, out image2);

                if (image1.IsEmpty || image2.IsEmpty)
                {
                    throw new Exception("Failed to read the image");
                }

                Mat mat1 = new Mat();
                Mat mat2 = new Mat();
                CvInvoke.CvtColor(image1, mat1, ColorConversion.Rgb2Hsv);
                CvInvoke.CvtColor(image2, mat2, ColorConversion.Rgb2Hsv);

                mat1.ConvertTo(mat1, DepthType.Cv32F);
                mat2.ConvertTo(mat2, DepthType.Cv32F);

                using VectorOfUMat Vec1 = new VectorOfUMat();
                using VectorOfUMat Vec2 = new VectorOfUMat();
                Vec1.Push(mat1.GetUMat(AccessType.Read));
                Vec2.Push(mat2.GetUMat(AccessType.Read));

                using Mat hist1 = new Mat();
                using Mat hist2 = new Mat();
                CvInvoke.CalcHist(Vec1, new int[] { 0, 1 }, null, hist1, new int[] { 180, 256 }, new float[] { 0, 180, 0, 256 }, false);
                CvInvoke.CalcHist(Vec2, new int[] { 0, 1 }, null, hist2, new int[] { 180, 256 }, new float[] { 0, 180, 0, 256 }, false);

                CvInvoke.Normalize(hist1, hist1, 0, 1, NormType.MinMax);
                CvInvoke.Normalize(hist2, hist2, 0, 1, NormType.MinMax);

                double distance = CvInvoke.CompareHist(hist1, hist2, HistogramCompMethod.Bhattacharyya);

                return distance < similarity;
            }
            catch(Exception ex)
            {
                return true;
            }
            finally
            {
                image1?.Dispose();
                image2?.Dispose();
            }
        }

        private static void EnsurePairSize(Image ImgA, Image ImgB, out Mat image1, out Mat image2)
        {
            image1 = new Mat();
            image2 = new Mat();
            {
                using MemoryStream DataA = new MemoryStream();
                using MemoryStream DataB = new MemoryStream();

                if (ImgA.Size != ImgB.Size)
                {
                    if (ImgA.Width * ImgA.Height < ImgB.Width * ImgB.Height)
                    {
                        using var tmp = new Bitmap(ImgA.Width, ImgA.Height);
                        using var graphics = Graphics.FromImage(tmp);
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.DrawImage(ImgB, 0, 0, tmp.Width, tmp.Height);

                        ImgA.Save(DataA, ImageFormat.Png);
                        tmp.Save(DataB, ImageFormat.Png);
                    }
                    else
                    {
                        using var tmp = new Bitmap(ImgB.Width, ImgB.Height);
                        using var graphics = Graphics.FromImage(tmp);
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.DrawImage(ImgA, 0, 0, tmp.Width, tmp.Height);

                        tmp.Save(DataA, ImageFormat.Png);
                        ImgB.Save(DataB, ImageFormat.Png);
                    }
                }
                else
                {
                    ImgA.Save(DataA, ImageFormat.Png);
                    ImgB.Save(DataB, ImageFormat.Png);
                }

                CvInvoke.Imdecode(DataA.ToArray(), ImreadModes.Color, image1);
                CvInvoke.Imdecode(DataB.ToArray(), ImreadModes.Color, image2);
            }
        }

        public static bool OpenCV_SSIM_AreImageSimilar(this Image ImgA, Image ImgB)
        {
            Mat image1 = null;
            Mat image2 = null;
            try
            {
                EnsurePairSize(ImgA, ImgB, out image1, out image2);

                var SSIM = CalcSSIM(image1, image2);
                return SSIM >= 0.990;
            }
            finally
            {
                image1?.Dispose();
                image2?.Dispose();
            }
        }

        enum RGBIndex
        {
            Red = 0,
            Green,
            Blue,
            All
        }

        static double CalcSSIM(Mat Image1, Mat Image2)
        {
            double[] SSIM = new double[] { -1, -1, -1, -1 };
            const double C1 = 6.5025;
            const double C2 = 58.5225;

            if (SSIM[(int)RGBIndex.All] != -1)
                return SSIM[(int)RGBIndex.All];

            if (Image1 == null)
                throw new Exception("image1 can not be null.");
            if (Image2 == null)
                throw new Exception("image2 can not be null.");

            using Image<Bgr, byte> img1_temp = new Image<Bgr, byte>(Image1);
            using Image<Bgr, byte> img2_temp = new Image<Bgr, byte>(Image2);


            if (img1_temp.Size != img2_temp.Size || img1_temp.NumberOfChannels != img2_temp.NumberOfChannels)
                throw new Exception();

            int imageWidth = img1_temp.Width;
            int imageHeight = img1_temp.Height;
            int nChan = img1_temp.NumberOfChannels;
            Size imageSize = new Size(imageWidth, imageHeight);

            using Image<Bgr, float> img1 = img1_temp.ConvertScale<float>(1.0, 1);
            using Image<Bgr, float> img2 = img2_temp.ConvertScale<float>(1.0, 1);
            using Image<Bgr, byte> diff = img2_temp.Copy();

            using Image<Bgr, float> img1_sq = img1.Pow(2);
            using Image<Bgr, float> img2_sq = img2.Pow(2);
            using Image<Bgr, float> img1_img2 = img1.Mul(img2);

            using Image<Bgr, float> mu1 = img1.SmoothGaussian(11, 11, 1.5, 0);
            using Image<Bgr, float> mu2 = img2.SmoothGaussian(11, 11, 1.5, 0);

            using Image<Bgr, float> mu1_sq = mu1.Pow(2);
            using Image<Bgr, float> mu2_sq = mu2.Pow(2);
            using Image<Bgr, float> mu1_mu2 = mu1.Mul(mu2);

            Image<Bgr, float> sigma1_sq = img1_sq.SmoothGaussian(11, 11, 1.5, 0);
            sigma1_sq = sigma1_sq.AddWeighted(mu1_sq, 1, -1, 0);

            Image<Bgr, float> sigma2_sq = img2_sq.SmoothGaussian(11, 11, 1.5, 0);
            sigma2_sq = sigma2_sq.AddWeighted(mu2_sq, 1, -1, 0);

            Image<Bgr, float> sigma12 = img1_img2.SmoothGaussian(11, 11, 1.5, 0);
            sigma12 = sigma12.AddWeighted(mu1_mu2, 1, -1, 0);

            // (2*mu1_mu2 + C1)
            Image<Bgr, float> temp1 = mu1_mu2.ConvertScale<Single>(2, 0);
            temp1 = temp1.Add(new Bgr(C1, C1, C1));

            // (2*sigma12 + C2)
            Image<Bgr, float> temp2 = sigma12.ConvertScale<Single>(2, 0);
            temp2 = temp2.Add(new Bgr(C2, C2, C2));

            // ((2*mu1_mu2 + C1).*(2*sigma12 + C2))
            using Image<Bgr, float> temp3 = temp1.Mul(temp2);

            // (mu1_sq + mu2_sq + C1)
            temp1 = mu1_sq.Add(mu2_sq);
            temp1 = temp1.Add(new Bgr(C1, C1, C1));

            // (sigma1_sq + sigma2_sq + C2)
            temp2 = sigma1_sq.Add(sigma2_sq);
            temp2 = temp2.Add(new Bgr(C2, C2, C2));            

            // ((mu1_sq + mu2_sq + C1).*(sigma1_sq + sigma2_sq + C2))
            temp1 = temp1.Mul(temp2, 1);

            // ((2*mu1_mu2 + C1).*(2*sigma12 + C2))./((mu1_sq + mu2_sq + C1).*(sigma1_sq + sigma2_sq + C2))
            using Image<Bgr, float> ssim_map = new Image<Bgr, float>(imageSize);
            CvInvoke.Divide(temp3, temp1, ssim_map);

            temp1.Dispose();
            temp2.Dispose();
            sigma1_sq.Dispose();
            sigma2_sq.Dispose();
            sigma12.Dispose();

            Bgr avg = new Bgr();
            MCvScalar sdv = new MCvScalar();
            ssim_map.AvgSdv(out avg, out sdv);

            SSIM[(int)RGBIndex.Red] = avg.Red;
            SSIM[(int)RGBIndex.Green] = avg.Green;
            SSIM[(int)RGBIndex.Blue] = avg.Blue;
            SSIM[(int)RGBIndex.All] = (avg.Red + avg.Green + avg.Blue) / 3.0;


            if (SSIM[(int)RGBIndex.All] == 1)//Same Image
            {
                return SSIM[(int)RGBIndex.All];
            }

            using Image<Gray, float> gray32 = new Image<Gray, float>(imageSize);
            CvInvoke.CvtColor(ssim_map, gray32, ColorConversion.Bgr2Gray);

            using Image<Gray, byte> gray8 = gray32.ConvertScale<byte>(255, 0);
            using Image<Gray, byte> gray1 = gray8.ThresholdBinaryInv(new Gray(254), new Gray(255));

            using VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(gray1, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            /*
            string ImageDifferent = "dbg.png";

            int NumDifferences = contours.Size;
            if (ImageDifferent == null)
                return SSIM[(int)RGBIndex.All];

            for (int i = 0; i < NumDifferences; i++)
            {
                using (VectorOfPoint contour = contours[i])
                {
                    Rectangle rect = CvInvoke.BoundingRectangle(contour);
                    diff.Draw(rect, new Bgr(0, 0, 1), 1);
                }
            }

            try
            {
                diff.Save(ImageDifferent);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            */
            return SSIM[(int)RGBIndex.All];
        }

        public static bool AForge_AreImagesSimilar(this Image ImageA, Image ImageB, double MinSimilarity = 0.997, float threshold = 0.8f)
        {
            if (threshold > 1)
                throw new ArgumentOutOfRangeException(nameof(threshold));

            // Carregar as imagens
            Bitmap image1 = new Bitmap(ImageA);
            Bitmap image2 = new Bitmap(ImageB);

            try
            {
                // Redimensionar as imagens para a mesma resolução
                ResizeNearestNeighbor resizeFilter = new ResizeNearestNeighbor(image1.Width, image1.Height);
                image2 = resizeFilter.Apply(image2);

                // Converter as imagens para escala de cinza para simplificar a comparação
                Grayscale grayscaleFilter = new Grayscale(0.2125, 0.7154, 0.0721);
                using var grayImg1 = grayscaleFilter.Apply(image1);
                using var grayImg2 = grayscaleFilter.Apply(image2);

                var Results = new ExhaustiveTemplateMatching(threshold).ProcessImage(grayImg1.To24bppRgbFormat(), grayImg2.To24bppRgbFormat());

                return Results.Any(x => x.Similarity >= MinSimilarity);
            }
            finally
            {
                image1.Dispose();
                image2.Dispose();
            }
        }
        public static Bitmap To24bppRgbFormat(this Bitmap img)
        {
            return img.Clone(new Rectangle(0, 0, img.Width, img.Height),
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        }
    }
}