using System;
using System.Text;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace MangaUnhost {
    class GuerrillaMail : IDisposable {
        /* 
         * GuerrillaMail.cs
         * -------------------------------------------------------------
         * Quick and easy E-mail class for GuerrillaMail
         * Get new email, get messages, send messages then dispose of it
         * 
         * Free to use for whatever purpose
         * https://github.com/Ezzpify/
         *
        */



        /// <summary>
        /// Email content class
        /// </summary>
        public struct Email {
            /// <summary>
            /// ID of email
            /// </summary>
            public int mail_id { get; set; }


            /// <summary>
            /// Address of sender
            /// </summary>
            public string mail_from { get; set; }


            /// <summary>
            /// Email subject
            /// </summary>
            public string mail_subject { get; set; }


            /// <summary>
            /// Email content
            /// </summary>
            public string mail_excerpt { get; set; }


            /// <summary>
            /// Email timestamp
            /// </summary>
            public object mail_timestamp { get; set; }


            /// <summary>
            /// If email is read
            /// This is 0 if unread
            /// </summary>
            public object mail_read { get; set; }


            /// <summary>
            /// Email received date GMT
            /// </summary>
            public string mail_date { get; set; }


            /// <summary>
            /// Email attributes attatched
            /// </summary>
            public object att { get; set; }


            /// <summary>
            /// Email size kb
            /// </summary>
            public string mail_size { get; set; }


            /// <summary>
            /// Email replied to
            /// </summary>
            public string reply_to { get; set; }


            /// <summary>
            /// Content type
            /// Example: text
            /// </summary>
            public string content_type { get; set; }


            /// <summary>
            /// Email recipients
            /// </summary>
            public string mail_recipient { get; set; }


            /// <summary>
            /// Email source id
            /// </summary>
            public int? source_id { get; set; }


            /// <summary>
            /// Email source mail id
            /// </summary>
            public int? source_mail_id { get; set; }


            /// <summary>
            /// Email body
            /// </summary>
            public string mail_body { get; set; }
        }

        private struct EmailInit {
            public string email_addr;
            public long email_timestamp;
            public string alias;
            public string sid_token;
        }

        /// <summary>
        /// Email auth class
        /// </summary>
        public struct Auth {
            /// <summary>
            /// If the request was a success
            /// </summary>
            public bool success;


            /// <summary>
            /// List of error codes if request was not a success
            /// </summary>
            public List<object> error_codes;
        }


        /// <summary>
        /// Main email root class
        /// </summary>
        public struct Response {
            /// <summary>
            /// List of emails in the inbox
            /// </summary>
            public List<Email> list { get; set; }


            /// <summary>
            /// Amount of emails
            /// </summary>
            public string count { get; set; }


            /// <summary>
            /// Authentication status
            /// </summary>
            public Auth auth { get; set; }
        }


        /// <summary>
        /// Cookie container storage
        /// </summary>
        private CookieContainer mCookies = new CookieContainer();


        /// <summary>
        /// Optional proxy address string
        /// </summary>
        private WebProxy mProxy = new WebProxy();


        /// <summary>
        /// If we should use the provided proxy
        /// </summary>
        public bool mUseProxy { get; set; }


        /// <summary>
        /// Email address name
        /// </summary>
        private string mEmailAddress;


        /// <summary>
        /// Email address name
        /// </summary>
        private string mEmailAlias;


        /// <summary>
        /// Initializer for the class with optional proxy
        /// </summary>
        /// <param name="proxy">Proxy address to request through</param>
        public GuerrillaMail(WebProxy proxy = null) : this(null, proxy) { }


        /// <summary>
        /// Initializer for the class using a custom email, with optional proxy
        /// </summary>
        public GuerrillaMail(string email, WebProxy proxy = null) {
            if (proxy != null) {
                mProxy = proxy;
                mUseProxy = true;
            }

            if (string.IsNullOrEmpty(email)) {
                InitializeEmail();
            } else {
                InitializeEmail(email);
            }
        }


        /// <summary>
        /// This initializes the email and inbox on site
        /// </summary>
        private void InitializeEmail(string email = null) {
            /*Initialize the inbox*/
            string Rst;
            if (email == null) {
                Rst = Contact("f=get_email_address");
            } else {
                Rst = Contact("f=set_email_user", string.Format("email_user={0}&lang=en&site={1}", email, GetDomain(0)));
            }
            EmailInit Response = Extensions.JsonDecode<EmailInit>(Rst);
            mEmailAddress = Response.email_addr;
            mEmailAlias = Response.alias;

            /*Delete the automatic welcome email - id is always 1*/
            DeleteSingleEmail(1);
        }


        /// <summary>
        /// Changes the current email address
        /// </summary>
        public void ChangeEmail(string address) {
            InitializeEmail(address);
        }


        /// <summary>
        /// Returns full json response
        /// </summary>
        /// <returns></returns>
        public Response GetContent() {
            string Resp = Contact("f=get_email_list&offset=0");
            var Response = Extensions.JsonDecode<Response>(Resp);
            return Response;
        }


        /// <summary>
        /// Returns all emails in a json string
        /// offset=0 implies getting all emails
        /// </summary>
        /// <returns>Returns list of email</returns>
        public List<Email> GetAllEmails() {
            return GetContent().list;
        }


        /// <summary>
        /// Returns all emails received after a specific email (specified by mail_id)
        /// Example: GetEmailsSinceID(53451833)
        /// </summary>
        /// <param name="mail_id">mail_id of an email</param>
        /// <returns>Returns list of emails</returns>
        public List<Email> GetEmailsSinceID(string mail_id) {
            var emails = Extensions.JsonDecode<Response>(Contact("f=check_email&seq=" + mail_id));
            return emails.list;
        }


        /// <summary>
        /// Returns the last email
        /// If there are no emails it will return empty string
        /// </summary>
        /// <returns>Returns null if no email</returns>
        public Email GetLastEmail() {
            return GetAllEmails().LastOrDefault();
        }

        public Email GetEmail(int mail_id) {
            var emails = Extensions.JsonDecode<Email>(Contact("f=fetch_email&email_id=mr_" + mail_id));
            return emails;
        }


        /// <summary>
        /// Returns our email with a specified domain
        /// </summary>
        /// <param name="domain">Specifies which domain to return (0-8) useful for services that blocks certain domains</param>
        /// <returns>Returns email as string</returns>
        public string GetMyEmail(int domain = 0) {
            /*There are several email domains you can use by default*/
            /*Some sites may block guerrilla mail domains, so we can use several different ones*/
            return mEmailAddress;
        }


        /// <summary>
        /// Returns our alias with a specified domain
        /// </summary>
        /// <param name="domain">Specifies which domain to return (0-8) useful for services that blocks certain domains</param>
        /// <returns>Returns email as string</returns>
        public string GetMyAlias(int domain = 0) {
            /*There are several email domains you can use by default*/
            /*Some sites may block guerrilla mail domains, so we can use several different ones*/
            return string.Format("{0}@", mEmailAlias) + GetDomain(domain);
        }


        /// <summary>
        /// Get the domain matching the given parameter.
        /// </summary>
        /// <param name="domain">Specifies which domain to return (0-8) useful for services that blocks certain domains</param>
        /// <returns></returns>
        private static string GetDomain(int domain) {
            switch (domain) {
                case 1:
                    return "grr.la";
                case 2:
                    return "guerrillamail.biz";
                case 3:
                    return "guerrillamail.com";
                case 4:
                    return "guerrillamail.de";
                case 5:
                    return "guerrillamail.net";
                case 6:
                    return "guerrillamail.org";
                case 7:
                    return "guerrillamailblock.com";
                case 8:
                    return "spam4.me";

                default:
                    return "sharklasers.com";
            }
        }


        /// <summary>
        /// Deletes an array of emails from the mailbox
        /// </summary>
        /// <param name="mail_ids">String array of mail ids</param>
        public void DeleteEmails(IEnumerable<string> mail_ids) {
            /*Go through each array value and format delete string*/
            string idString = string.Empty;
            foreach (string id in mail_ids) {
                /*Example: &email_ids[]53666&email_ids[]53667*/
                idString += string.Format("&email_ids[]{0}", id);
            }

            /*Delete emails*/
            Contact("f=del_email" + idString);
        }


        /// <summary>
        /// Deletes a single email
        /// </summary>
        /// <param name="mail_id">mail_id of an email</param>
        public void DeleteSingleEmail(int mail_id) {
            Contact("f=del_email&email_ids[]=" + mail_id);
        }


        /// <summary>
        /// Calls the page with arguments
        /// </summary>
        /// <param name="parameters">GET arguments</param>
        /// <param name="body">POST arguments</param>
        /// <returns>Returns json</returns>
        private string Contact(string parameters, string body = null) {
            /*Set up the request*/
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://api.guerrillamail.com/ajax.php?" + parameters);
            request.CookieContainer = mCookies;
            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";

            if (!string.IsNullOrEmpty(body)) {
                byte[] buffer = Encoding.UTF8.GetBytes(body);

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.ContentLength = buffer.Length;

                using (Stream steam = request.GetRequestStream())
                    steam.Write(buffer, 0, buffer.Length);
            }

            /*If we're using a proxy*/
            string Proxy = Tools.Proxy;
            if (mUseProxy && Proxy != null)
                request.Proxy = new WebProxy(Proxy);

            try {
                /*Fetch the response*/
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                    if (response.StatusCode == HttpStatusCode.OK) {
                        using (Stream stream = response.GetResponseStream()) {
                            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                            return reader.ReadToEnd();
                        }
                    }
                }
            } catch {
                mUseProxy = true;
            }

            /*Something messed up, returning empty string*/
            return string.Empty;
        }


        /// <summary>
        /// Dispose object
        /// </summary>
        public void Dispose() {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}