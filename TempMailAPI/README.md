# temp-mail-API
> Temp mail is a service which lets you use anonymous emails for free!

**temp-mail-api** is C#-based wrapper for [TempMail](https://temp-mail.org) (Version 0.1 based on [temp-mail-API](https://github.com/RyuzakiH/temp-mail-API))

# Usage
```CSharp
var tempMail = new TempMail ();

// To get Mailbox
var mails = temp.GetEmailsReceived ();
for (int i 0; i < mails.Count; i ++) {
	mails [i].Delete ();
}

// To delete current E-mail
temp.Delete ();

// To generate a new E-mail
temp.Change (user, domain);
```