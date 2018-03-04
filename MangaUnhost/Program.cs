using System;
using System.Windows.Forms;

namespace MangaUnhost {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Updater();
            Application.Run(new Main());
        }

        private static void Updater() {
            AppVeyor Updater = new AppVeyor("Marcussacana", "MangaUnhost", "MangaUnhost\\bin\\MangaUnhost.zip");
            string Result = Updater.FinishUpdate();
            if (Result != null) {
                System.Diagnostics.Process.Start(Result);
                Environment.Exit(0);
            }

            if (Updater.HaveUpdate()) {
                if (MessageBox.Show("Atualização Encontrada, Deseja Atualizar?", "MangaUnhost", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                    Updater.Update();
                }
            }
        }
    }
}
