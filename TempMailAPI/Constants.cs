namespace TempMailAPI {
	/// <summary>
	/// Description of Constants.
	/// </summary>
	public static class Constants {
		public static string URL_DOMAIN = "temp-mail.org";
		public static string URL_BASE = "https://" + URL_DOMAIN + "/en";
		public static string URL_CHANGE = URL_BASE + "/option/change/";
		public static string URL_CHECK = URL_BASE + "/option/check/";
		public static string URL_DELETE = URL_BASE + "/option/delete/";
		public static string URL_DOWNLOAD = URL_BASE + "/download/";
		public static string URL_REFRESH = URL_BASE + "/option/refresh/";
		public static string URL_VIEW = URL_BASE + "/view/";
		public static string URL_SOURCE = URL_BASE + "/source/";
		public static string URL_DELETE_MAIL = URL_BASE + "/delete/";
		
		public static string UserAgent = "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36";
		
		public static System.Collections.Generic.Dictionary <string, string> HeadersDics = new System.Collections.Generic.Dictionary <string, string> () {
				{ "Accept", "*/*" },
				{ "Accept-Language", "en-US,en" },
				{ "User-Agent", Constants.UserAgent },
				{ "Host", Constants.URL_DOMAIN },
				{ "Origin", "https://" + Constants.URL_DOMAIN },
				{ "X-Requested-With", "XMLHttpRequest" },
				{ "Upgrade-Insecure-Requests", "1" }
		};
		
		internal static System.Collections.Generic.Dictionary <string, System.Text.RegularExpressions.Regex> RegexObjects = new System.Collections.Generic.Dictionary <string, System.Text.RegularExpressions.Regex> () {
			{ "Lines", new System.Text.RegularExpressions.Regex ("(\n|\r|\r\n)") },
            { "SpacesBetweenTags", new System.Text.RegularExpressions.Regex (@">\s*<") },
            { "Domains", new System.Text.RegularExpressions.Regex ("<select name=\"domain\" class=\"form-control\" id=\"domain\">(<option value=\"(?<domain>.*?)\">.*?</option>)+</select>") },
            { "MailsIds", new System.Text.RegularExpressions.Regex (Constants.URL_VIEW + "(?<id>.*?)\"") },
            { "Mail", new System.Text.RegularExpressions.Regex ("class=\"mail opentip\" value=\"(?<email>.*?)\"") }
        };
	}
}