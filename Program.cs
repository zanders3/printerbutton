using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace PrinterButton
{
    class Program
    {
        static string[] headerFiles;
        static Random random = new Random();
        static Poem poem;

        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            poem = new Poem();
            
            DirectoryInfo info = new DirectoryInfo(".");
            headerFiles = Directory.GetFiles(".", "*.png").Where(file => !new FileInfo(file).Attributes.HasFlag(FileAttributes.Hidden)).ToArray();
            if (headerFiles.Length == 0)
            {
                MessageBox.Show("Nothing to print! There are no .png files in the " + Environment.CurrentDirectory + " folder");
                return;
            }

            PrintDialog pd = new PrintDialog();
            pd.Document = new PrintDocument();
            pd.Document.DefaultPageSettings.PaperSize = new PaperSize("Receipt", (int)poem.PageWidth, 520);
            pd.Document.PrintPage += Document_PrintPage;

            if (pd.ShowDialog() != DialogResult.OK)
                return;

            Application.Run(new PrinterButtonStatus(pd.Document, "COM3"));
        }

        static void Document_PrintPage(object sender, PrintPageEventArgs e)
        {
            string selectedFile = headerFiles[random.Next(headerFiles.Length)];
            Image loadedImage = new Bitmap(selectedFile);
            e.Graphics.DrawImage(loadedImage, 0f, 0f);
            if (!Path.GetFileName(selectedFile).StartsWith("footnote"))
                return;

            float headerHeight = poem.HeaderHeight;

            Font font = new Font(new FontFamily(poem.FontFamily), poem.FontSize);
            string poemText = poem.GeneratePoem(random, out selectedFile);
            e.Graphics.DrawString(poemText, font, Brushes.Black, new RectangleF(0f, headerHeight, poem.PageWidth, 400f), StringFormat.GenericTypographic);
            float poemHeight = e.Graphics.MeasureString(poemText, font, new SizeF(poem.PageWidth, 400f)).Height;

            if (selectedFile != null)
                loadedImage = new Bitmap(selectedFile);

            e.Graphics.DrawImage(loadedImage, 0f, headerHeight + poemHeight);
        }
    }
}
