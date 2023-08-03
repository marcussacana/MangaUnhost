using CefSharp;
using CefSharp.DevTools.WebAuthn;
using CefSharp.OffScreen;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace MangaUnhost.Hosts
{
    class BilibiliComics : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var Page in GetChapterPages(ID))
            {
                yield return Page.TryDownload(Referer: $"https://www.bilibilicomics.com/mc{ComicID}/{ID}?from=manga_detail");
            }
        }

        public ChapterInfo? GetChapterInfo(int ID)
        {
            var jsonInfo = Post("https://www.bilibilicomics.com/twirp/comic.v1.Comic/GetImageIndex?mob_app=android_comic&version=2.14.0&device=android&platform=android&appkey=1d8b6e7d45233436&lang=en&sys_lang=en", $"{{\"ep_id\": {ID}}}");

            var RespInfo = JsonConvert.DeserializeObject<ChapterInfoResponse>(jsonInfo);

            if (RespInfo.code != 0)
                return null;

            return RespInfo.data;
        }

        public ChapterInfo? GetPaidChapterInfo(int ChapterID, bool LastAccTried = false)
        {
            Account? Acc = null;
            var FakeAccount = Ini.GetConfig(nameof(BilibiliComics), "FakeAccount", Main.SettingsPath, false)?.ToLower()?.Trim() == "true";
            var AccessToken = Ini.GetConfig(nameof(BilibiliComics), "AccessToken", Main.SettingsPath, false);
            var RefreshToken = Ini.GetConfig(nameof(BilibiliComics), "RefreshToken", Main.SettingsPath, false);

            while (string.IsNullOrEmpty(AccessToken) || string.IsNullOrEmpty(RefreshToken) || (FakeAccount && LastAccTried)) 
            {                
                using var Browser = new ChromiumWebBrowser("https://www.bilibilicomics.com/account");
                Browser.BypassGoogleCEFBlock();
                Browser.Size = new System.Drawing.Size(900, 500);
                Browser.WaitForLoad();

                if (FakeAccount)
                {
                    //Logout last account
                    while (!Browser.GetCurrentUrl().Contains("bilibilicomics.com/account"))
                    {
                        Browser.DeleteCookies();
                        Browser.LoadUrl("https://www.bilibilicomics.com/account");
                        Browser.WaitForLoad();
                    }

                    //Find account look in the old accounts for someone
                    //that are never used with the current comic
                    Acc = FindAccount() ?? CreateAccount();

                    ThreadTools.Wait(1000, true);

                    try
                    {

                        Browser.TypeInInput("document.getElementById('input-20')", Acc.Value.Email);
                        Browser.TypeInInput("document.getElementById('input-23')", Acc.Value.Password);
                    }
                    catch
                    {
                        Browser.DeleteCookies();
                        ThreadTools.Wait(5000, true);
                        continue;
                    }

                    while (Browser.GetCurrentUrl().Contains("bilibilicomics.com/account"))
                    {
                        ThreadTools.Wait(5000, true);

                        Browser.EvaluateScript("document.getElementsByClassName('login-in-btn bili-button')[0].click();");

                        ThreadTools.Wait(500, true);

                        Browser.EvaluateScript("document.getElementsByClassName('confirm-button primary')[0].click();");

                        ThreadTools.Wait(500, true);
                    }
                }
                else
                {
                    var PopUp = new BrowserPopup(Browser, () =>
                    {
                        try
                        {
                            return !Browser.GetCurrentUrl().Contains("bilibilicomics.com/account");
                        }
                        catch
                        {
                            return true;
                        }
                    });

                    PopUp.ShowDialog();
                }



                Browser.WaitForLoad();

                var Cookie = Browser.GetCookie("access_token");
                Cookie = WebUtility.UrlDecode(Cookie);

                var Obj = JsonConvert.DeserializeObject((dynamic)Cookie);

                AccessToken = Obj.accessToken;
                RefreshToken = Obj.refreshToken;
                break;
            }

            dynamic data = new ExpandoObject();
            data.refresh_token = RefreshToken;
            var PostData = JsonConvert.SerializeObject(data);

            try
            {
                var Response = Post("https://us-user.bilibilicomics.com/twirp/global.v1.User/RefreshToken?mob_app=android_comic&version=2.14.0&device=android&platform=android&appkey=1d8b6e7d45233436&lang=en&sys_lang=en", PostData, Authorization: "Bearer " + AccessToken);

                var Obj = JsonConvert.DeserializeObject((dynamic)Response);

                AccessToken = Obj.data.access_token;
                RefreshToken = Obj.data.refresh_token;

                Ini.SetConfig(nameof(BilibiliComics), "AccessToken", AccessToken, Main.SettingsPath);
                Ini.SetConfig(nameof(BilibiliComics), "RefreshToken", RefreshToken, Main.SettingsPath);
            }
            catch
            {
                return null;
            }


            string Credential = null;

            try
            {
                var Response = Post("https://us-user.bilibilicomics.com/twirp/global.v1.User/GetCredential?mob_app=android_comic&version=2.14.0&device=android&platform=android&appkey=1d8b6e7d45233436&lang=en&sys_lang=en", $"{{\"type\": 1, \"comic_id\": {ComicID}, \"ep_id\": {ChapterID}}}", Authorization: "Bearer " + AccessToken);

                var Obj = JsonConvert.DeserializeObject((dynamic)Response);

                Credential = Obj.data.credential;
            }
            catch
            {
                if (!LastAccTried)
                    return GetPaidChapterInfo(ChapterID, true);

                if (FakeAccount)
                {
                    Credential = FreeUnlock(ChapterID, "Bearer " + AccessToken);

                    AccountTools.SaveAccountData(nameof(BilibiliComics), Acc.Value.Email, (Acc.Value.Data ?? "") + $"{ComicID},");


                    Main.Status = "Loading...";
                }
                else
                    return null;
            }


            var jsonInfo = Post("https://www.bilibilicomics.com/twirp/comic.v1.Comic/GetImageIndex?mob_app=android_comic&version=2.14.0&device=android&platform=android&appkey=1d8b6e7d45233436&lang=en&sys_lang=en", $"{{\"ep_id\": {ChapterID}, \"credential\": \"{Credential}\"}}", Authorization: "Bearer " + AccessToken);

            var RespInfo = JsonConvert.DeserializeObject<ChapterInfoResponse>(jsonInfo);

            if (RespInfo.code != 0)
                return null;

            return RespInfo.data;
        }

        private Account? FindAccount()
        {
            var Accs = AccountTools.LoadAccounts(nameof(BilibiliComics));

            foreach (var Acc in Accs)
            {
                if (!Acc.Data.Contains(ComicID))
                    return Acc;
            }

            return null;
        }

        private Account CreateAccount()
        {
            while (true)
            {
                Main.Status = "Creating Account...";

                const string RegisterURL = "https://www.bilibilicomics.com/account/sign-up";

                using var Browser = new ChromiumWebBrowser(RegisterURL);
                Browser.BypassGoogleCEFBlock();
                Browser.Size = new System.Drawing.Size(900, 500);
                Browser.WaitForLoad();

                var Email = new GuerrillaMail();

                ThreadTools.Wait(1000, true);

                try
                {

                    var OK = Browser.TypeInInput("document.getElementById('input-20')", Email.GetMyEmail());
                    OK &= Browser.TypeInInput("document.getElementById('input-23')", "123456anon");

                    if (!OK)
                        continue;
                }
                catch
                {
                    Browser.DeleteCookies();
                    ThreadTools.Wait(5000, true);
                    continue;
                }


                Browser.EvaluateScript("document.getElementById('input-28').value = true;\r\nvar btn = document.forms[0].getElementsByTagName('button')[0];\r\nbtn.removeAttribute(\"disabled\");\r\nbtn.click();");

                ThreadTools.Wait(500, true);

                Browser.EvaluateScript("document.getElementsByClassName('confirm-button primary')[0].click();");

                while (Browser.EvaluateScript<bool>("document.getElementsByClassName('validation-inputs').length == 0"))
                {
                    ThreadTools.Wait(500, true);
                }

                int Tries = 0;
                
                string Code = null;
                while (true)
                {
                    if (Tries > 4)
                        break;
                    Tries++;
                    ThreadTools.Wait(1000 * 10, true);
                    var Emails = Email.GetAllEmails();

                    var Target = Emails.Where(x => x.mail_from?.Contains("bilibili") ?? false);

                    if (!Target.Any())
                        continue;

                    var Mail = Email.GetEmail(Target.First().mail_id);
                    var Doc = new HtmlAgilityPack.HtmlDocument();
                    Doc.LoadHtml(Mail.mail_body);

                    var Node = Doc.SelectSingleNode("//h2");

                    Code = Node.InnerText;
                    break;
                }

                if (Tries > 4)
                    continue;

                var Cookies = Browser.GetCookies();

                var JSON = $"{{\"email\":\"{Email.GetMyEmail()}\",\"type\":1,\"code\":\"{Code}\"}}";

                var Resp = Post("https://us-user.bilibilicomics.com/twirp/global.v1.User/VerifyCode?device=pc&platform=web&lang=en&sys_lang=en", JSON);

                var Token = JsonConvert.DeserializeObject<VerifyCodeResponse>(Resp);

                if (Token.code != 0)
                    continue;

                JSON = $"{{\"email\":\"{Email.GetMyEmail()}\",\"password\":\"{EncryptPassword("123456anon")}\",\"token\":\"{Token.data.token}\"}}";
                Resp = Post("https://us-user.bilibilicomics.com/twirp/global.v1.User/Register?device=pc&platform=web&lang=en&sys_lang=en", JSON);

                var Acc = new Account()
                {
                    Email = Email.GetMyEmail(),
                    Password = "123456anon"
                };

                AccountTools.SaveAccount(nameof(BilibiliComics), Acc);

                return Acc;
            }
        }

        public string EncryptPassword(string Password)
        {
            var RespJS = Post("https://www.bilibilicomics.com/twirp/global.v1.Comic/GetPasswordPk?device=pc&platform=web&lang=en&sys_lang=en", "{}");

            var Resp = JsonConvert.DeserializeObject<PassowordPublicKeyResponse>(RespJS);

            if (Resp.code != 0)
                return null;

            RSA Algo;

            using (MemoryStream Stream = new MemoryStream(Encoding.UTF8.GetBytes(Resp.data.pk)))
            using (TextReader Reader = new StreamReader(Stream))
            {
                var PEM = new PemReader(Reader);
                var Key = (RsaKeyParameters)PEM.ReadObject();
                Algo = DotNetUtilities.ToRSA(Key);
            }            

            var Pass = Algo.Encrypt(Encoding.UTF8.GetBytes(Password), RSAEncryptionPadding.Pkcs1);

            return Convert.ToBase64String(Pass);
        }

        public string FreeUnlock(int ChapterID, string Authorization)
        {
            Main.Status = "Unlocking Chapter...";

            var Response = Post("https://www.bilibilicomics.com/twirp/global.v1.Comic/GetCredential?mob_app=android_comic&version=2.14.0&device=android&platform=android&appkey=1d8b6e7d45233436&lang=en&sys_lang=en", $"{{\"type\": 1, \"comic_id\": {ComicID}, \"ep_id\": {ChapterID}}}", Authorization: Authorization);

            var Obj = JsonConvert.DeserializeObject((dynamic)Response);

            string Credential = Obj.data.credential;

            var jsonReq = $"{{\"credential\":\"{Credential}\",\"comic_id\":{ComicID},\"buff_id\":0}}";

            var Resp = Post("https://us-user.bilibilicomics.com/twirp/comic.v1.User/ActiveComicWaitFree?mob_app=android_comic&version=2.14.0&device=android&platform=android&appkey=1d8b6e7d45233436&lang=en&sys_lang=en", jsonReq, Authorization: Authorization);
            
            bool Success = Resp.Contains("\"code\":0");

            if (!Success)
                return null;

            while (true)
            {
                jsonReq = $"{{\"comic_id\":{ComicID}}}";

                Resp = Post("https://us-user.bilibilicomics.com/twirp/comic.v1.User/GetComicWaitFreeInfo?mob_app=android_comic&version=2.14.0&device=android&platform=android&appkey=1d8b6e7d45233436&lang=en&sys_lang=en", jsonReq, Authorization: Authorization);

                Success = Resp.Contains("\"code\":0");

                var Time = DataTools.ReadJson(Resp, "remain_wait_time");
                if (Time == "0")
                    break;

                ThreadTools.Wait(5000, true);
            }

            Resp = Post("https://www.bilibilicomics.com/twirp/global.v1.Comic/GetCredential?mob_app=android_comic&version=2.14.0&device=android&platform=android&appkey=1d8b6e7d45233436&lang=en&sys_lang=en", $"{{\"type\": 2, \"comic_id\": {ComicID}, \"ep_id\": {ChapterID}}}", Authorization: Authorization);
            
            Obj = JsonConvert.DeserializeObject((dynamic)Resp);

            var Cred = Obj.data.credential;

            jsonReq = $"{{\"credential\":\"{Cred}\",\"comic_id\":{ComicID},\"ep_id\":{ChapterID},\"buff_id\":0}}";

            Resp = Post("https://us-user.bilibilicomics.com/twirp/comic.v1.User/WaitFreeEp?mob_app=android_comic&version=2.14.0&device=android&platform=android&appkey=1d8b6e7d45233436&lang=en&sys_lang=en", jsonReq, Authorization: Authorization);

            Obj = JsonConvert.DeserializeObject((dynamic)Resp);

            return Obj.data.credential;
        }

        public string[] GetChapterPages(int ID)
        {

            var Chapter = FreeMap[ID] ? GetChapterInfo(ID) : GetPaidChapterInfo(ID);

            if (Chapter == null) throw new Exception("Failed to get Chapter Info");

            int Quality = Chapter.Value.images.First().x;
            if (Quality >= 2000)
                Quality = 2000;
            else if (Quality >= 1600)
                Quality = 1600;
            else if (Quality >= 1200)
                Quality = 1200;
            else if (Quality >= 1000)
                Quality = 1000;

            List<string> Urls = new List<string>();
            foreach (var Image in Chapter.Value.images)
            {
                string Path = $"{Image.path}@{Quality}w.webp";

                var Req = new GetTokenRequest() { urls = $"[\"{Path}\"]" };

                var jsonReq = JsonConvert.SerializeObject(Req);

                var tokenResp = Post("https://www.bilibilicomics.com/twirp/comic.v1.Comic/ImageToken?mob_app=android_comic&version=2.14.0&device=android&platform=android&appkey=1d8b6e7d45233436&lang=en&sys_lang=en", jsonReq);

                var TokenInfo = JsonConvert.DeserializeObject<GetTokenResponse>(tokenResp);
                if (TokenInfo.code != 0)
                    throw new Exception(TokenInfo.msg);

                var Token = TokenInfo.data.First();

                Urls.Add($"{Token.url}?token={Token.token}");
            }

            return Urls.ToArray();
        }

        int FreeID = 0;

        Dictionary<int, bool> FreeMap = new Dictionary<int, bool>();

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            foreach (var Chap in Info.ep_list/*.Where(x => x.pay_gold == 0)*/.OrderByDescending(x => x.ord))
            {
                FreeMap[Chap.id] = Chap.pay_gold == 0;
                if (Chap.pay_gold == 0)
                    FreeID = Chap.id;

                int ID = Chap.ord;
                int.TryParse(Chap.short_title, out ID);

                yield return new KeyValuePair<int, string>(Chap.id, ID.ToString());
            }
        }

        public int GetChapterPageCount(int ID)
        {
            return (FreeMap[ID] ? GetChapterInfo(ID) : GetPaidChapterInfo(ID)).Value.images.Count;
        }

        public IDecoder GetDecoder()
        {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "BiliBiliComics",
                Author = "Marcussacana",
                SupportComic = true,
                Version = new Version(2, 1)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            return false;
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.AbsoluteUri.ToLowerInvariant().Contains("detail/mc") && Uri.Host.ToLowerInvariant().Contains("bilibilicomics.com");
        }

        public ComicInfo LoadUri(Uri Uri)
        {
            ComicID = Uri.AbsoluteUri.Substring("/mc").Split('?').First();
            var jsonInfo = Post("https://www.bilibilicomics.com/twirp/comic.v1.Comic/ComicDetail?device=android&lang=en&sys_lang=en", $"{{\"comic_id\":{ComicID}}}");

            var Response = JsonConvert.DeserializeObject<ComicInfoResponse>(jsonInfo);
            if (Response.code != 0)
                throw new Exception(Response.msg);

            Info = Response.data;

            return new ComicInfo()
            {
                Title = Info.title,
                ContentType = ContentType.Comic,
                Cover = Info.vertical_cover.Download(Referer: "https://www.bilibilicomics.com/"),
                Url = Uri
            };
        }

        string ComicID;
        BiliBiliComicInfo Info;



        public string Post(string URI, string Data, string ContentType = "application/json;charset=UTF-8", string Referer = "https://www.bilibilicomics.com/", string Authorization = null)
        {
            var Request = WebRequest.CreateHttp(URI);
            Request.Method = "POST";
            Request.ContentType = ContentType;
            Request.Referer = Referer;
            Request.UserAgent = ProxyTools.UserAgent;

            if (Authorization != null)
               Request.Headers[HttpRequestHeader.Authorization] = Authorization;

            using (var DataStream = new MemoryStream(Encoding.UTF8.GetBytes(Data)))
            using (var SendStream = Request.GetRequestStream())
            {
                DataStream.CopyTo(SendStream);
            }

            var Response = Request.GetResponse();
            using (var ResponseData = new MemoryStream())
            using (var ResponseStream = Response.GetResponseStream())
            {
                ResponseStream.CopyTo(ResponseData);

                var FinalData = ResponseData.ToArray();
                return Encoding.UTF8.GetString(FinalData);
            }
        }
        struct ComicInfoResponse
        {
            public int code;
            public string msg;
            public BiliBiliComicInfo data;
        }
        struct PassowordPublicKeyResponse
        {
            public int code;
            public string msg;
            public PasswordPublicKeyInfo data;
        }
        struct VerifyCodeResponse
        {
            public int code;
            public string msg;
            public VerifyCodeInfo data;
        }

        struct VerifyCodeInfo
        {
            public string token;
        }

        struct PasswordPublicKeyInfo
        {
            public string pk;
        }

        struct BiliBiliComicInfo
        {
            public int id;
            public string title;
            public int comic_type;
            public int page_default;
            public int page_allow;
            public string horizontal_cover;
            public string square_cover;
            public string vertical_cover;
            public List<string> author_name;
            public List<string> styles;
            public int last_ord;
            public int is_finish;
            public int status;
            public string evaluate;
            public int total;
            public List<ComicEpisode> ep_list;

        }

        struct ComicEpisode
        {
            public int id;
            public int ord;
            public int read;
            public int play_mode;
            public bool is_locked;
            public int pay_gold;
            public long size;
            public string short_title;
            public bool is_in_free;
            public string title;
            public string cover;
            public string pub_time;
            public int comments;
            public bool allow_wait_free;
            public string progress;
            public int like_count;
            public int chapter_id;
            public int type;
            public int origin_gold;
            public bool is_risky;
        }
        public struct ChapterInfoResponse {
            public int code;
            public string msg;
            public ChapterInfo data;
        }

        public struct ChapterInfo {
            public string path;
            public List<ImageInfo> images;
        }

        public struct ImageInfo
        {
            public string path;
            public int x;
            public int y;
        }

        public struct GetTokenResponse
        {
            public int code;
            public string msg;
            public List<TokenData> data;
        }

        public struct TokenData
        {
            public string url;
            public string token;
        }

        public struct GetTokenRequest
        {
            public string urls;
        }
    }
}
