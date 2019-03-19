using System;
using System.Collections.Generic;

namespace TempMailAPI.Models {
	/// <summary>
	/// Description of Mail.
	/// </summary>
	public class Mail {
		public Mail () { }
		public Mail (string from, string subject, string content, string to, string date) {
        	this.Content = content;
        	this.Date = date;
        	this.From = from;
        	this.Subject = subject;
        	this.To = to;
        }
        internal Mail (TempMailAPI.TempMail tempMail, string id = "", string from = "",
		             string subject = "", string content = "", string to = "", string date = "") {
        	this.Content = content;
        	this.Date = date;
        	this.From = from;
        	this.ID = id;
        	this.Subject = subject;
        	this.tempMail = tempMail;
        	this.To = to;
        }
		private void NullAll () {
			this.Content = string.Empty;
        	this.Date = string.Empty;
        	this.From = string.Empty;
        	this.ID = string.Empty;
        	this.Subject = string.Empty;
        	this.tempMail = null;
        	this.To = string.Empty;
		}
        public string Delete () {
			if (hasEmail) {
				var source = this.tempMail.GET (Constants.URL_DELETE_MAIL + this.ID);
				this.NullAll ();
				return source;
			}
			return string.Empty;
        }
		private bool hasEmail { get { return this.tempMail != null && !string.IsNullOrEmpty (this.ID); } }
		// Isso é o email ou o source?!
        public string Download () {
			if (this.hasEmail) {
				var source = this.tempMail.GET ("https://temp-mail.org/en/download/" + this.ID);
				System.IO.File.WriteAllText (@"C:\API\EmailDown.txt", source);
				return source;
			}
			return string.Empty;
		}
		// Usar: https://emkei.cz/ para testar os valores que vem no e-mail
		public MailSource GetSource () {
			if (hasEmail) {
				var source = this.tempMail.GET (Constants.URL_SOURCE + this.ID);
				// System.IO.File.WriteAllText (@"C:\API\EmailSource.txt", source);
			}
			return new MailSource ();
		}
		public override string ToString () {
			return string.Format ("[Mail From={0}, Subject={1}, Content={2}, To={3}, Date={4}]", From, Subject, Content, To, Date);
		}
        
        private TempMailAPI.TempMail tempMail;
        public string ID { get; set; }
		public string From { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public string To { get; set; }
        public string Date { get; set; }
	}
}