using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Threading;

namespace NN
{
    public class DrawingWindow : Form
    {
        PictureBox drawingArea;
        Bitmap drawingBitmap;
        Graphics drawingGraphics;
        bool mouse1 = false;

        public DrawingWindow()
        {
            this.Size = new Size(240, 320);
            this.Text = "Input";
            this.ShowIcon = false;
            this.FormBorderStyle = FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            drawingArea = new PictureBox();
            drawingArea.Size = new Size(224, 224);
            drawingArea.Location = new Point(0, 0);
            drawingArea.MouseDown += new MouseEventHandler(DrawArea_MouseDown);
            drawingArea.MouseUp += new MouseEventHandler(DrawArea_MouseUp);
            drawingArea.MouseMove += new MouseEventHandler(DrawArea_MouseMove);
            this.Controls.Add(drawingArea);

            Button buttonClear = new Button();
            buttonClear.Size = new Size(112, 55);
            buttonClear.Location = new Point(112, 224);
            buttonClear.Text = "Clear";
            buttonClear.FlatStyle = FlatStyle.Flat;
            buttonClear.Click += new EventHandler(ButtonClear_Click);
            this.Controls.Add(buttonClear);

            Button buttonAnalyze = new Button();
            buttonAnalyze.Size = new Size(112, 55);
            buttonAnalyze.Location = new Point(0, 224);
            buttonAnalyze.Text = "Analyze";
            buttonAnalyze.FlatStyle = FlatStyle.Flat;
            buttonAnalyze.Click += new EventHandler(ButtonAnalyze_Click);
            this.Controls.Add(buttonAnalyze);

            drawingBitmap = new Bitmap(224, 224);
            drawingGraphics = Graphics.FromImage(drawingBitmap);
            drawingArea.Image = drawingBitmap;
            drawingGraphics.FillRectangle(Brushes.Black, 0, 0, drawingArea.Width, drawingArea.Height); // fill black
        }

        public void OpenWindow()
        {
            Application.Run(this);
        }

        private void DrawArea_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                mouse1 = true;
        }

        private void DrawArea_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                mouse1 = false;
        }

        private void DrawArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouse1)
            {
                drawingGraphics.FillRectangle(Brushes.White, e.X, e.Y, 12, 12);
                drawingArea.Image = drawingBitmap;
            }
        }

        private void ButtonAnalyze_Click(object sender, EventArgs e)
        {
            Program.InputFromDrawingWindow();
        }

        private void ButtonClear_Click(object sender, EventArgs e)
        {
            drawingGraphics.FillRectangle(Brushes.Black, 0, 0, drawingArea.Width, drawingArea.Height); // fill black
            drawingArea.Image = drawingBitmap;
        }

        public Bitmap GetBitmap()
        {
            return drawingBitmap;
        }

        public Bitmap GetResizedBitmap()
        {
            Bitmap scaledBmp = new Bitmap(28, 28);
            using (Graphics g = Graphics.FromImage(scaledBmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(drawingBitmap, 0, 0, 28, 28);
            }
            return scaledBmp;
        }
    }
}
