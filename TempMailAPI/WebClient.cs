using System;
using System.Linq;
namespace TempMailAPI {
	public class WebClient : System.Net.WebClient {
		public System.Net.CookieContainer CookieContainer { get; set; }
		public System.Net.HttpStatusCode StatusCode { get; set; }
		public bool AllowAutoRedirect { get; set; }
		public bool KeepAlive { get; set; }

		public WebClient () : this (new System.Net.CookieContainer ()) { }
		public WebClient (System.Net.CookieContainer cookieContainer, bool allowAutoRedirect = true, bool KeepAlive = true) {
			this.CookieContainer = cookieContainer;
			this.AllowAutoRedirect = allowAutoRedirect;
			this.KeepAlive = KeepAlive;
		}

		protected override System.Net.WebRequest GetWebRequest (Uri address) {
			var request = (System.Net.HttpWebRequest) base.GetWebRequest (address);
			request.CookieContainer = this.CookieContainer;
			request.KeepAlive = this.KeepAlive;
			request.AllowAutoRedirect = this.AllowAutoRedirect;
			return request;
		}

		protected override System.Net.WebResponse GetWebResponse (System.Net.WebRequest request) {
			System.Net.WebResponse response = null;
			try { response = request.GetResponse (); }
			catch (System.Net.WebException wb) { response = ((System.Net.HttpWebResponse) wb.Response); }

			this.StatusCode = ((System.Net.HttpWebResponse) response).StatusCode;

			if (this.CookieContainer == null)
				this.CookieContainer = ExtractCookies (response, new System.Collections.Generic.List <string> () { "expires", "path", "domain", "max-age" });

			return response;
		}

		private System.Net.CookieContainer ExtractCookies (System.Net.WebResponse response,
		                                                   System.Collections.Generic.List <string> list) {
			var cookieContainer = new System.Net.CookieContainer ();
			// List<string> list = new List<string>() { "expires", "path", "domain", "max-age" };

			var setCookie = response.Headers ["Set-Cookie"].Split (';').ToList ();

			for (int i = 0; i < setCookie.Count; i ++) {
				setCookie [i] = setCookie [i].Trim ().ToLower ().Replace ("httponly,", string.Empty);
				var x = setCookie [i].Count (c => c == ',') > 1;
				if ((!setCookie[i].Contains ("expires") && setCookie [i].Contains (",")) ||
				    (setCookie[i].Contains ("expires") && setCookie [i].Count (c => c == ',') > 1)) {
					var temp = setCookie [i].Split (',');
					setCookie [i] = temp [0];
					if (!temp [0].Contains ("expires"))
						setCookie.Insert (i + 1, temp [1]);
					else
						setCookie.Insert (i + 1, temp [2]);
				}
			}

			System.Net.Cookie tempCookie = new System.Net.Cookie();
			for (int i = 0; i < setCookie.Count; i++) {
				if (!setCookie[i].Contains("=")) continue;

				var temp = setCookie[i].Split('=');
				if (list.TrueForAll(k => !temp[0].Contains(k))) {
					if (tempCookie.Name != string.Empty)
						cookieContainer.Add(tempCookie);

					tempCookie = new System.Net.Cookie ();
					tempCookie.Name = temp[0];
					tempCookie.Value = temp[1];
				}

				if (temp[0] == "path") tempCookie.Path = temp[1];
				else if (temp[0] == "domain")
					tempCookie.Domain = (temp[1].First() == '.') ? temp[1].Substring(1) : temp[1];

				if (i == setCookie.Count - 1 && tempCookie.Name != string.Empty)
					cookieContainer.Add (tempCookie);
			}
			return cookieContainer;
		}
	}
}