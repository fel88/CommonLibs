﻿// -----------------------------------------------------------------------
// <copyright file="Annotation.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace DjvuNet.DataChunks.Annotations
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Annotation
    {
        #region Public Properties

        #region Name

        private string _Name;

        /// <summary>
        /// Gets the name of the annotation which also could be an annotations value
        /// </summary>
        public string Name
        {
            get
            {
                return _Name;
            }

            private set
            {
                if (Name != value)
                {
                    _Name = value;
                }
            }
        }

        #endregion Name

        #region Parameters

        private Annotation[] _parameters;

        /// <summary>
        /// Gets the parameters for the annotation
        /// </summary>
        public Annotation[] Parameters
        {
            get { return _parameters; }

            private set
            {
                if (Parameters != value)
                {
                    _parameters = value;
                }
            }
        }

        #endregion Parameters

        #endregion Public Properties

        #region Constructors

        public Annotation(string text)
        {
            DecodeText(text);
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Breaks the text into annotation pieces
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string[] BreakIntoAnnotationPieces(string text)
        {
            List<string> pieces = new List<string>();

            int index = text.IndexOf('(');

            while (index != -1)
            {
                int end = FindAnnotationEnd(text, index);

                if (end != -1)
                {
                    pieces.Add(text.Substring(index, end - index));
                    index = end;
                }

                index = text.IndexOf('(', index);
            }

            return pieces.ToArray();
        }

        public override string ToString()
        {
            string parameterText = " " + string.Join(" ", Parameters.Select(x => x.ToString()));

            return string
                .Format("({0}{1})", Name, parameterText.Trim());
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Finds the end of the annotation which starts at the given offset
        /// </summary>
        /// <param name="text"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        private static int FindAnnotationEnd(string text, int start)
        {
            int parenCount = 0;

            for (int pos = start; pos < text.Length; pos++)
            {
                // Start of paren piece
                if (text[pos] == '(')
                {
                    parenCount++;
                }

                // End of paren piece
                else if (text[pos] == ')')
                {
                    parenCount--;
                }

                // Check if at end of piece
                if (parenCount == 0)
                {
                    return pos + 1;
                }
            }

            // Annotation end was not found
            return -1;
        }

        /// <summary>
        /// Decodes the text of the annotation
        /// </summary>
        /// <param name="text"></param>
        private void DecodeText(string text)
        {
            string[] parameters = BreakIntoParameterPieces(text);

            Name = parameters[0];
            _parameters = parameters
                .Skip(1)
                .Select(x => new Annotation(x))
                .ToArray();
        }

        /// <summary>
        /// Breaks the text into annotation pieces
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private string[] BreakIntoParameterPieces(string text)
        {
            // Skip the opening and closing parens
            text = text.Trim();

            if (text.StartsWith("(") == true)
            {
                text = text.Substring(1, text.Length - 2);
            }

            List<string> pieces = new List<string>();

            int start = -1;
            bool isInString = false;
            for (int pos = 0; pos < text.Length; pos++)
            {
                // Check for end of word or number
                if (Char.IsWhiteSpace(text[pos]) || pos == text.Length - 1)
                {
                    if (isInString == true && text[pos] == '"')
                    {
                        pieces.Add(text.Substring(start, pos - start + 1));
                    }
                    else if (start != -1 && isInString == false)
                    {
                        pieces.Add(text.Substring(start, pos - start));
                        start = -1;
                    }
                }
                else if (text[pos] == '"')
                {
                    if (isInString == true)
                    {
                        pieces.Add(text.Substring(start, pos - start + 1));
                        isInString = false;
                        start = -1;
                    }
                    else
                    {
                        start = pos;
                        isInString = true;
                    }
                }
                else if (text[pos] == '(')
                {
                    int end = FindAnnotationEnd(text, pos);
                    pieces.Add(text.Substring(pos, end - pos));

                    pos = end;
                }
                else if (start == -1)
                {
                    start = pos;
                }
            }

            return pieces.ToArray();
        }

        #endregion Private Methods
    }
}