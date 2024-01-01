using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MangaUnhost.Decoders
{
    internal class AvifDecoder : CommonImage
    {
        public override Bitmap Decode(byte[] Data)
        {
            if (Data.Length < 0xC)
                return base.Decode(Data);

            if (BitConverter.ToUInt32(Data.Skip(8).Take(4).ToArray(), 0) != 0x66697661)
                return base.Decode(Data);

            Dictionary<string, object> postParameters = new Dictionary<string, object>();
            postParameters.Add("file", new FormUpload.FileParameter(Data, "img.avif", "image/avif"));
            postParameters.Add("targetformat", "png");
            postParameters.Add("imagequality", "100");
            postParameters.Add("imagesize", "option1");
            postParameters.Add("customsize", "");
            postParameters.Add("code", "83000");
            postParameters.Add("filelocation", "local");
            postParameters.Add("oAuthToken", "");

            // Create request and receive response
            string postURL = "https://s12.aconvert.com/convert/convert-batch-win3.php";
            using HttpWebResponse webResponse = FormUpload.MultipartFormDataPost(postURL, "https://www.aconvert.com/", ProxyTools.UserAgent, postParameters);

            // Process response
            StreamReader responseReader = new StreamReader(webResponse.GetResponseStream());
            string fullResponse = responseReader.ReadToEnd();
            webResponse.Close();

            var Server = DataTools.ReadJson(fullResponse, "server");
            var filename = DataTools.ReadJson(fullResponse, "filename");
            var state = DataTools.ReadJson(fullResponse, "state");

            if (state != "SUCCESS")
                return base.Decode(Data);

            //https://s12.aconvert.com/convert/p3r68-cdx67/aceyq-dv0y6.png
            var OutUrl = new Uri($"https://s{Server}.aconvert.com/convert/p3r68-cdx67/{filename}");

            var Decoded = OutUrl.Download("https://www.aconvert.com/image/avif-to-png/",  ProxyTools.UserAgent);
            return base.Decode(Decoded);
        }
    }
}
