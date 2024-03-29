﻿using System;

namespace GifLib
{
    [Serializable]
    internal class GifParsingException : Exception
    {
        public GifParsingException()
        {
        }

        public GifParsingException(string message) : base(message)
        {
        }

        public GifParsingException(string message, Exception innerException) : base(message, innerException)
        {
        }


    }
}