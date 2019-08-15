﻿// -----------------------------------------------------------------------
// <copyright file="DjvuPage.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using DjvuNet.DataChunks;
using DjvuNet.DataChunks.Directory;
using DjvuNet.DataChunks.Text;
using DjvuNet.Graphics;
using DjvuNet.JB2;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using DjvuNet.Wavelet;
using Bitmap = System.Drawing.Bitmap;
using ColorPalette = DjvuNet.DataChunks.Graphics.ColorPalette;
using Image = System.Drawing.Image;
using Rectangle = System.Drawing.Rectangle;

namespace DjvuNet
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using GBitmap = DjvuNet.Graphics.Bitmap;
    using GMap = DjvuNet.Graphics.Map;
    using GPixel = DjvuNet.Graphics.Pixel;
    using GPixelReference = DjvuNet.Graphics.PixelReference;
    using GPixmap = DjvuNet.Graphics.PixelMap;
    using GRect = DjvuNet.Graphics.Rectangle;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DjvuPage : INotifyPropertyChanged
    {
        #region Private Variables

        /// <summary>
        /// True if the page has been previously loaded, false otherwise
        /// </summary>
        private bool _hasLoaded = false;

        private object _loadingLock = new object();
        private bool _isBackgroundDecoded = false;

        #endregion Private Variables

        #region Public Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Public Events

        #region Public Properties

        #region Thumbnail

        private TH44Chunk _thumbnail;

        /// <summary>
        /// Gets the thumbnail for the page
        /// </summary>
        public TH44Chunk Thumbnail
        {
            get
            {
                return _thumbnail;
            }

            private set
            {
                if (Thumbnail != value)
                {
                    _thumbnail = value;
                }
            }
        }

        #endregion Thumbnail

        #region Document

        private DjvuDocument _document;

        /// <summary>
        /// Gets the document the page belongs to
        /// </summary>
        public DjvuDocument Document
        {
            get
            {
                return _document;
            }

            private set
            {
                if (Document != value)
                {
                    _document = value;
                }
            }
        }

        #endregion Document

        #region IncludedItems

        private DjviChunk[] _includedItems;

        /// <summary>
        /// Gets the included items
        /// </summary>
        public DjviChunk[] IncludedItems
        {
            get
            {
                return _includedItems;
            }

            private set
            {
                if (IncludedItems != value)
                {
                    _includedItems = value;
                }
            }
        }

        #endregion IncludedItems

        #region PageForm

        private FormChunk _pageForm;

        /// <summary>
        /// Gets the form chunk for the page
        /// </summary>
        public FormChunk PageForm
        {
            get
            {
                return _pageForm;
            }

            private set
            {
                if (PageForm != value)
                {
                    _pageForm = value;
                }
            }
        }

        #endregion PageForm

        #region Info

        private InfoChunk _info;

        /// <summary>
        /// Gets the info chunk for the page
        /// </summary>
        public InfoChunk Info
        {
            get
            {
                if (_info == null)
                {
                    _info = (InfoChunk)PageForm.Children.FirstOrDefault(x => x is InfoChunk);
                }

                return _info;
            }
        }

        #endregion Info

        #region Width

        /// <summary>
        /// Gets the width of the page
        /// </summary>
        public int Width
        {
            get
            {
                if (Info != null)
                {
                    return Info.Width;
                }

                return 0;
            }
        }

        #endregion Width

        #region Height

        /// <summary>
        /// Gets the height of the page
        /// </summary>
        public int Height
        {
            get
            {
                if (Info != null)
                {
                    return Info.Height;
                }

                return 0;
            }
        }

        #endregion Height

        #region Header

        private DirmComponent _header;

        /// <summary>
        /// Gets directory header for the page
        /// </summary>
        public DirmComponent Header
        {
            get
            {
                return _header;
            }

            private set
            {
                if (Header != value)
                {
                    _header = value;
                }
            }
        }

        #endregion Header

        #region Text

        private DataChunks.Text.TextChunk _text;

        /// <summary>
        /// Gets the info chunk for the page
        /// </summary>
        public DataChunks.Text.TextChunk Text
        {
            get
            {
                if (_text == null)
                {
                    _text = (DataChunks.Text.TextChunk)PageForm.Children.FirstOrDefault(x => x is DataChunks.Text.TextChunk);
                }

                return _text;
            }
        }

        #endregion Text

        #region ForegroundJB2Image

        private JB2.JB2Image _foregroundJB2Image;

        /// <summary>
        /// Gets the forground image
        /// </summary>
        public JB2.JB2Image ForegroundJB2Image
        {
            get
            {
                if (_foregroundJB2Image == null)
                {
                    // Get the first chunk if present
                    var chunk = (SjbzChunk)PageForm.Children.FirstOrDefault(x => x is SjbzChunk);

                    if (chunk != null)
                    {
                        _foregroundJB2Image = chunk.Image;
                    }
                }

                return _foregroundJB2Image;
            }
        }

        #endregion ForegroundJB2Image

        #region ForegroundIWPixelMap

        private Wavelet.IWPixelMap _foregroundIWPixelMap;

        /// <summary>
        /// Gets the Foreground pixel map
        /// </summary>
        public Wavelet.IWPixelMap ForegroundIWPixelMap
        {
            get
            {
                if (_foregroundIWPixelMap == null)
                {
                    var chunk = (FG44Chunk)PageForm.Children.FirstOrDefault(x => x is FG44Chunk);

                    if (chunk != null)
                    {
                        _foregroundIWPixelMap = chunk.ForegroundImage;
                    }
                }

                return _foregroundIWPixelMap;
            }
        }

        #endregion ForegroundIWPixelMap

        #region BackgroundIWPixelMap

        private Wavelet.IWPixelMap _backgroundIWPixelMap;

        /// <summary>
        /// Gets the background pixel map
        /// </summary>
        public Wavelet.IWPixelMap BackgroundIWPixelMap
        {
            get
            {
                if (_backgroundIWPixelMap == null)
                {
                    var chunk = (BG44Chunk)PageForm.Children.FirstOrDefault(x => x is BG44Chunk);

                    if (chunk != null)
                    {
                        _backgroundIWPixelMap = chunk.BackgroundImage;
                    }
                }

                return _backgroundIWPixelMap;
            }
        }

        #endregion BackgroundIWPixelMap

        #region ForegroundPalette

        private ColorPalette _foregroundPalette;

        /// <summary>
        /// Gets the palette for the foreground
        /// </summary>
        public ColorPalette ForegroundPalette
        {
            get
            {
                if (_foregroundPalette == null)
                {
                    //_foregroundPalette = PageForm.Children.FirstOrDefault(x => x is XYZ);
                }

                return _foregroundPalette;
            }
        }

        #endregion ForegroundPalette

        #region ForegroundPixelMap

        private GPixmap _foregroundPixelMap;

        /// <summary>
        /// Gets the pixel map for the foreground
        /// </summary>
        public GPixmap ForegroundPixelMap
        {
            get
            {
                if (_foregroundPixelMap == null)
                {
                    _foregroundPixelMap = ForegroundIWPixelMap.GetPixmap();
                }

                return _foregroundPixelMap;
            }
        }

        #endregion ForegroundPixelMap

        #region IsPageImageCached

        private bool _isPageImageCached;

        /// <summary>
        /// True if the page image is cached, false otherwise
        /// </summary>
        public bool IsPageImageCached
        {
            get
            {
                return _isPageImageCached;
            }

            set
            {
                if (IsPageImageCached != value)
                {
                    _isPageImageCached = value;
                    _image = null;

                    if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("IsPageImageCached"));

                    //OnPropertyChanged("IsPageImageCached");
                }
            }
        }

        #endregion IsPageImageCached

        #region Image

        private System.Drawing.Bitmap _image;

        /// <summary>
        /// Gets the image for the page
        /// </summary>
        public System.Drawing.Bitmap Image
        {
            get
            {
                if (_image == null)
                {
                    _image = BuildImage();
                }

                return _image;
            }

            private set
            {
                _image = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Image"));
            }
        }

        #endregion Image

        #region IsInverted

        private bool _isInverted;

        /// <summary>
        /// True if the image is inverted, false otherwise
        /// </summary>
        public bool IsInverted
        {
            get
            {
                return _isInverted;
            }

            set
            {
                if (IsInverted != value)
                {
                    _isInverted = value;
                    ClearImage();
                    ThumbnailImage = InvertImage(ThumbnailImage);
                }
            }
        }

        #endregion IsInverted

        #region ThumbnailImage

        private System.Drawing.Bitmap _thumbnailImage;

        /// <summary>
        /// Gets or sets the thumbnail image for the page
        /// </summary>
        public System.Drawing.Bitmap ThumbnailImage
        {
            get
            {
                return _thumbnailImage;
            }

            set
            {
                if (ThumbnailImage != value)
                {
                    _thumbnailImage = value;
                    if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("ThumbnailImage"));

                    //OnPropertyChanged("ThumbnailImage");
                }
            }
        }

        #endregion ThumbnailImage

        #region PageNumber

        private int _pageNumber;

        /// <summary>
        /// Gets the number of the page
        /// </summary>
        public int PageNumber
        {
            get
            {
                return _pageNumber;
            }

            private set
            {
                if (PageNumber != value)
                {
                    _pageNumber = value;
                    if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("PageNumber"));

                    //OnPropertyChanged("PageNumber");
                }
            }
        }

        #endregion PageNumber

        #region IsColor

        /// <summary>
        /// True if this is photo or compound
        /// </summary>
        public bool IsColor
        {
            get { return IsLegalCompound() || IsLegalBilevel(); }
        }

        #endregion IsColor

        #endregion Public Properties

        #region Constructors

        public DjvuPage(int pageNumber, DjvuDocument document, DirmComponent header, TH44Chunk thumbnail, DjviChunk[] includedItems, FormChunk form)
        {
            PageNumber = pageNumber;
            Document = document;
            Header = header;
            Thumbnail = thumbnail;
            IncludedItems = includedItems;
            PageForm = form;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Preloads the page
        /// </summary>
        public void Preload()
        {
            lock (_loadingLock)
            {
                if (_hasLoaded == false)
                {
                    // Build all the images
                    GetBackgroundImage(1, true);
                    GetForegroundImage(1, true);
                    GetTextImage(1, true);

                    _hasLoaded = true;
                }
            }
        }

        /// <summary>
        /// Gets the text for the rectangle location
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public string GetTextForLocation(Rectangle rect)
        {
            if (Text == null || Text.Zone == null) return "";

            StringBuilder text = new StringBuilder();

            TextZone[] textItems = Text.Zone.OrientedSearchForText(rect, Height);

            TextZone currentParent = null;

            foreach (TextZone item in textItems)
            {
                if (currentParent != item.Parent)
                {
                    text.AppendLine();
                    currentParent = item.Parent;
                }

                if (item.Parent == currentParent)
                {
                    text.Append(item.Text + " ");
                }
            }

            return text.ToString().Trim();
        }

        /// <summary>
        /// Clears the stored image from memory
        /// </summary>
        public void ClearImage()
        {
            IsPageImageCached = false;

            if (_image != null)
            {
                _image.Dispose();
                _image = null;
            }
        }

        /// <summary>
        /// Resizes the image to the new dimensions
        /// </summary>
        /// <param name="srcImage"></param>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        /// <returns></returns>
        public System.Drawing.Bitmap ResizeImage(System.Drawing.Bitmap srcImage, int newWidth, int newHeight)
        {
            // Check if the image needs resizing
            if (srcImage.Width == newWidth && srcImage.Height == newHeight)
            {
                return srcImage;
            }

            // Resize the image
            System.Drawing.Bitmap newImage = new System.Drawing.Bitmap(newWidth, newHeight);

            using (System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(srcImage, new System.Drawing.Rectangle(0, 0, newWidth, newHeight));
            }

            srcImage.Dispose();

            return newImage;
        }

        /// <summary>
        /// Resizes the pages image to the new dimensions
        /// </summary>
        /// <param name="srcImage"></param>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        /// <returns></returns>
        public System.Drawing.Bitmap ResizeImage(int newWidth, int newHeight)
        {
            return ResizeImage(Image, newWidth, newHeight);
        }

        /// <summary>
        /// Extracts a thumbnail image for the page
        /// </summary>
        /// <returns></returns>
        public System.Drawing.Bitmap ExtractThumbnailImage()
        {
            if (Thumbnail != null)
            {
                return Thumbnail.Image;
            }

            var result = BuildImage();
            var scaleAmount = (double)128 / result.Width;

            result = ResizeImage(result, (int)(result.Width * scaleAmount), (int)(result.Height * scaleAmount));

            return result;
        }

        /// <summary>
        /// Gets the background pixmap
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="subSample"></param>
        /// <param name="gamma"></param>
        /// <param name="retval"></param>
        /// <returns></returns>
        public GPixmap GetBgPixmap(GRect rect, int subsample, double gamma, GPixmap retval)
        {
            GPixmap pm = null;
            int width = (Info == null)
                            ? 0
                            : Info.Width;
            int height = (Info == null)
                             ? 0
                             : Info.Height;

            if ((width <= 0) || (height <= 0) || (Info == null))
            {
                return null;
            }

            double gamma_correction = 1.0D;

            if ((gamma > 0.0D) && (Info != null))
            {
                gamma_correction = gamma / Info.Gamma;
            }

            if (gamma_correction < 0.10000000000000001D)
            {
                gamma_correction = 0.10000000000000001D;
            }
            else if (gamma_correction > 10D)
            {
                gamma_correction = 10D;
            }

            IWPixelMap bgIWPixmap = BackgroundIWPixelMap;

            if (bgIWPixmap != null)
            {
                int w = bgIWPixmap.Width;
                int h = bgIWPixmap.Height;

                if ((w == 0) || (h == 0) || (width == 0) || (height == 0))
                {
                    return null;
                }

                int red = ComputeRed(width, height, w, h);

                if ((red < 1) || (red > 12))
                {
                    return null;
                }

                if (subsample == red)
                {
                    pm = bgIWPixmap.GetPixmap(1, rect, retval);
                }
                else if (subsample == (2 * red))
                {
                    pm = bgIWPixmap.GetPixmap(2, rect, retval);
                }
                else if (subsample == (4 * red))
                {
                    pm = bgIWPixmap.GetPixmap(4, rect, retval);
                }
                else if (subsample == (8 * red))
                {
                    pm = bgIWPixmap.GetPixmap(8, rect, retval);
                }
                else if ((red * 4) == (subsample * 3))
                {
                    GRect xrect = new GRect();
                    xrect.Right = (int)Math.Floor(rect.Right * 4D / 3D);
                    xrect.Bottom = (int)Math.Floor(rect.Bottom * 4D / 3D);
                    xrect.Left = (int)Math.Ceiling((double)rect.Left * 4D / 3D);
                    xrect.Top = (int)Math.Ceiling((double)rect.Top * 4D / 3D);

                    GRect nrect = new GRect(0, 0, rect.Width, rect.Height);
                    if (xrect.Left > w)
                    {
                        xrect.Left = w;
                    }

                    if (xrect.Top > h)
                    {
                        xrect.Top = h;
                    }

                    GPixmap ipm = bgIWPixmap.GetPixmap(1, xrect, null);
                    pm = (retval != null)
                             ? retval
                             : new GPixmap();
                    pm.Downsample43(ipm, nrect);
                }
                else
                {
                    int po2 = 16;

                    while ((po2 > 1) && (subsample < (po2 * red)))
                    {
                        po2 >>= 1;
                    }

                    int inw = ((w + po2) - 1) / po2;
                    int inh = ((h + po2) - 1) / po2;
                    int outw = ((width + subsample) - 1) / subsample;
                    int outh = ((height + subsample) - 1) / subsample;
                    PixelMapScaler ps = new PixelMapScaler(inw, inh, outw, outh);
                    ps.SetHorzRatio(red * po2, subsample);
                    ps.SetVertRatio(red * po2, subsample);

                    GRect xrect = ps.GetRequiredRect(rect);
                    GPixmap ipm = bgIWPixmap.GetPixmap(po2, xrect, null);
                    pm = (retval != null)
                             ? retval
                             : new GPixmap();
                    ps.Scale(xrect, ipm, rect, pm);
                }

                if ((pm != null) && (gamma_correction != 1.0D))
                {
                    pm.ApplyGammaCorrection(gamma_correction);

                    for (int i = 0; i < 9; i++)
                    {
                        pm.ApplyGammaCorrection(gamma_correction);
                    }
                }

                return pm;
            }
            else
            {
                return null;
            }
        }

        public Graphics.Bitmap BuildBitmap(Graphics.Rectangle rect, int subsample, int align, Bitmap retVal)
        {
            return GetBitmap(rect, subsample, align, null);
        }

        public GPixmap GetPixelMap(GRect rect, int subsample, double gamma, PixelMap retval)
        {
            if (rect.Empty)
            {
                return (retval == null)
                ? (new PixelMap())
                : retval.Init(0, 0, null);
            }

            GPixmap bg = GetBgPixmap(rect, subsample, gamma, retval);
            if (ForegroundJB2Image != null)
            {
                if (bg == null)
                {
                    bg = (retval == null) ? new PixelMap() : retval;
                    bg.Init(
                     rect.Height,
                     rect.Width, GPixel.WhitePixel);
                }
                if (Stencil(bg, rect, subsample, gamma))
                {
                    retval = bg;
                }
            }
            else
            {
                retval = bg;
            }

            return retval;
        }

        /// <summary>
        /// Gets a complete image for the page
        /// </summary>
        /// <returns></returns>
        public System.Drawing.Bitmap BuildPageImage()
        {
            throw new NotImplementedException();
            /*int subsample = 1;
            if (this.Info == null && Document.FormChunk.Children[0].ChunkID == "DJVU" && Document.FormChunk.Children[1] is InfoChunk)
                this._info = (InfoChunk)Document.FormChunk.Children[1];

            int width = Info.Width / subsample;
            int height = Info.Height / subsample;

            var map = GetMap(new GRect(0, 0, width, height), subsample, null);

            if (map == null) return new Bitmap(Info.Width, Info.Height);

            int[] pixels = new int[width * height];

            map.FillRgbPixels(0, 0, width, height, pixels, 0, width);
            var image = ConvertDataToImage(pixels);

            if (IsInverted == true)
            {
                image = InvertImage(image);
            }

            return image;*/
        }



        /// <summary>
        /// Gets the image for the page
        /// </summary>
        /// <returns></returns>
        public System.Drawing.Bitmap BuildImage(int subsample = 1)
        {
            lock (_loadingLock)
            {
                DateTime start = DateTime.Now;
                System.Drawing.Bitmap background = GetBackgroundImage(subsample, false);
                Console.WriteLine("Background: " + DateTime.Now.Subtract(start).TotalMilliseconds);
                start = DateTime.Now;

                using (System.Drawing.Bitmap foreground = GetForegroundImage(subsample, false))
                {
                    Console.WriteLine("Foreground: " + DateTime.Now.Subtract(start).TotalMilliseconds);
                    start = DateTime.Now;

                    using (System.Drawing.Bitmap mask = GetTextImage(subsample, false))
                    {
                        Console.WriteLine("Mask: " + DateTime.Now.Subtract(start).TotalMilliseconds);
                        start = DateTime.Now;

                        _hasLoaded = true;

                        BitmapData backgroundData =
                            background.LockBits(new System.Drawing.Rectangle(0, 0, background.Width, background.Height),
                                                ImageLockMode.ReadWrite, background.PixelFormat);
                        int backgroundPixelSize = GetPixelSize(backgroundData);

                        BitmapData foregroundData =
                            foreground.LockBits(new System.Drawing.Rectangle(0, 0, foreground.Width, foreground.Height),
                                                ImageLockMode.ReadOnly, foreground.PixelFormat);
                        int foregroundPixelSize = GetPixelSize(foregroundData);

                        BitmapData maskData = mask.LockBits(new System.Drawing.Rectangle(0, 0, mask.Width, mask.Height),
                                                            ImageLockMode.ReadOnly, mask.PixelFormat);



                        if (backgroundData.Height != foregroundData.Height)
                        {
                            throw new ArgumentException("foreground height!=background height");
                        }

                        int size = backgroundData.Stride * backgroundData.Height;
                        byte[] data = new byte[size];
                        


                        int fsize = foregroundData.Stride * foregroundData.Height;
                        byte[] fdata = new byte[fsize];
                        System.Runtime.InteropServices.Marshal.Copy(foregroundData.Scan0, fdata, 0, fsize);

                        int msize = maskData.Stride * maskData.Height;
                        byte[] mdata = new byte[msize];
                        System.Runtime.InteropServices.Marshal.Copy(maskData.Scan0, mdata, 0, msize);

                        //int maskPixelSize = GetPixelSize(maskData);

                        int height = background.Height;
                        int width = background.Width;
                        var bpxsz = Bitmap.GetPixelFormatSize(background.PixelFormat) / 8;
                        var frsz = Bitmap.GetPixelFormatSize(foreground.PixelFormat) / 8;
                        var msksz = Bitmap.GetPixelFormatSize(mask.PixelFormat) / 8;
                        for (int y = 0; y < height; y++)
                        {



                            byte maskRow = mdata[y * maskData.Stride];                            
                            int mult = y * backgroundData.Stride;
                            int fmult = y * foregroundData.Stride;
                            for (int x = 0; x < width; x++)
                            {
                                // Check if the mask byte is set
                                maskRow = mdata[y * maskData.Stride + x * msksz];
                                if (maskRow > 0)
                                {
                                    var flag = _isInverted == true;
                                    var b1 = BitConverter.ToUInt32(fdata, fmult + x * frsz);
                                    var b2 = InvertColor(b1);
                                    var b3 = flag ? b2 : b1;

                                    for (int i = 0; i < frsz; i++)
                                    {
                                        data[mult + x * bpxsz + i] = (byte)((b3 & (0xff << (i * 8))) >> (i * 8));
                                    }
                                    var res = BitConverter.ToUInt32(data, mult + x * bpxsz);
                                }
                                else if (_isInverted == true)
                                {
                                    var b2 = InvertColor(BitConverter.ToInt32(data, mult + x * bpxsz));
                                    for (int i = 0; i < frsz; i++)
                                    {
                                          data[mult + x * bpxsz + i] = (byte)((b2 & (0xff << (i * 8))) >> (i * 8));
                                    }                                    
                                }
                            }
                        }

                        System.Runtime.InteropServices.Marshal.Copy(data, 0, backgroundData.Scan0, data.Length);

                        mask.UnlockBits(maskData);
                        foreground.UnlockBits(foregroundData);
                        background.UnlockBits(backgroundData);

                        return background;
                    }
                }
            }
        }

        public GBitmap GetBitmap(GRect rect, int subsample, int align, GBitmap retval)
        {
            return GetBitmapList(rect, 1, 1, null);
        }

        public GBitmap GetBitmapList(GRect rect, int subsample, int align, List<int> components)
        {
            if (rect.Empty)
            {
                return new Graphics.Bitmap();
            }

            if (Info != null)
            {
                int width = Info.Width;
                int height = Info.Height;

                JB2Image fgJb2 = ForegroundJB2Image;

                if (
                  (width != 0)
                  && (height != 0)
                  && (fgJb2 != null)
                  && (fgJb2.Width == width)
                  && (fgJb2.Height == height))
                {
                    return fgJb2.GetBitmap(rect, subsample, align, 0, components);
                }
            }

            return null;
        }

        public Map GetMap(GRect segment, int subsample, GMap retval)
        {
            retval =
               IsColor
              ? (GMap)GetPixelMap(
                segment,
                subsample,
                0.0D,
                (retval is GPixmap)
                ? (GPixmap)retval
                : null)
              : (GMap)GetBitmap(
                segment,
                subsample,
                1,
                ((retval is GBitmap)
                ? (GBitmap)retval
                : null));

            return retval;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Converts the pixel data to a bitmap image
        /// </summary>
        /// <param name="pixels"></param>
        /// <returns></returns>
        private System.Drawing.Bitmap ConvertDataToImage(int[] pixels)
        {
            throw new NotImplementedException();
            /*// create a bitmap and manipulate it
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(Info.Width, Info.Height, PixelFormat.Format32bppArgb);
            BitmapData bits = bmp.LockBits(new System.Drawing.Rectangle(0, 0, Info.Width, Info.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);

            for (int y = 0; y < Info.Height; y++)
            {
                var row = (int*)((byte*)bits.Scan0 + (y * bits.Stride));

                for (int x = 0; x < Info.Width; x++)
                {
                    row[x] = pixels[y * Info.Width + x];
                }
            }

            bmp.UnlockBits(bits);

            return bmp;*/
        }

        private bool IsLegalBilevel()
        {
            if (Info == null)
            {
                return false;
            }

            int width = Info.Width;
            int height = Info.Height;

            if ((width <= 0) || (height <= 0))
            {
                return false;
            }

            JB2Image fgJb2 = ForegroundJB2Image;

            if ((fgJb2 == null) || (fgJb2.Width != width) || (fgJb2.Height != height))
            {
                return false;
            }

            return !(BackgroundIWPixelMap != null || ForegroundIWPixelMap != null
                     || ForegroundPalette != null);
        }

        private bool IsLegalCompound()
        {

            if (Info == null)
            {
                return false;
            }

            int width = Info.Width;
            int height = Info.Height;

            if ((width <= 0) || (height <= 0))
            {
                return false;
            }

            JB2Image fgJb2 = ForegroundJB2Image;

            if ((fgJb2 == null) || (fgJb2.Width != width) || (fgJb2.Height != height))
            {
                return false;
            }

            // There is no need to synchronize since we won't access data which could be updated.
            IWPixelMap bgIWPixmap = (IWPixelMap)BackgroundIWPixelMap;
            int bgred = 0;

            if (bgIWPixmap != null)
            {
                bgred =
                  ComputeRed(
                    width,
                    height,
                    bgIWPixmap.Width,
                    bgIWPixmap.Height);
            }

            if ((bgred < 1) || (bgred > 12))
            {
                return false;
            }

            int fgred = 0;

            if (ForegroundIWPixelMap != null)
            {
                GPixmap fgPixmap = ForegroundPixelMap;
                fgred = ComputeRed(
                    width,
                    height,
                    fgPixmap.ImageWidth,
                    fgPixmap.ImageHeight);
            }

            return ((fgred >= 1) && (fgred <= 12));
        }

        private bool Stencil(PixelMap pm, Graphics.Rectangle rect, int subsample, double gamma)
        {
            if (Info == null)
            {
                return false;
            }

            int width = Info.Width;
            int height = Info.Height;

            if ((width <= 0) || (height <= 0))
            {
                return false;
            }

            double gamma_correction = 1.0D;

            if (gamma > 0.0D)
            {
                gamma_correction = gamma / Info.Gamma;
            }

            if (gamma_correction < 0.10000000000000001D)
            {
                gamma_correction = 0.10000000000000001D;
            }
            else if (gamma_correction > 10D)
            {
                gamma_correction = 10D;
            }

            JB2Image fgJb2 = ForegroundJB2Image;

            if (fgJb2 != null)
            {
                ColorPalette fgPalette = ForegroundPalette;

                if (fgPalette != null)
                {
                    List<int> components = new List<int>();
                    GBitmap bm = GetBitmapList(rect, subsample, 1, components);

                    if (fgJb2.Blits.Count != fgPalette.BlitColors.Length)
                    {
                        pm.Attenuate(bm, 0, 0);

                        return false;
                    }

                    GPixmap colors =
                      new GPixmap().Init(
                        1,
                        fgPalette.PaletteColors.Length,
                        null);
                    GPixelReference color = colors.CreateGPixelReference(0);

                    for (int i = 0; i < colors.ImageWidth; color.IncOffset())
                    {
                        fgPalette.index_to_color(i++, color);
                    }

                    colors.ApplyGammaCorrection(gamma_correction);

                    List<int> compset = new List<int>();

                    while (components.Count > 0)
                    {
                        int lastx = 0;
                        int colorindex = fgPalette.BlitColors[((int)components[0])];
                        GRect comprect = new GRect();
                        compset = new List<int>();

                        for (int pos = 0; pos < components.Count;)
                        {
                            int blitno = ((int)components[pos]);
                            JB2Blit pblit = fgJb2.Blits[blitno];

                            if (pblit.Left < lastx)
                            {
                                break;
                            }

                            lastx = pblit.Left;

                            if (fgPalette.BlitColors[blitno] == colorindex)
                            {
                                JB2Shape pshape = fgJb2.GetShape(pblit.ShapeNumber);
                                GRect xrect =
                                  new GRect(
                                    pblit.Left,
                                    pblit.Bottom,
                                    pshape.Bitmap.ImageWidth,
                                    pshape.Bitmap.ImageHeight);
                                comprect.Recthull(comprect, xrect);
                                compset.Add(components[pos]);
                                components.Remove(pos);
                            }
                            else
                            {
                                pos++;
                            }
                        }

                        comprect.XMin /= subsample;
                        comprect.YMin /= subsample;
                        comprect.XMax = ((comprect.XMax + subsample) - 1) / subsample;
                        comprect.YMax = ((comprect.YMax + subsample) - 1) / subsample;
                        comprect.Intersect(comprect, rect);

                        if (comprect.Empty)
                        {
                            continue;
                        }

                        //        bm   = getBitmap(comprect, subsample, 1);
                        bm = new GBitmap();
                        bm.Init(
                          comprect.Height,
                          comprect.Width,
                          0);
                        bm.Grays = 1 + (subsample * subsample);

                        int rxmin = comprect.XMin * subsample;
                        int rymin = comprect.YMin * subsample;

                        for (int pos = 0; pos < compset.Count; ++pos)
                        {
                            int blitno = ((int)compset[pos]);
                            JB2Blit pblit = fgJb2.Blits[blitno];
                            JB2Shape pshape = fgJb2.GetShape(pblit.ShapeNumber);
                            bm.Blit(
                              pshape.Bitmap,
                              pblit.Left - rxmin,
                              pblit.Bottom - rymin,
                              subsample);
                        }

                        color.SetOffset(colorindex);
                        pm.Blit(
                          bm,
                          comprect.XMin - rect.XMin,
                          comprect.YMin - rect.YMin,
                          color);
                    }

                    return true;
                }

                // Three layer model.
                IWPixelMap fgIWPixmap = ForegroundIWPixelMap;

                if (fgIWPixmap != null)
                {
                    GBitmap bm = GetBitmap(rect, subsample, 1, null);

                    if ((bm != null) && (pm != null))
                    {
                        GPixmap fgPixmap = ForegroundPixelMap;
                        int w = fgPixmap.ImageWidth;
                        int h = fgPixmap.ImageHeight;
                        int red = ComputeRed(width, height, w, h);

                        //          if((red < 1) || (red > 12))
                        if ((red < 1) || (red > 16))
                        {
                            return false;
                        }
                        //
                        //          int supersample = (red <= subsample)
                        //            ? 1
                        //            : (red / subsample);
                        //          int wantedred = supersample * subsample;
                        //
                        //          if(red == wantedred)
                        //          {
                        //            pm.stencil(bm, fgPixmap, supersample, rect, gamma_correction);
                        //
                        //            return 1;
                        //          }
                        pm.Stencil(bm, fgPixmap, red, subsample, rect, gamma_correction);
                        return true;
                    }
                }
            }

            return false;
        }

        private int ComputeRed(int w, int h, int rw, int rh)
        {
            for (int red = 1; red < 16; red++)
            {
                if (((((w + red) - 1) / red) == rw) && ((((h + red) - 1) / red) == rh))
                {
                    return red;
                }
            }

            return 16;
        }

        private System.Drawing.Bitmap InvertImage(System.Drawing.Bitmap invertImage)
        {
            return null;

            //if (invertImage == null)
            //{
            //    return null;
            //}

            //var image = (System.Drawing.Bitmap)invertImage.Clone();

            //BitmapData imageData = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
            //                                      ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            //int height = image.Height;
            //int width = image.Width;

            //Parallel.For(
            //    0,
            //    height,
            //    y =>
            //    {
            //        uint* imageRow = (uint*)(imageData.Scan0 + (y * imageData.Stride));

            //        for (int x = 0; x < width; x++)
            //        {
            //            // Check if the mask byte is set
            //            imageRow[x] = InvertColor(imageRow[x]);
            //        }
            //    });

            //image.UnlockBits(imageData);

            //return image;
        }

        /// <summary>
        /// Inverts the color value
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private uint InvertColor(uint color)
        {
            return 0x00FFFFFFu ^ color;
        }

        /// <summary>
        /// Inverts the color value
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private int InvertColor(int color)
        {
            return 0x00FFFFFF ^ color;
        }

        /// <summary>
        /// Gets the pixel size for the pixel data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private int GetPixelSize(BitmapData data)
        {
            if (data.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                return 1;
            }

            if (data.PixelFormat == PixelFormat.Format16bppGrayScale || data.PixelFormat == PixelFormat.Format16bppRgb555 ||
                data.PixelFormat == PixelFormat.Format16bppRgb565 || data.PixelFormat == PixelFormat.Format16bppArgb1555)
            {
                return 2;
            }

            if (data.PixelFormat == PixelFormat.Format24bppRgb)
            {
                return 3;
            }

            if (data.PixelFormat == PixelFormat.Format32bppArgb || data.PixelFormat == PixelFormat.Format32bppArgb || data.PixelFormat == PixelFormat.Format32bppPArgb || data.PixelFormat == PixelFormat.Format32bppRgb)
            {
                return 4;
            }

            throw new Exception("Unknown image format: " + data.PixelFormat);
        }

        /// <summary>
        /// Gets the foreground image for the page
        /// </summary>
        /// <param name="resizeToPage"></param>
        /// <returns></returns>
        private System.Drawing.Bitmap GetForegroundImage(int subsample, bool resizeImage = false)
        {
            lock (_loadingLock)
            {
                var result = ForegroundIWPixelMap == null
                                 ? CreateBlankImage(Brushes.Black, Info.Width / subsample, Info.Height / subsample)
                                 : ForegroundIWPixelMap.GetPixmap().ToImage();

                return resizeImage == true ? ResizeImage(result, Info.Width / subsample, Info.Height / subsample) : result;
            }
        }

        private System.Drawing.Bitmap GetTextImage(int subsample, bool resizeImage = false)
        {
            if (ForegroundJB2Image == null)
            {
                return new System.Drawing.Bitmap(Info.Width / subsample, Info.Height / subsample);
            }

            lock (_loadingLock)
            {
                var result = ForegroundJB2Image.GetBitmap(subsample, 4).ToImage();

                return resizeImage == true ? ResizeImage(result, Info.Width / subsample, Info.Height / subsample) : result;
            }
        }

        /// <summary>
        /// Gets the background image for the page
        /// </summary>
        /// <returns></returns>
        private System.Drawing.Bitmap GetBackgroundImage(int subsample, bool resizeImage = false)
        {
            BG44Chunk[] backgrounds = PageForm.GetChildrenItems<BG44Chunk>();

            if (backgrounds == null || backgrounds.Count() == 0)
            {
                return CreateBlankImage(Brushes.White, Info.Width, Info.Height);
            }

            // Get the composite background image
            Wavelet.IWPixelMap backgroundMap = null;

            lock (_loadingLock)
            {
                foreach (var background in backgrounds)
                {
                    if (backgroundMap == null)
                    {
                        // Get the initial image
                        backgroundMap = background.BackgroundImage;
                    }
                    else
                    {
                        if (_isBackgroundDecoded == false)
                        {
                            background.ProgressiveDecodeBackground(backgroundMap);
                        }
                    }
                }

                _isBackgroundDecoded = true;
            }

            Bitmap result = backgroundMap.GetPixmap().ToImage();
            return resizeImage == true ? ResizeImage(result, Info.Width / subsample, Info.Height / subsample) : result;
        }

        /// <summary>
        /// Creates a blank image with the given color
        /// </summary>
        /// <param name="imageColor"></param>
        /// <returns></returns>
        private System.Drawing.Bitmap CreateBlankImage(Brush imageColor, int width, int height)
        {
            System.Drawing.Bitmap newBackground = new System.Drawing.Bitmap(Info.Width, Info.Height);

            // Fill the whole image with white
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(newBackground))
            {
                g.FillRegion(imageColor, new Region(new System.Drawing.Rectangle(0, 0, width, height)));
            }

            return newBackground;
        }

        #endregion Private Methods
    }
}