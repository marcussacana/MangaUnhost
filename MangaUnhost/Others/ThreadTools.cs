using Nito.AsyncEx;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MangaUnhost.Others {
    public static class ThreadTools {

        public static void Wait(int Milliseconds, bool DoEvents = false) {
            try
            {
                int Delay = 50;
                DateTime Begin = DateTime.Now;
                while ((DateTime.Now - Begin).TotalMilliseconds < Milliseconds)
                {
                    AsyncContext.Run(() => Task.Delay(Delay));

                    if (DoEvents && !Main.Instance.InvokeRequired)
                        Application.DoEvents();
                }
            }
            catch {

                //WTF Wine
                try
                {
                    if (!DoEvents)
                        System.Threading.Thread.Sleep(Milliseconds);
                }
                catch { }

            }
        }
    }
}
