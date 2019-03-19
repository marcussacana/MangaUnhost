     
//System.Text.RegularExpressions
namespace TempMailAPI {
	/// <summary>
	/// Description of TempMail.
	/// </summary>
	public class TempMail : HttpClient {

		public TempMail () {
            this.CreateSession();
            this.GetAvailableDomains();
        }

		private bool InvalidLogin (string login, string domain) {
			return string.IsNullOrEmpty (login) || string.IsNullOrEmpty (domain);
		}
		private bool HasDomains { get { return this.AvailableDomains.Count > 0; } }
		/// <summary>
		/// temp.GetEmailsReceived ();
		/// </summary>
		/// <returns></returns>
		public System.Collections.Generic.List <TempMailAPI.Models.Mail> GetEmailsReceived (string target = "") {
			var mails = new System.Collections.Generic.List <TempMailAPI.Models.Mail> ();
			
			using (var client = this.CreateHttpClient (Constants.HeadersDics)) {
            	string response = client.DownloadString (Constants.URL_CHECK);
	            response = Constants.RegexObjects ["Lines"].Replace (response, string.Empty);
	            response = Constants.RegexObjects ["SpacesBetweenTags"].Replace (response, "><");
	
	            var ids = new System.Collections.Generic.List <string> ();
	            
	            var matches = Constants.RegexObjects ["MailsIds"].Matches (response);
	            for (int i = 0; i < matches.Count; i ++) {
	            	var id = matches [i].Groups ["id"].Value;
	            	if (!ids.Contains (id)) {
	                    var mail = new TempMailAPI.Models.Mail (this, id);
	                    
	                    var source = this.GET (Constants.URL_SOURCE + id);
	                    var result = System.Text.RegularExpressions.Regex.Split (source, "\r\n|\n|\r");
	                    
	                    for (int j = 0; j < result.Length; j ++) {
			            	if (result [j].Length > 0 && result [j] [0] != ' ' &&
			            	    				result [j] [0] != '\t' && result [j].Contains(":")) {
			            		var index = result [j].IndexOf (':');
			
			                    var name = result [j].Substring (0, index);
			                    var value = result [j].Substring (index + 1).Trim ();
			
			                    if (name == "Subject") mail.Subject = value;
			                    else if (name == "From") mail.From = value;
			                    else if (name == "To") mail.To = value;
			                    else if (name == "Date") mail.Date = value;
			                }
			            }
			            mail.Content = new System.Text.RegularExpressions.Regex("--.*\r\nContent-Type: text/plain; charset=UTF-8\r\n\r\n(?<text>.*?)\r\n\r\n--.*", System.Text.RegularExpressions.RegexOptions.Singleline).Match(source).Groups["text"].Value.Trim();
	                    
			            if (target.Length > 0 && target == mail.From || target.Length == 0) mails.Add (mail);
	                    ids.Add (id);
	                }
	            }
			}
			
			return mails;
		}
		/// <summary>
        /// Sends a get request to the Url provided using this session cookies and returns the string result.
        /// </summary>
        /// <param name="url"></param>
        public string GET (string url) {
            return this.CreateHttpClient (Constants.HeadersDics).DownloadString (url);
        }

		private void CreateSession () {
			using (var client = this.CreateHttpClient (Constants.HeadersDics)) {
				string response = client.DownloadString (Constants.URL_BASE);
				
				var matches = Constants.RegexObjects ["Mail"].Matches (response);
				if (matches.Count > 0) {
					string email = matches [0].Groups ["email"].Value;
					this.User = email.Substring (0, email.IndexOf ('@'));
					this.Domain = email.Substring (email.IndexOf ('@') + 1);
				}
				this.cookies = client.CookieContainer;
			}
		}
        /// <summary>
        /// temp.GetAvailableDomains ();
        /// </summary>
		private void GetAvailableDomains () {
        	var domains = new System.Collections.Generic.List <string> ();

			using (var client = CreateHttpClient (Constants.HeadersDics)) {
				string res = client.DownloadString (Constants.URL_CHANGE);
				res = Constants.RegexObjects ["Lines"].Replace (res, string.Empty);
				res = Constants.RegexObjects ["SpacesBetweenTags"].Replace (res, "><");
				var matches = Constants.RegexObjects ["Domains"].Matches (res);
				if (matches.Count > 0) {
					for (int i = 0; i < matches [0].Groups ["domain"].Captures.Count; i ++) {
						domains.Add (matches [0].Groups ["domain"].Captures [i].Value.Substring (1));
					}
					this.AvailableDomains = domains;
				}
				this.cookies = client.CookieContainer;
			}
        }
		
        /// <summary>
        /// temp.Change ();
        /// temp.Change (login);
        /// temp.Change (login, domain);
        /// </summary>
        /// <param name="login"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
		public bool Change (string login, string domain) {
			if (login == this.User && this.Domain == Domain) return false;
			
			if (this.InvalidLogin (login, domain))
				throw new System.Exception ("");
			if (!this.HasDomains) this.GetAvailableDomains ();
			if (!this.AvailableDomains.Contains (domain))
                throw new System.Exception ("The domain you entered isn't an available domain");
			
			var dic = new System.Collections.Generic.Dictionary <string, string> ();
			var list = new System.Collections.Generic.List <string> (Constants.HeadersDics.Keys);
	        for (int i = 0; i < list.Count; i++)
	        	dic.Add (list [i], Constants.HeadersDics [list [i]]);
			dic.Add ("Referer", Constants.URL_CHANGE);
			dic.Add ("Content-Type", "application/x-www-form-urlencoded");
			
			using (var client = this.CreateHttpClient (dic)) {
				var csrf = client.CookieContainer.GetCookies (new System.Uri ("https://" + Constants.URL_DOMAIN)) ["csrf"];
				var data = string.Format ("csrf={0}&mail={1}&domain={2}", csrf.Value, login, "@" + domain);
				var res = client.UploadString (Constants.URL_CHANGE, data);
				if (client.StatusCode == System.Net.HttpStatusCode.OK) {
					this.User = login;
					this.Domain = domain;
					return true;
				}
			}
            return false;
		}
		/// <summary>
		/// temp.Delete ();
		/// </summary>
		/// <returns></returns>
		public bool Delete () {
			using (var client = this.CreateHttpClient (Constants.HeadersDics)) {
				string res = client.DownloadString (Constants.URL_DELETE);
				if (client.StatusCode == System.Net.HttpStatusCode.OK) {
					var obj = (System.Collections.Generic.Dictionary <string, object>) new System.Web.Script.Serialization.JavaScriptSerializer ().Deserialize <object> (res);
					string email = obj ["mail"].ToString ();
					this.User = email.Substring (0, email.IndexOf ('@'));
					this.Domain = email.Substring (email.IndexOf ('@') + 1);
					return true;
				}
			}
            return false;
		}
		
		public string User { get; private set; }
		public string Domain { get; private set; }
		public string Jid {
			get { return this.User + "@" + this.Domain; }
		}

        public System.Collections.Generic.List<string> AvailableDomains { get; private set; } = new System.Collections.Generic.List<string>();
	}
}