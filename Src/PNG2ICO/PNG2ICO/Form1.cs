using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Management;
using System.Security.Cryptography;
using System.Threading;
using System.Globalization;
namespace PNG2ICO
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(e.KeyData);
        }
        private void OnKeyDown(Keys keyData)
        {
            if (keyData == Keys.F1)
            {
                const string message = "• Author: Michaël André Franiatte.\n\r\n\r• Contact: michael.franiatte@gmail.com.\n\r\n\r• Publisher: https://github.com/michaelandrefraniatte.\n\r\n\r• Copyrights: All rights reserved, no permissions granted.\n\r\n\r• License: Not open source, not free of charge to use.";
                const string caption = "About";
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            if (keyData == Keys.Escape)
            {
                this.Close();
            }
        }
        public static Form1 form = (Form1)Application.OpenForms["Form1"];
        private static Color colortransparency = Color.Transparent;
        private static string filename; 
        private int x, y;
        private Bitmap bitmap, pngbitmap;
        private Image img;
        private void Form1_Shown(object sender, EventArgs e)
        {
            this.pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            this.pictureBox3.SizeMode = PictureBoxSizeMode.StretchImage;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Thread newThread = new Thread(new ThreadStart(showOpenFileDialog));
            newThread.SetApartmentState(ApartmentState.STA);
            newThread.Start();
        }
        public void showOpenFileDialog()
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Filter = "All Files(*.*)|*.*";
            if (op.ShowDialog() == DialogResult.OK)
            {
                filename = op.FileName;
                pictureBox1.Image = new Bitmap(filename);
                img = Bitmap.FromFile(filename);
                this.Text = filename;
            }
        }
        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            Bitmap bitmap = (Bitmap)pictureBox1.Image;
            x = e.X * bitmap.Width / pictureBox1.ClientSize.Width;
            y = e.Y * bitmap.Height / pictureBox1.ClientSize.Height;
            colortransparency = bitmap.GetPixel(x, y);
            this.pictureBox2.BackgroundImage = null;
            this.pictureBox2.BackColor = colortransparency;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            bitmap = new Bitmap(img);
            if (x != 0 | y != 0)
            {
                bitmap = Transparentify(bitmap, x, y, 1, 1, 1);
                bitmap = ExpandTransparency(bitmap, 2.0f);
                bitmap.MakeTransparent(colortransparency);
            }
            pngbitmap = bitmap;
            img = (Bitmap)bitmap;
            bitmap = new Bitmap(img, new Size(160, 160));
            pictureBox3.Image = bitmap;
        }
        private Bitmap Transparentify(Bitmap bm_input, int x, int y, int dr, int dg, int db)
        {
            Color target_color = bm_input.GetPixel(x, y);
            byte r = target_color.R;
            byte g = target_color.G;
            byte b = target_color.B;
            Bitmap bm = new Bitmap(bm_input);
            Stack<Point> points = new Stack<Point>();
            int width = bm_input.Width;
            int height = bm_input.Height;
            bool[,] added_to_stack = new bool[width, height];
            points.Push(new Point(x, y));
            added_to_stack[x, y] = true;
            bm.SetPixel(x, y, Color.Transparent);
            while (points.Count > 0)
            {
                Point point = points.Pop();
                for (int i = point.X - 1; i <= point.X + 1; i++)
                {
                    for (int j = point.Y - 1; j <= point.Y + 1; j++)
                    {
                        if ((i < 0) || (i >= width) ||
                            (j < 0) || (j >= height)) continue;
                        if (added_to_stack[i, j]) continue;
                        Color color = bm_input.GetPixel(i, j);
                        if (Math.Abs(r - color.R) > dr) continue;
                        if (Math.Abs(g - color.G) > dg) continue;
                        if (Math.Abs(b - color.B) > db) continue;
                        points.Push(new Point(i, j));
                        added_to_stack[i, j] = true;
                        bm.SetPixel(i, j, Color.Transparent);
                    }
                }
            }
            return bm;
        }
        private Bitmap ExpandTransparency(Bitmap input_bm, float max_dist)
        {
            Bitmap result_bm = new Bitmap(input_bm);
            float[,] distances = GetDistancesToTransparent(input_bm, max_dist);
            int width = input_bm.Width;
            int height = input_bm.Height;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (input_bm.GetPixel(x, y).A == 0)
                        continue;
                    float distance = distances[x, y];
                    if (distance > max_dist) continue;
                    float scale = distance / max_dist;
                    Color color = input_bm.GetPixel(x, y);
                    int r = color.R;
                    int g = color.G;
                    int b = color.B;
                    int a = (int)(255 * scale);
                    color = Color.FromArgb(a, r, g, b);
                    result_bm.SetPixel(x, y, color);
                }
            }
            return result_bm;
        }
        private float[,] GetDistancesToTransparent(Bitmap bm, float max_dist)
        {
            int width = bm.Width;
            int height = bm.Height;
            float[,] distances = new float[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    distances[x, y] = float.PositiveInfinity;
            int dxmax = (int)max_dist;
            if (dxmax < max_dist) dxmax++;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (bm.GetPixel(x, y).A == 0)
                    {
                        for (int dx = -dxmax; dx <= dxmax; dx++)
                        {
                            int px = x + dx;
                            if ((px < 0) || (px >= width)) continue;
                            for (int dy = -dxmax; dy <= dxmax; dy++)
                            {
                                int py = y + dy;
                                if ((py < 0) || (py >= height)) continue;
                                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                                if (distances[px, py] > dist)
                                    distances[px, py] = dist;
                            }
                        }
                    }
                }
            }
            return distances;
        }
        private void button3_Click(object sender, EventArgs e)
        {
            SaveAsIcon(bitmap, System.IO.Path.GetFileNameWithoutExtension(filename) + ".ico");
            SaveAsIconPng(pngbitmap, System.IO.Path.GetFileNameWithoutExtension(filename) + ".ico.png");
        }
        private void SaveAsIcon(Bitmap SourceBitmap, string FilePath)
        {
            FileStream FS = new FileStream(FilePath, FileMode.Create);
            FS.WriteByte(0); 
            FS.WriteByte(0);
            FS.WriteByte(1); 
            FS.WriteByte(0);
            FS.WriteByte(1); 
            FS.WriteByte(0);
            FS.WriteByte((byte)SourceBitmap.Width);
            FS.WriteByte((byte)SourceBitmap.Height);
            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(0); 
            FS.WriteByte(0);
            FS.WriteByte(32); 
            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(22);
            FS.WriteByte(0);
            FS.WriteByte(0);
            FS.WriteByte(0);
            SourceBitmap.Save(FS, ImageFormat.Png);
            long Len = FS.Length - 22;
            FS.Seek(14, SeekOrigin.Begin);
            FS.WriteByte((byte)Len);
            FS.WriteByte((byte)(Len >> 8));
            FS.Close();
        }
        private void SaveAsIconPng(Bitmap SourceBitmap, string FilePath)
        {
            SourceBitmap.Save(FilePath, ImageFormat.Png);
        }
        private void pictureBox1_MouseEnter(object sender, EventArgs e)
        {
            pictureBox1.Cursor = Cursors.Cross;
        }
        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            pictureBox1.Cursor = Cursors.Default;
        }
        private void pictureBox2_MouseEnter(object sender, EventArgs e)
        {
            pictureBox2.Cursor = Cursors.Hand;
        }
        private void pictureBox2_MouseLeave(object sender, EventArgs e)
        {
            pictureBox2.Cursor = Cursors.Default;
        }
        private void pictureBox2_Click(object sender, EventArgs e)
        {
            x = 0;
            y = 0;
            pictureBox2.BackgroundImage = new Bitmap("bckg.png");
            colortransparency = Color.Transparent;
        }
    }
}
