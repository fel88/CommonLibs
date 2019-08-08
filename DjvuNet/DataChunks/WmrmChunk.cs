// <copyright file="WmrmChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using DjvuNet.DataChunks.Enums;
using DjvuNet.JB2;

namespace DjvuNet.DataChunks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class WmrmChunk : IFFChunk
    {
        #region Private Variables

        private long _dataLocation = 0;

        #endregion Private Variables

        #region Public Properties

        #region ChunkType

        public override ChunkTypes ChunkType
        {
            get { return ChunkTypes.WMRM; }
        }

        #endregion ChunkType

        #region WatermarkImage

        private JB2Image _watermarkImage;

        /// <summary>
        /// Gets the image used to remove the watermark
        /// </summary>
        public JB2Image WatermarkImage
        {
            get
            {
                if (_watermarkImage == null)
                {
                    _watermarkImage = ReadCompressedWatermarkImage();
                }

                return _watermarkImage;
            }

            private set
            {
                if (WatermarkImage != value)
                {
                    _watermarkImage = value;
                }
            }
        }

        #endregion WatermarkImage

        #endregion Public Properties

        #region Constructors

        public WmrmChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document)
            : base(reader, parent, document)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override void ReadChunkData(DjvuReader reader)
        {
            _dataLocation = reader.Position;

            // Skip the data since it is delayed read
            reader.Position += Length;
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Reads the image which is used to remove the watermark
        /// </summary>
        /// <returns></returns>
        private JB2Image ReadCompressedWatermarkImage()
        {
            using (DjvuReader reader = Reader.CloneReader(_dataLocation, Length))
            {
                JB2Image image = new JB2Image();
                image.Decode(reader);

                return image;
            }
        }

        #endregion Private Methods
    }
}