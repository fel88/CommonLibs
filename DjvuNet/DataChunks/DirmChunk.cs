// <copyright file="DirmChunk.cs" company="">
// TODO: Update copyright text.
// </copyright>

using DjvuNet.Compression;
using DjvuNet.DataChunks.Directory;
using DjvuNet.DataChunks.Enums;

namespace DjvuNet.DataChunks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DirmChunk : IFFChunk
    {
        #region Private Variables

        private bool _isInitialized = false;
        private long _dataLocation = 0;
        private int _compressedSectionLength = 0;

        #endregion Private Variables

        #region Public Properties

        #region ChunkType

        public override ChunkTypes ChunkType
        {
            get { return ChunkTypes.Dirm; }
        }

        #endregion ChunkType

        #region IsBundled

        private bool _isBundled = false;

        /// <summary>
        /// True if the document is bundled, false otherwise
        /// </summary>
        //[DataMember]
        public bool IsBundled
        {
            get
            {
                return _isBundled;
            }

            private set
            {
                //if (ValidateIsBundled(value) == false) return;
                if (IsBundled != value)
                {
                    _isBundled = value;
                }
            }
        }

        #endregion IsBundled

        #region Version

        private int _version;

        /// <summary>
        /// Gets the version of the dirm information
        /// </summary>
        //[DataMember]
        public int Version
        {
            get
            {
                return _version;
            }

            private set
            {
                //if (ValidateVersion(value) == false) return;
                if (Version != value)
                {
                    _version = value;
                }
            }
        }

        #endregion Version

        #region Components

        private DirmComponent[] _components;

        /// <summary>
        /// Gets the dirm components
        /// </summary>
        public DirmComponent[] Components
        {
            get
            {
                if (_isInitialized == false)
                {
                    using (DjvuReader reader = Reader.CloneReader(_dataLocation, Length))
                    {
                        ReadCompressedData(reader, _components.Length, _compressedSectionLength);
                    }
                }

                return _components;
            }

            private set
            {
                if (Components != value)
                {
                    _components = value;
                }
            }
        }

        #endregion Components

        #endregion Public Properties

        #region Constructors

        public DirmChunk(DjvuReader reader, IFFChunk parent, DjvuDocument document)
            : base(reader, parent, document)
        {
        }

        #endregion Constructors

        #region Protected Methods

        protected override void ReadChunkData(DjvuReader reader)
        {
            sbyte flagByte = reader.ReadSByte();

            // B[7]
            IsBundled = (flagByte >> 7) == 1;

            // B[6..0]
            Version = flagByte & 127;

            int count = reader.ReadInt16MSB();

            ReadComponentData(reader, count);
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Reads the data for the components
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="count"></param>
        private void ReadComponentData(DjvuReader reader, int count)
        {
            List<DirmComponent> components = new List<DirmComponent>();

            // Read the offsets for the components
            for (int x = 0; x < count; x++)
            {
                int offset = reader.ReadInt32MSB();
                components.Add(new DirmComponent(offset));
            }

            _dataLocation = reader.Position;
            _isInitialized = false;
            _compressedSectionLength = (int)(Length - (reader.Position - Offset - 12));

            // Skip the bytes since this section is delayed read
            reader.Position += _compressedSectionLength;

            _components = components.ToArray();
        }

        /// <summary>
        /// Reads the compressed data from the djvu file
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="count"></param>
        /// <param name="compressedSectionLength"></param>
        private void ReadCompressedData(DjvuReader reader, int count, int compressedSectionLength)
        {
            DjvuReader decompressor = reader.GetBZZEncodedReader(compressedSectionLength);

            // Read the component sizes
            for (int x = 0; x < count; x++)
            {
                _components[x].Size = decompressor.ReadInt24MSB();
            }

            // Read the component flag information
            for (int x = 0; x < count; x++)
            {
                _components[x].DecodeFlags(decompressor.ReadSByte());
            }

            // Read the component strings
            for (int x = 0; x < count; x++)
            {
                _components[x].ID = decompressor.ReadNullTerminatedString();
                if (_components[x].HasName == true) _components[x].Name = decompressor.ReadNullTerminatedString();
                if (_components[x].HasTitle == true) _components[x].Title = decompressor.ReadNullTerminatedString();
            }

            _isInitialized = true;
        }

        #endregion Private Methods
    }
}