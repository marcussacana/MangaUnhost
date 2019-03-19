using System;

namespace TempMailAPI.Models {
	/// <summary>
	/// Description of MailSource.
	/// </summary>
	public class MailSource {
		public string Received { get; set; }
		public string To { get; set; }
		public string Subject { get; set; }
		public string From { get; set; }
		public Enums.XPriority XPriority { get; set; }
		public string Importance { get; set; }
		public string ErrorTo { get; set; }
		public string ReplyTo { get; set; }
		public Enums.ContentType ContentType { get; set; }
		public Enums.Charset Charset { get; set; }
		public string MessageId { get; set; }
		public string Date { get; set; }
		public string Body { get; set; }
		
		public MailSource () { }
		public override string ToString() {
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder ();
			
			stringBuilder.AppendLine ("Received: " + this.Received);
			stringBuilder.AppendLine ("To: " + this.To);
			stringBuilder.AppendLine ("Subject: " + this.Subject);
			stringBuilder.AppendLine ("From: " + this.From);
			stringBuilder.AppendLine ("X-Priority: " + this.XPriority.GetDescription ()); // X-Priority: 3 (Normal)
			stringBuilder.AppendLine ("Importance: " + this.Importance); // Importance: Normal
			stringBuilder.AppendLine ("Errors-To: " + this.ErrorTo);
			stringBuilder.AppendLine ("Reply-To: " + this.ReplyTo);
			stringBuilder.AppendLine ("Content-Type: " + this.ContentType.GetDescription () + "; charset=" + this.Charset.GetDescription ());
			stringBuilder.AppendLine ("Message-Id: <" + this.MessageId + ">");
			stringBuilder.AppendLine ("Date: " + this.Date);
			stringBuilder.AppendLine ("\r\n" + this.Body);
			
			return stringBuilder.ToString ();
		}

	}
}