using Nito.AsyncEx;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MangaUnhost.Others {
    public static class ThreadTools {

        public static void Wait(int Milliseconds, bool DoEvents = false) {
            int Delay = DoEvents ? 10 : 1;
            DateTime Begin = DateTime.Now;
            while ((DateTime.Now - Begin).TotalMilliseconds < Milliseconds) {
                AsyncContext.Run(() => Task.Delay(Delay));

                if (DoEvents && !Main.Instance.InvokeRequired)
                    Application.DoEvents();
            }
        }
    }
}
