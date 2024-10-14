using AForge.Math.Metrics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static MangaUnhost.Others.DataTools;

namespace MangaUnhost
{
    public partial class TestDetection : Form
    {
        public TestDetection()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var fd = new OpenFileDialog();
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            imgLeft.Image?.Dispose();
            imgLeft.Image = Image.FromFile(fd.FileName);
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            var fd = new OpenFileDialog();
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            imgRight.Image?.Dispose();
            imgRight.Image = Image.FromFile(fd.FileName);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var Rst = imgLeft.Image.OpenCV_AreImagesSimilar(imgRight.Image, double.Parse(tbSimilarity.Text.Replace(".", ",")));

            MessageBox.Show(Rst ? "Iguais!" : "Diferentes!");
        }

		private void btnAFTest_Click(object sender, EventArgs e)
		{
            try
            {
                var Similarity = double.Parse(tbSimilarity.Text.Replace(".", ","));
                try
                {
                    var Rst = imgLeft.Image.AForge_AreImagesSimilar(imgRight.Image, 1 - Similarity);

                    MessageBox.Show(Rst ? "FAIL\nNo translation detected in the page" : "PASS\nThe translation has been detected", "Test Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
                catch
                {

                }
            }
            catch
            {
                MessageBox.Show("Invalid Similarity Value", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

		}

		private void btnAFSetCurrentMode_Click(object sender, EventArgs e)
		{
            try
            {
				var sensitivity = double.Parse(tbSimilarity.Text.Replace(".", ","));
				Main.Instance.SetTextDectionSettings(true, sensitivity);
				Close();
			}
            catch
			{
				MessageBox.Show("Invalid Similarity Value", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void btnOCTest_Click(object sender, EventArgs e)
		{
			try
			{
				var Similarity = double.Parse(tbOCSimilarity.Text.Replace(".", ","));
				try
				{
					var Rst = imgLeft.Image.OpenCV_AreImagesSimilarCustom(imgRight.Image, Similarity);
					MessageBox.Show(Rst ? "FAIL\nNo translation detected in the page" : "PASS\nThe translation has been detected", "Test Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				catch
				{

				}
			}
			catch
			{
				MessageBox.Show("Invalid Similarity Value", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

		}

		private void vsButton2_Click(object sender, EventArgs e)
		{

		}

		private void btnOCSetCurrentMode_Click(object sender, EventArgs e)
		{
            try
            {
				var sensitivity = double.Parse(tbOCSimilarity.Text.Replace(".", ","));
		        Main.Instance.SetTextDectionSettings(false, sensitivity);
                Close();
			}
            catch
			{
				MessageBox.Show("Invalid Similarity Value", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void btnHelp_Click(object sender, EventArgs e)
		{
            MessageBox.Show("This settings will be used for manga translation, this change the detection sensibility of texts.\nIf the translation is keeping pages without translation or taking forever means you should need change test new settings here.\n\nTo test a setting you need to select two versions of the same chapter page but with different languages (can be any language, it will be used just for tunning).\n\nAfer that you should change the 'Sensitivity' value and click in 'Test Settings' until you see a PASS messsage.\n\nThen check if there are no false-positive detection by selecting the same image in both sides but with different resolution or quality, then click at 'Test Settings' again without change anything, and you MUST see a Fail message this time.", "Text Detection Tunning Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
