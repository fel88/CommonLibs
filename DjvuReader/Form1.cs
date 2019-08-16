using DjvuNet.DataChunks.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjvuReader
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            tableLayoutPanel1.ColumnStyles[1].Width = 0;
            bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height); ;
            gr = Graphics.FromImage(bmp);
            SizeChanged += Form1_SizeChanged;
            pictureBox1.MouseDown += Form1_MouseDown;
            pictureBox1.MouseUp += Form1_MouseUp;
            MouseWheel += Form1_MouseWheel;
            pictureBox1.MouseMove += PictureBox1_MouseMove;
        }

        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            pictureBox1.Focus();
        }

        private void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
            var pos = pictureBox1.PointToClient(Cursor.Position);
            float zold = zoom;
            if (e.Delta > 0)
            {
                zoom *= 1.2f;
            }
            else
            {
                zoom /= 1.2f;
            }

            if (zoom < 0.08)
            {
                zoom = 0.08f;
            }

            if (zoom > 100)
            {
                zoom = 100f;
            }


            float zz = zoom / zold;
            sx = zz * (pos.X + sx) - pos.X;
            sy = zz * (pos.Y + sy) - pos.Y;

        }

        int startx;
        int starty;
        float startsx;
        float startsy;
        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            drag = false;
        }

        bool drag = false;
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            var pos = pictureBox1.PointToClient(Cursor.Position);
            startx = pos.X;
            starty = pos.Y;
            startsx = sx;
            startsy = sy;
            drag = true;
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height); ;
            gr = Graphics.FromImage(bmp);
        }

        Graphics gr;
        Bitmap bmp;
        DjvuNet.DjvuDocument doc;

        public Bitmap Resize(Bitmap bb, Size target)
        {
            Bitmap ret = new Bitmap(target.Width, target.Height);
            var gr = Graphics.FromImage(ret);
            gr.DrawImage(bb, new Rectangle(0, 0, target.Width, target.Height), new Rectangle(0, 0, bb.Width, bb.Height), GraphicsUnit.Pixel);
            return ret;
        }
        public Bitmap Resize(Bitmap bb, float koef)
        {
            var wn = (int)(bb.Width * koef);
            var hn = (int)(bb.Height * koef);
            return Resize(bb, new Size(wn, hn));
        }
        private void ToolStripButton1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "djvu|*.djvu";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Cache.Clear();
                pageIndex = 0;
                sy = 0;
                doc = new DjvuNet.DjvuDocument(ofd.FileName);
                page1 = doc.Pages[0].BuildImage();

                page1 = Resize(page1, koef);
                bound1 = new Rectangle(0, 0, page1.Width, page1.Height);
                page2 = doc.Pages[1].BuildImage();
                page2 = Resize(page2, koef);
                bound2 = new Rectangle(0, 0, page1.Width, page1.Height);
                sizes = new SizeF[doc.Pages.Count()];
                for (int i = 0; i < doc.Pages.Count(); i++)
                {
                    sizes[i] = new SizeF(doc.Pages[i].Width, doc.Pages[i].Height);
                }
            }

        }

        SizeF[] sizes;
        float sx;
        float sy;
        float zoom = 0.1f;
        Bitmap page1;
        Bitmap page2;
        Rectangle bound1;
        Rectangle bound2;
        int vgap = 20;
        int pageIndex = 0;
        int pageIndex2 = 1;
        float koef = 0.5f;
        public class PageCache
        {
            public int Index;
            public Bitmap Page;
        }

        public List<PageCache> Cache = new List<PageCache>();
        public void AddToCache(PageCache cc)
        {
            Cache.Add(cc);
            while (Cache.Count > 5)
            {
                Cache.RemoveAt(0);
            }
        }
        void ReloadPages(int ind)
        {
            Thread th = new Thread(() =>
            {
                pageIndex = ind;
                if (Cache.Any(z => z.Index == ind))
                {
                    lock (obj1)
                    {
                        page1 = Cache.First(z => z.Index == ind).Page;
                    }
                }
                else
                {
                    var temp1 = doc.Pages[ind].BuildImage();
                    lock (obj1)
                    {
                        page1 = Resize(temp1, koef);
                        AddToCache(new PageCache() { Index = ind, Page = page1 });
                        bound1 = new Rectangle(0, 0, page1.Width, page1.Height);
                    }
                    temp1.Dispose();
                }
                if (Cache.Any(z => z.Index == ind + 1))
                {
                    lock (obj1)
                    {
                        page2 = Cache.First(z => z.Index == ind + 1).Page;
                    }
                }
                else
                {
                    if (doc.Pages.Count() < ind)
                    {
                        lock (obj1)
                        {
                            page2 = null;
                        }
                    }
                    else
                    {
                        var temp2 = doc.Pages[ind + 1].BuildImage();
                        lock (obj1)
                        {

                            page2 = Resize(temp2, koef);
                            AddToCache(new PageCache() { Index = ind + 1, Page = page2 });
                            bound2 = new Rectangle(0, 0, page2.Width, page2.Height);
                        }
                        temp2.Dispose();
                    }
                }

            });
            th.IsBackground = true;
            th.Start();
        }
        public object obj1 = new object();
        private void Timer1_Tick(object sender, EventArgs e)
        {
            var pos = pictureBox1.PointToClient(Cursor.Position);
            if (drag)
            {
                sy = startsy + (starty - pos.Y);
                sx = startsx + (startx - pos.X);
            }
            var b11 = GetBack(new PointF(0, 0));
            var b22 = GetBack(new PointF(pictureBox1.Width, 0));
            var ww = b22.X - b11.X;
            if (ww > bound1.Width)
            {
                sx = -(pictureBox1.Width / 2 - (bound1.Width * zoom / 2));
            }
            gr.Clear(Color.White);
            
            gr.ResetTransform();
            gr.TranslateTransform(-sx, -sy);
            gr.ScaleTransform(zoom, zoom);
            if (sizes != null)
            {
                label1.Text = pageIndex + " / " + doc.Pages.Count();
                var b1 = GetBack(new PointF(0, 0));
                float accum = 0;
                for (int i = 0; i < sizes.Length; i++)
                {
                    accum += (sizes[i].Height * koef) + vgap;
                    if (accum > b1.Y)
                    {
                        if (pageIndex != i)
                        {
                            ReloadPages(i);
                        }
                        break;
                    }

                }
            }
            if (page1 != null)
            {
                float shift1 = 0;
                for (int i = 0; i < pageIndex; i++)
                {
                    shift1 += (sizes[i].Height * koef) + vgap;
                }
                var state = gr.Save();
                gr.TranslateTransform(0, shift1);

                lock (obj1)
                {

                    var rect = new Rectangle(0, 0, doc.Pages[pageIndex].Width, doc.Pages[pageIndex].Height);
                    if (doc.Pages[pageIndex].Text != null && doc.Pages[pageIndex].Text.Zone != null)
                    {
                        TextZone[] textItems = doc.Pages[pageIndex].Text.Zone.OrientedSearchForText(rect, doc.Pages[pageIndex].Height);
                        foreach (var item in textItems)
                        {
                            var r = item.Rectangle;
                            var hh = (r.YMax - r.YMin);
                            if (checkBox1.Checked)
                            {
                                gr.DrawRectangle(Pens.Black, r.XMin * koef, (doc.Pages[pageIndex].Height - r.YMin - hh) * koef, (r.XMax - r.XMin) * koef, hh * koef);
                                if (item == highlightedZone)
                                {
                                    gr.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Red)), r.XMin * koef, (doc.Pages[pageIndex].Height - r.YMin - hh) * koef, (r.XMax - r.XMin) * koef, hh * koef);
                                }
                            }

                            //  gr.DrawRectangle(Pens.Black, r.Left * koef, r.Top * koef, r.Width * koef, r.Height * koef);
                        }
                    }

                    gr.DrawRectangle(Pens.Black, bound1);
                    gr.DrawImage(page1, new PointF(0, 0));
                }
                gr.Restore(state);
            }
            if (page2 != null)
            {
                float shift2 = 0;
                for (int i = 0; i < pageIndex + 1; i++)
                {
                    shift2 += (sizes[i].Height * koef) + vgap;
                }
                var state = gr.Save();
                gr.TranslateTransform(0, shift2);

                lock (obj1)
                {
                    if (page2 != null)
                    {
                        gr.DrawRectangle(Pens.Black, bound2);
                        gr.DrawImage(page2, new PointF(0, 0));
                    }
                }
                gr.Restore(state);

            }

            gr.ResetTransform();
            var pp = GetBack(pos);
            //gr.DrawString(pp.X + "; " + pp.Y, new Font("Segoe", 8), Brushes.Black, 0, 0);            
            pictureBox1.Image = bmp;
        }

        public PointF GetBack(PointF p)
        {
            var px = (p.X + sx) / zoom;
            var py = (p.Y + sy) / zoom;
            return new PointF(px, py);
        }
        public PointF GetForward(PointF p)
        {
            var px = (p.X * zoom - sx);
            var py = (p.Y * zoom - sy);
            return new PointF(px, py);
        }
        void GoToPage(int newp)
        {
            float accum = 0;
            for (int i = 0; i < newp; i++)
            {
                accum += (sizes[i].Height * koef) + vgap;
            }
            sy = accum * zoom;
            ReloadPages(newp);
        }

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var newp = int.Parse(textBox1.Text);

                GoToPage(newp);
            }
        }

        TextZone highlightedZone = null;
        private void Button1_Click(object sender, EventArgs e)
        {

            listView1.Items.Clear();
            for (int i = 0; i < doc.Pages.Count(); i++)
            {
                if (doc.Pages[i].Text == null) continue;
                var ss = doc.Pages[i].Text.Zone.SearchForText(textBox2.Text);
                if (ss.Any())
                {
                    foreach (var item in ss)
                    {
                        listView1.Items.Add(new ListViewItem(new string[] { "page " + i }) { Tag = new Tuple<int, TextZone>(i, item) });
                    }                    
                }

            }
            if (listView1.Items.Count > 0)
            {
                tableLayoutPanel1.ColumnStyles[1].Width = 100;
            }
            else
            {
                MessageBox.Show("text not found");
            }

        }

        
        private void ListView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var zone = listView1.SelectedItems[0].Tag as Tuple<int, TextZone>;
            GoToPage(zone.Item1);
            highlightedZone = zone.Item2;
        }
    }
}
