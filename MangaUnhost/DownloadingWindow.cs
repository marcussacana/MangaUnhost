﻿using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MangaUnhost
{
    public partial class DownloadingWindow : Form
    {
        long ContentLenght = 0;
        long Downloaded = 0;

        bool Finished;

        string URL;
        string SaveAs;
        public DownloadingWindow(string URL, string SaveAs)
        {
            InitializeComponent();

            this.URL = URL;
            this.SaveAs = SaveAs;

            Shown += (sender, args) =>
            {
                new Thread(Download).Start();
            };
        }

        private void ProgressUpdateTick(object sender, EventArgs e)
        {
            ProgressBar.StartingAngle += 7;
            if (ProgressBar.StartingAngle > 359)
                ProgressBar.StartingAngle = 0;

            if (ContentLenght > 0)
            {
                try
                {
                    ProgressBar.ShowText = true;
                    ProgressBar.Value = (int)((Downloaded / (decimal)ContentLenght) * 100);
                }
                catch { }
            }
            else
            {
                ProgressBar.ShowText = false;
                ProgressBar.Value = 100;
            }
            if (Finished)
                Close();
        }

        public void Download()
        {
            try
            {
                DoDownload(URL, SaveAs);
            }
            catch {
                try
                {
                    int part = 0;
                    while (true)
                    {
                        try
                        {
                            DoDownload(URL.Replace(".zip", $".zip.{part:D3}"), SaveAs + $".{part}");
                            part++;
                        }
                        catch
                        {
                            break;
                        }
                    }


                    using var Output = File.Create(SaveAs);
                    for (int i = 0; i < part; i++)
                    {
                        using (var Fragment = File.OpenRead(SaveAs + $".{i}"))
                        {
                            Fragment.CopyTo(Output);
                        }
                        File.Delete(SaveAs + $".{i}");
                    }

                    Finished = true;
                    return;
                }
                catch { }
                throw;
            }

            Finished = true;
        }

        private void DoDownload(string URL, string SaveAs)
        {
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(URL);

            Request.UseDefaultCredentials = true;
            Request.Method = "GET";
            Request.Timeout = 1000 * 30;

            using (var Response = Request.GetResponse())
            using (var RespData = Response.GetResponseStream())
            using (var Output = File.Create(SaveAs))
            {
                ContentLenght = Response.ContentLength;
                Downloaded = 0;

                int Readed = 0;
                do
                {
                    byte[] Buffer = new byte[1024 * 4];
                    Readed = RespData.Read(Buffer, 0, Buffer.Length);
                    Output.Write(Buffer, 0, Readed);
                    Downloaded += Readed;
                } while (Readed != 0);
            }
        }
    }
}
