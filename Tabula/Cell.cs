using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig.Core;

namespace Tabula
{
    /**
    * ** tabula/Cell.java **
    */
    public class Cell : RectangularTextContainer<TextChunk>
    {
        public static Cell EMPTY => new Cell(new PdfRectangle());

        private bool spanning;
        private bool placeholder;
        private List<TextChunk> textElements;

        public Cell(TextChunk chunk) : this(chunk.BoundingBox)
        {
            SetTextElements(new List<TextChunk>() { chunk });
        }

        public Cell(PdfRectangle pdfRectangle) : base(pdfRectangle)
        {
            this.SetPlaceholder(false);
            this.SetSpanning(false);
            this.SetTextElements(new List<TextChunk>());
        }

        [Obsolete("Use Cell(PdfPoint, PdfPoint) or Cell(PdfRectangle) instead.")]
        public Cell(double top, double left, double width, double height) : base(top, left, width, height)
        {
            this.SetPlaceholder(false);
            this.SetSpanning(false);
            this.SetTextElements(new List<TextChunk>());
            throw new ArgumentOutOfRangeException();
        }

        public Cell(PdfPoint topLeft, PdfPoint bottomRight)
            : this (new PdfRectangle(topLeft.X, bottomRight.Y, bottomRight.X, topLeft.Y))
        {
            this.SetPlaceholder(false);
            this.SetSpanning(false);
            this.SetTextElements(new List<TextChunk>());
            //throw new ArgumentOutOfRangeException();
        }

        public override string GetText(bool useLineReturns)
        {
            if (this.textElements.Count == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            Utils.Sort(this.textElements, new ILL_DEFINED_ORDER());
            double curTop = this.textElements[0].GetBottom();
            foreach (TextChunk tc in this.textElements)
            {
                if (useLineReturns && tc.GetBottom() < curTop) //.getTop() < curTop)
                {
                    sb.Append('\r');
                }
                sb.Append(tc.GetText());
                curTop = tc.GetBottom();
            }
            return sb.ToString().Trim();
        }

        public override string GetText()
        {
            return GetText(true);
        }

        public bool IsSpanning()
        {
            return spanning;
        }

        public void SetSpanning(bool spanning)
        {
            this.spanning = spanning;
        }

        public bool IsPlaceholder()
        {
            return placeholder;
        }

        public void SetPlaceholder(bool placeholder)
        {
            this.placeholder = placeholder;
        }

        public override List<TextChunk> GetTextElements()
        {
            return textElements;
        }

        public void SetTextElements(List<TextChunk> textElements)
        {
            this.textElements = textElements;
        }

        public override string ToString()
        {
            return GetText();
        }
    }
}
