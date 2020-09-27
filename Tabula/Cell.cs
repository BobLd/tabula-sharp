using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig.Core;

namespace Tabula
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/Cell.java
    /// <summary>
    /// A cell in a table.
    /// </summary>
    public class Cell : RectangularTextContainer<TextChunk>
    {
        /// <summary>
        /// An empty Cell, with coordinates [0, 0, 0, 0].
        /// </summary>
        public static Cell EMPTY => new Cell(new PdfRectangle());

        /// <summary>
        /// Create a cell in a table.
        /// </summary>
        /// <param name="pdfRectangle"></param>
        public Cell(PdfRectangle pdfRectangle)
            : base(pdfRectangle)
        {
            this.SetPlaceholder(false);
            this.SetSpanning(false);
            this.SetTextElements(new List<TextChunk>());
        }

        /// <summary>
        /// Create a cell in a table.
        /// </summary>
        /// <param name="chunk"></param>
        public Cell(TextChunk chunk)
            : this(chunk.BoundingBox)
        {
            SetTextElements(new List<TextChunk>() { chunk });
        }

        /// <summary>
        /// Create a cell in a table.
        /// </summary>
        /// <param name="topLeft"></param>
        /// <param name="bottomRight"></param>
        public Cell(PdfPoint topLeft, PdfPoint bottomRight)
            : this (new PdfRectangle(topLeft.X, bottomRight.Y, bottomRight.X, topLeft.Y))
        {
            if (Math.Round(topLeft.X, 2) > Math.Round(bottomRight.X, 2))
            {
                throw new ArgumentException("Points order is wrong. topLeft.X should be < bottomRight.X.");
            }

            if (Math.Round(bottomRight.Y, 2) > Math.Round(topLeft.Y, 2))
            {
                throw new ArgumentException("Points order is wrong. bottomRight.Y should be < topLeft.Y.");
            }
        }

        /// <summary>
        /// Gets the cell's text.
        /// </summary>
        /// <param name="useLineReturns"></param>
        public override string GetText(bool useLineReturns)
        {
            if (base.textElements.Count == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            Utils.Sort(this.textElements, new ILL_DEFINED_ORDER());
            double curTop = this.textElements[0].Bottom;
            foreach (TextChunk tc in this.textElements)
            {
                if (useLineReturns && tc.Bottom < curTop) //.getTop() < curTop)
                {
                    sb.Append('\r');
                }
                sb.Append(tc.GetText());
                curTop = tc.Bottom;
            }
            return sb.ToString().Trim();
        }

        /// <summary>
        /// Gets the cell's text.
        /// </summary>
        public override string GetText()
        {
            return GetText(true);
        }

        public bool IsSpanning { get; private set; }

        public void SetSpanning(bool spanning)
        {
            this.IsSpanning = spanning;
        }

        public bool IsPlaceholder { get; private set; }

        public void SetPlaceholder(bool placeholder)
        {
            this.IsPlaceholder = placeholder;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return GetText();
        }
    }
}
