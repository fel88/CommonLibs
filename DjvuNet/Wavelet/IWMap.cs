using System;
using DjvuNet.Graphics;

namespace DjvuNet.Wavelet
{
    /// <summary>
    /// This class represents structured wavelette data.
    /// </summary>
    public sealed class IWMap
    {
        #region Public Variables

        #region Bh

        private int _bh;

        /// <summary>
        /// Gets or sets the Bh value
        /// </summary>
        public int Bh
        {
            get
            {
                return _bh;
            }

            set
            {
                if (Bh != value)
                {
                    _bh = value;
                }
            }
        }

        #endregion Bh

        #region Blocks

        private IWBlock[] _blocks;

        /// <summary>
        /// Gets or sets the blocks
        /// </summary>
        public IWBlock[] Blocks
        {
            get { return _blocks; }

            set
            {
                if (Blocks != value)
                {
                    _blocks = value;
                }
            }
        }

        #endregion Blocks

        #region Bw

        private int _bw;

        /// <summary>
        /// Gets or sets the Bw value
        /// </summary>
        public int Bw
        {
            get { return _bw; }

            set
            {
                if (Bw != value)
                {
                    _bw = value;
                }
            }
        }

        #endregion Bw

        #region Ih

        private int _ih;

        /// <summary>
        /// Gets or sets the Ih value
        /// </summary>
        public int Ih
        {
            get { return _ih; }

            private set
            {
                if (Ih != value)
                {
                    _ih = value;
                }
            }
        }

        #endregion Ih

        #region Iw

        private int _iw;

        /// <summary>
        /// Gets or sets the Iw value
        /// </summary>
        public int Iw
        {
            get { return _iw; }

            set
            {
                if (Iw != value)
                {
                    _iw = value;
                }
            }
        }

        #endregion Iw

        #region Nb

        private int _nb;

        /// <summary>
        /// Gets or sets the Nb value
        /// </summary>
        public int Nb
        {
            get { return _nb; }

            private set
            {
                if (Nb != value)
                {
                    _nb = value;
                }
            }
        }

        #endregion Nb

        #region Top

        private int _top;

        /// <summary>
        /// Gets or sets the top value
        /// </summary>
        public int Top
        {
            get { return _top; }

            private set
            {
                if (Top != value)
                {
                    _top = value;
                }
            }
        }

        #endregion Top

        #endregion Public Variables

        #region Constructors

        /// <summary> Creates a new Map object.</summary>
        public IWMap()
        {
        }

        public IWMap(int w, int h)
        {
            Init(w, h);
        }

        #endregion Constructors

        #region Public Methods

        public IWMap Duplicate()
        {
            IWMap retval = null;

            try
            {
                retval = new IWMap { Bh = Bh, Blocks = this.Blocks, Bw = Bw, Ih = Ih, Iw = Iw, Nb = Nb, Top = Top };

                IWBlock[] blocks = (IWBlock[])this.Blocks.Clone();
                ((IWMap)retval).Blocks = blocks;

                for (int i = 0; i < Nb; i++)
                {
                    blocks[i] = (IWBlock)blocks[i].Duplicate();
                }
            }
            catch (Exception ignored)
            {
            }

            return retval;
        }

        #endregion Public Methods

        #region Private Methods

        private static void Backward(short[] p, int pidx, int w, int h, int rowsize, int begin, int end)
        {
            for (int scale = begin >> 1; scale >= end; scale >>= 1)
            {
                for (int j = 0; j < w; j += scale)
                {
                    BackwardFilter(p, pidx, j, j + (h * rowsize), j, scale * rowsize);
                }

                for (int i = 0; i < h; i += scale)
                {
                    BackwardFilter(p, pidx, i * rowsize, (i * rowsize) + w, i * rowsize, scale);
                }
            }
        }

        private static void BackwardFilter(short[] p, int pidx, int b, int e, int z, int s)
        {
            int s3 = 3 * s;

            if ((z < b) || (z > e))
            {
                throw new Exception("Filter parameters were out of bounds");
            }

            int n = z;
            int bb;
            int cc;
            int aa = bb = cc = 0;
            int dd = ((n + s) >= e) ? 0 : ((p[pidx + n + s]));

            for (; (n + s3) < e; n = (n + s3) - s)
            {
                aa = bb;
                bb = cc;
                cc = dd;
                dd = p[pidx + n + s3];
                p[pidx + n] = (short)(p[pidx + n] - ((((9 * (bb + cc)) - (aa + dd)) + 16) >> 5));
            }

            for (; n < e; n = n + s + s)
            {
                aa = bb;
                bb = cc;
                cc = dd;
                dd = 0;
                p[pidx + n] = (short)(p[pidx + n] - ((((9 * (bb + cc)) - (aa + dd)) + 16) >> 5));
            }

            n = z + s;
            aa = 0;
            bb = p[(pidx + n) - s];
            cc = ((n + s) >= e) ? 0 : ((p[pidx + n + s]));
            dd = ((n + s3) >= e) ? 0 : ((p[pidx + n + s3]));

            if (n < e)
            {
                int x = bb;

                if ((n + s) < e)
                {
                    x = (bb + cc + 1) >> 1;
                }

                p[pidx + n] = (short)(p[pidx + n] + x);
                n = n + s + s;
            }

            for (; (n + s3) < e; n = (n + s3) - s)
            {
                aa = bb;
                bb = cc;
                cc = dd;
                dd = p[pidx + n + s3];

                int x = (((9 * (bb + cc)) - (aa + dd)) + 8) >> 4;
                p[pidx + n] = (short)(p[pidx + n] + x);
            }

            if ((n + s) < e)
            {
                aa = bb;
                bb = cc;
                cc = dd;
                dd = 0;

                int x = (bb + cc + 1) >> 1;
                p[pidx + n] = (short)(p[pidx + n] + x);
                n = n + s + s;
            }

            if (n < e)
            {
                aa = bb;
                bb = cc;
                cc = dd;
                dd = 0;

                int x = bb;
                p[pidx + n] = (short)(p[pidx + n] + x);
            }
        }

        private int GetBucketCount()
        {
            int buckets = 0;

            for (int blockno = 0; blockno < Nb; blockno++)
            {
                for (int buckno = 0; buckno < 64; buckno++)
                {
                    if (Blocks[blockno].GetBlock(buckno) != null)
                    {
                        buckets++;
                    }
                }
            }

            return buckets;
        }

        public void Image(int index, sbyte[] img8, int rowsize, int pixsep, bool fast)
        {
            short[] data16 = new short[Bw * Bh];
            short[] liftblock = new short[1024];
            int pidx = 0;
            IWBlock[] block = Blocks;
            int blockidx = 0;
            int ppidx = 0;

            for (int i = 0; i < Bh; i += 32, pidx += (32 * Bw))
            {
                for (int j = 0; j < Bw; j += 32)
                {
                    block[blockidx].WriteLiftBlock(liftblock, 0, 64);
                    blockidx++;

                    ppidx = pidx + j;

                    for (int ii = 0, p1idx = 0; ii++ < 32; p1idx += 32, ppidx += Bw)
                    {
                        Array.Copy(liftblock, p1idx, data16, ppidx, 32);
                    }
                }
            }

            if (fast)
            {
                Backward(data16, 0, Iw, Ih, Bw, 32, 2);
                pidx = 0;

                for (int i = 0; i < Bh; i += 2, pidx += Bw)
                {
                    for (int jj = 0; jj < Bw; jj += 2, pidx += 2)
                    {
                        data16[pidx + Bw] = data16[pidx + Bw + 1] = data16[pidx + 1] = data16[pidx];
                    }
                }
            }
            else
            {
                Backward(data16, 0, Iw, Ih, Bw, 32, 1);
            }

            pidx = 0;

            for (int i = 0, rowidx = index; i++ < Ih; rowidx += rowsize, pidx += Bw)
            {
                for (int j = 0, pixidx = rowidx; j < Iw; pixidx += pixsep)
                {
                    int x = (data16[pidx + (j++)] + 32) >> 6;

                    if (x < -128)
                    {
                        x = -128;
                    }
                    else if (x > 127)
                    {
                        x = 127;
                    }

                    img8[pixidx] = (sbyte)x;
                }
            }
        }

        public void Image(int subsample, Rectangle rect, int index, sbyte[] img8, int rowsize, int pixsep, bool fast)
        {
            int nlevel = 0;

            while ((nlevel < 5) && ((32 >> nlevel) > subsample))
            {
                nlevel++;
            }

            int boxsize = 1 << nlevel;

            if (subsample != (32 >> nlevel))
            {
                throw new ArgumentException("(Map::image) Unsupported subsampling factor");
            }

            if (rect.Empty)
            {
                throw new ArgumentException("(Map::image) Rectangle is empty");
            }

            Rectangle irect = new Rectangle(0, 0, ((Iw + subsample) - 1) / subsample, ((Ih + subsample) - 1) / subsample);

            if ((rect.Right < 0) || (rect.Bottom < 0) || (rect.Left > irect.Left) || (rect.Top > irect.Top))
            {
                throw new ArgumentException("(Map::image) Rectangle is out of bounds: " + rect.Right + "," + rect.Bottom +
                                            "," + rect.Left + "," + rect.Top + "," + irect.Left + "," + irect.Top);
            }

            Rectangle[] needed = new Rectangle[8];
            Rectangle[] recomp = new Rectangle[8];

            for (int i = 0; i < 8; )
            {
                needed[i] = new Rectangle();
                recomp[i++] = new Rectangle();
            }

            int r = 1;
            needed[nlevel] = (Rectangle)rect.Duplicate();
            recomp[nlevel] = (Rectangle)rect.Duplicate();

            for (int i = nlevel - 1; i >= 0; i--)
            {
                needed[i] = recomp[i + 1];
                needed[i].Inflate(3 * r, 3 * r);
                needed[i].Intersect(needed[i], irect);
                r += r;
                recomp[i].Right = ((needed[i].Right + r) - 1) & ~(r - 1);
                recomp[i].Left = needed[i].Left & ~(r - 1);
                recomp[i].Bottom = ((needed[i].Bottom + r) - 1) & ~(r - 1);
                recomp[i].Top = needed[i].Top & ~(r - 1);
            }

            Rectangle work = new Rectangle();
            work.Right = needed[0].Right & ~(boxsize - 1);
            work.Bottom = needed[0].Bottom & ~(boxsize - 1);
            work.Left = ((needed[0].Left - 1) & ~(boxsize - 1)) + boxsize;
            work.Top = ((needed[0].Top - 1) & ~(boxsize - 1)) + boxsize;

            int dataw = work.Width;
            short[] data = new short[dataw * work.Height];
            int blkw = Bw >> 5;
            int lblock = ((work.Bottom >> nlevel) * blkw) + (work.Right >> nlevel);

            short[] liftblock = new short[1024];

            for (int by = work.Bottom, ldata = 0;
                 by < work.Top;
                 by += boxsize, ldata += (dataw << nlevel), lblock += blkw)
            {
                for (int bx = work.Right, bidx = lblock, rdata = ldata;
                     bx < work.Left;
                     bx += boxsize, bidx++, rdata += boxsize)
                {
                    IWBlock block = Blocks[bidx];
                    int mlevel = nlevel;

                    if ((nlevel > 2) &&
                        (((bx + 31) < needed[2].Right) || (bx > needed[2].Left) || ((by + 31) < needed[2].Bottom) ||
                         (by > needed[2].Top)))
                    {
                        mlevel = 2;
                    }

                    int bmax = ((1 << (mlevel + mlevel)) + 15) >> 4;
                    int ppinc = 1 << (nlevel - mlevel);
                    int ppmod1 = dataw << (nlevel - mlevel);
                    int ttmod0 = 32 >> mlevel;
                    int ttmod1 = ttmod0 << 5;
                    block.WriteLiftBlock(liftblock, 0, bmax);

                    for (int ii = 0, tt = 0, pp = rdata; ii < boxsize; ii += ppinc, pp += ppmod1, tt += (ttmod1 - 32))
                    {
                        for (int jj = 0; jj < boxsize; jj += ppinc, tt += ttmod0)
                        {
                            data[pp + jj] = liftblock[tt];
                        }
                    }
                }
            }

            r = boxsize;

            for (int i = 0; i < nlevel; i++)
            {
                Rectangle comp = needed[i];
                comp.Right = comp.Right & ~(r - 1);
                comp.Bottom = comp.Bottom & ~(r - 1);
                comp.Translate(-work.Right, -work.Bottom);

                if (fast && (i >= 4))
                {
                    for (int ii = comp.Bottom, pp = (comp.Bottom * dataw); ii < comp.Top; ii += 2, pp += (dataw + dataw))
                    {
                        for (int jj = comp.Right; jj < comp.Left; jj += 2)
                        {
                            data[pp + jj + dataw] = data[pp + jj + dataw + 1] = data[pp + jj + 1] = data[pp + jj];
                        }
                    }

                    break;
                }

                Backward(data, (comp.Bottom * dataw) + comp.Right, comp.Width, comp.Height, dataw, r, r >> 1);
                r >>= 1;
            }

            Rectangle nrect = (Rectangle)rect.Duplicate();
            nrect.Translate(-work.Right, -work.Bottom);

            for (int i = nrect.Bottom, pidx = (nrect.Bottom * dataw), ridx = index;
                 i++ < nrect.Top;
                 ridx += rowsize, pidx += dataw)
            {
                for (int j = nrect.Right, pixidx = ridx; j < nrect.Left; j++, pixidx += pixsep)
                {
                    int x = (data[pidx + j] + 32) >> 6;

                    if (x < -128)
                    {
                        x = -128;
                    }
                    else if (x > 127)
                    {
                        x = 127;
                    }

                    img8[pixidx] = (sbyte)x;
                }
            }
        }

        public IWMap Init(int w, int h)
        {
            Iw = w;
            Ih = h;
            Bw = ((w + 32) - 1) & unchecked((int)0xffffffe0);
            Bh = ((h + 32) - 1) & unchecked((int)0xffffffe0);
            Nb = (Bw * Bh) / 1024;
            Blocks = new IWBlock[Nb];

            for (int i = 0; i < Blocks.Length; i++)
            {
                Blocks[i] = new IWBlock();
            }

            return this;
        }

        private void Slashres(int res)
        {
            int minbucket = 1;

            if (res < 2)
            {
                return;
            }

            if (res < 4)
            {
                minbucket = 16;
            }
            else if (res < 8)
            {
                minbucket = 4;
            }

            for (int blockno = 0; blockno < Blocks.Length; blockno++)
            {
                for (int buckno = minbucket; buckno < 64; buckno++)
                {
                    Blocks[blockno].ClearBlock(buckno);
                }
            }
        }

        #endregion Private Methods
    }
}