using System;

namespace TempMailAPI {
	/// <summary>
	/// Description of HttpClientcs.
	/// </summary>
	public class HttpClient {
		protected internal System.Net.CookieContainer cookies;
		
		public HttpClient () {
			
		}
		
		protected internal WebClient CreateHttpClient () { return this.CreateHttpClient (Constants.HeadersDics); }
		protected internal WebClient CreateHttpClient (System.Collections.Generic.Dictionary <string, string> headers) {
			using (WebClient client = new WebClient ()) {
				if (headers.Count > 0) {
					var headersList = new System.Collections.Generic.List <string> (headers.Keys);
					for (int i = 0; i < headersList.Count; i ++)
						client.Headers.Add (headersList [i], headers [headersList [i]]);
				}
				client.CookieContainer = this.cookies;
				return client;
			}
        }
	}
}