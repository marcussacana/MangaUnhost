using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaUnhost.Others
{
    public static class AccountTools
    {
        public static Account[] LoadAccounts(string Type)
        {
            string Entries = Ini.GetConfig(Type, "Entries", Main.SettingsPath, false);
            if (Entries == string.Empty)
                return new Account[0];

            uint Count = uint.Parse(Entries);
            Account[] Accounts = new Account[Count];
            for (uint i = 0; i < Count; i++)
            {

                string Email = Ini.GetConfig($"{Type} Account.{i}", "Email", Main.SettingsPath, false);
                string Login = Ini.GetConfig($"{Type} Account.{i}", "Login;Username", Main.SettingsPath);
                string Pass = Ini.GetConfig($"{Type} Account.{i}", "Password", Main.SettingsPath);

                Accounts[i] = new Account()
                {
                    Login = Login,
                    Password = Pass,
                    Email = Email == string.Empty ? null : Email
                };
            }

            return Accounts;
        }

        public static void SaveAccount(string Type, Account Acc) => SaveAccount(Type, Acc.Login, Acc.Password, Acc.Email);
        public static void SaveAccount(string Type, string Login, string Password, string Email = null)
        {
            string Entries = Ini.GetConfig(Type, "Entries", Main.SettingsPath, false);

            int ID = 0;

            if (Entries == string.Empty)
            {
                Ini.SetConfig(Type, "Entries", "1", Main.SettingsPath);
            }
            else
            {
                ID = int.Parse(Entries);
                Ini.SetConfig(Type, "Entries", (ID + 1).ToString(), Main.SettingsPath);
            }

            Ini.SetConfig($"{Type} Account.{ID}", "Login", Login, Main.SettingsPath);
            Ini.SetConfig($"{Type} Account.{ID}", "Password", Password, Main.SettingsPath);

            if (!string.IsNullOrWhiteSpace(Email))
                Ini.SetConfig($"{Type} Account.{ID}", "Email", Email, Main.SettingsPath);
        }

        public static string GeneratePassword(int Length = 10, bool Special = false)
        {
            string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            if (Special)
                valid += "!@#$%&*()_=-";

            StringBuilder res = new StringBuilder();

            Random rnd = new Random();
            while (0 < Length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }

        public static string GetRandomComment()
        {
            string[] GenericComments = new string[] {
                "I'm loving this work, let's see if will keep the good history.",
                "I loved this chapter, let's see the next...",
                "It looks very, but very promising.",
                "Hmm, I think I have to read some more to know if I'll like it...",
                "I did not like the protagonist so much.",
                "I liked so much of the protagonist.",
                "I'm expecting good things of this work.",
                "So far without expectations yet.",
                "It started very well, let's see now...",
                "The Author started on the right foot."
             };

            return GenericComments[new Random().Next(0, GenericComments.Length)];
        }

        public static string GetRandomReply()
        {
            string[] GenericReplys = new string[] {
                "I agree.",
                "Indeed.",
                "Truth.",
                "I also think.",
                "I disagree.",
                "Do not.",
                "I think not.",
                "Maybe...",
                "Who knows."
            };

            return GenericReplys[new Random().Next(0, GenericReplys.Length)];
        }

        public static string PromptOption(string Question, string[] Options)
        {
            if (Options.Length == 1)
                return Options.Single();

            var Form = new System.Windows.Forms.Form
            {
                Size = new System.Drawing.Size(270, 120)
            };

            VSContainer ThemeContainer = new VSContainer()
            {
                Form = Form,
                FormOrWhole = VSContainer.__FormOrWhole.Form,
                AllowMaximize = false,
                AllowMinimize = false,
                Text = Question
            };
            Form.Controls.Add(ThemeContainer);


            VSComboBox ComboBox = new VSComboBox()
            {
                Size = new System.Drawing.Size(235, 30),
                Location = new System.Drawing.Point(10, 40)
            };

            foreach (string Language in Options)
                ComboBox.Items.Add(Language);

            ComboBox.SelectedIndex = 0;

            ThemeContainer.Controls.Add(ComboBox);

            Form.ShowDialog(Main.Instance);

            return ComboBox.SelectedItem.ToString();
        }



        public static int PromptValue(string Question, int Min = 0, int Max = int.MaxValue, int Default = 0)
        {
            var Form = new System.Windows.Forms.Form
            {
                Size = new System.Drawing.Size(270, 120)
            };

            VSContainer ThemeContainer = new VSContainer()
            {
                Form = Form,
                FormOrWhole = VSContainer.__FormOrWhole.Form,
                AllowMaximize = false,
                AllowMinimize = false,
                Text = Question
            };
            Form.Controls.Add(ThemeContainer);


            VSNormalTextBox TextBox = new VSNormalTextBox()
            {
                Size = new System.Drawing.Size(235, 30),
                Location = new System.Drawing.Point(10, 40)
            };

            TextBox.Text = $"{Default}";

            ThemeContainer.Controls.Add(TextBox);

            int Value = 0;

            do
            {
                Form.ShowDialog(Main.Instance);
            } while (!int.TryParse(TextBox.Text, out Value) || (Value < Min) || (Value > Max));

            return Value;
        }

        public static List<GuerrillaMail.Email> WaitEmail(this GuerrillaMail Email, Func<GuerrillaMail.Email, bool> Verify = null, int WaitMin = 10)
        {
            var Mails = new List<GuerrillaMail.Email>();

            DateTime Limit = DateTime.Now.AddMinutes(WaitMin);
            while (Mails.Count == 0)
            {

                ThreadTools.Wait(10000, true);
                if (DateTime.Now > Limit)
                    throw new Exception("Timeout");

                if (Verify == null)
                    Mails = Email.GetAllEmails();
                else
                    Mails = Email.GetAllEmails().Where(Verify).ToList();
            }

            return Mails;
        }

        public static void ClearEmail(this GuerrillaMail Email, bool Wait = false)
        {
            Email.DeleteEmails((from x in Wait ? Email.WaitEmail() : Email.GetAllEmails() select x.mail_id.ToString()));
        }
    }
}
