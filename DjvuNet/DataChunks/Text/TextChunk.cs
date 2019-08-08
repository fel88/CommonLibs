﻿// <copyright file="TextChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using System.Text;

namespace DjvuNet.DataChunks.Text
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public abstract class TextChunk : IFFChunk
    {
        #region Private Variables

        private bool _isDecoded = false;
        private long _dataLocation = 0;

        #endregion Private Variables

        #region Public Properties

        #region TextLength

        private int _textLength;

        /// <summary>
        /// Gets the length of the text
        /// </summary>
        public int TextLength
        {
            get
            {
                DecodeIfNeeded();
                return _textLength;
            }

            private set
            {
                if (TextLength != value)
                {
                    _textLength = value;
                }
            }
        }

        #endregion TextLength

        #region Text

        private string _text;

        /// <summary>
        /// Gets the text for the chunk
        /// </summary>
        public string Text
        {
            get
            {
                if (_text == null)
                {
                    DecodeIfNeeded();
                }

                return _text;
            }

            private set
            {
                if (Text != value)
                {
                    _text = value;
                }
            }
        }

        #endregion Text

        #region Version

        private sbyte _version;

        /// <summary>
        /// Gets the version of the text chunk
        /// </summary>
        public sbyte Version
        {
            get
            {
                DecodeIfNeeded();
                return _version;
            }

            private set
            {
                if (Version != value)
                {
                    _version = value;
                }
            }
        }

        #endregion Version

        #region Zone

        private TextZone _zone;

        /// <summary>
        /// Gets the text zone for the chunk
        /// </summary>
        public TextZone Zone
        {
            get
            {
                DecodeIfNeeded();
                return _zone;
            }

            private set
            {
                if (Zone != value)
                {
                    _zone = value;
                }
            }
        }

        #endregion Zone

        #endregion Public Properties

        #region Constructors

        public TextChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document)
            : base(reader, parent, document)
        {
        }

        #endregion Constructors

        #region Protected Methods

        /// <summary>
        /// Gets the reader for the text data
        /// </summary>
        /// <returns></returns>
        protected abstract DjvuReader GetTextDataReader(long position);

        /// <summary>
        /// Read the chunk data
        /// </summary>
        /// <param name="reader"></param>
        protected override void ReadChunkData(DjvuReader reader)
        {
            // Save the current position for delayed decoding
            _dataLocation = reader.Position;

            // Advance the reader
            reader.Position += Length;
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Decodes the compressed data if needed
        /// </summary>
        private void DecodeIfNeeded()
        {
            if (_isDecoded == false)
            {
                ReadCompressedTextData();
            }
        }

        /// <summary>
        /// Reads the compressed text data
        /// </summary>
        private void ReadCompressedTextData()
        {
            if (Length == 0) return;

            using (DjvuReader reader = GetTextDataReader(_dataLocation))
            {
                _textLength = reader.ReadInt24MSB();
                byte[] textBytes = reader.ReadBytes(_textLength);
                _text = Encoding.UTF8.GetString(textBytes);
                _version = reader.ReadSByte();

                _zone = new TextZone(reader, null, null, this);
            }

            _isDecoded = true;
        }

        #endregion Private Methods
    }
}