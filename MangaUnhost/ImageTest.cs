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
    public partial class ImageTest : Form
    {
        public ImageTest()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var Rst = pictureBox1.Image.AreImagesSimilar(pictureBox2.Image, double.Parse(textBox1.Text.Replace(".", ",")), float.Parse(textBox2.Text.Replace(".", ",")));

            MessageBox.Show(Rst ? "Iguais!" : "Diferentes!");
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var fd = new OpenFileDialog();
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            pictureBox1.Image?.Dispose();
            pictureBox1.Image = Image.FromFile(fd.FileName);
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            var fd = new OpenFileDialog();
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            pictureBox2.Image?.Dispose();
            pictureBox2.Image = Image.FromFile(fd.FileName);
        }
    }
}
