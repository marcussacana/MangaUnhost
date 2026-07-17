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
                    Extensions.SafeDoEvents();
            }

            if (ForceTimeoutAt != null && DateTime.Now > ForceTimeoutAt)
            {
                ForceTimeoutAt = null;
                throw new TimeoutException();
            }
        }

        public static T RunInBackground<T>(this Task<T> Task)
        {
            while (!Task.IsCanceled && !Task.IsCompleted && !Task.IsFaulted)
                Wait(100, true);

            if (Task.IsFaulted)
                throw Task.Exception;

            if (Task.IsCanceled)
                return default;

            return Task.Result;
        }
        public static void RunInBackground(this Task Task)
        {
            while (!Task.IsCanceled && !Task.IsCompleted && !Task.IsFaulted)
                Wait(100, true);

            if (Task.IsFaulted)
                throw Task.Exception;
        }
    }
}
