using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MangaUnhost.Others {
    public static class ThreadTools {

        public static DateTime? ForceTimeoutAt = null;

        public static void Wait(int Milliseconds, bool DoEvents = false)
        {
            int Delay = 50;
            DateTime Begin = DateTime.Now;
            while ((DateTime.Now - Begin).TotalMilliseconds < Milliseconds)
            {
                Thread.Sleep(Delay);

                if (DoEvents && !Main.Instance.InvokeRequired)
                    Application.DoEvents();
            }

            if (ForceTimeoutAt != null && DateTime.Now > ForceTimeoutAt)
            {
                ForceTimeoutAt = null;
                throw new TimeoutException();
            }
        }
    }
}
