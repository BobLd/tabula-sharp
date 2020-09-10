using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig.Core;

namespace Tabula
{
    // https://github.com/tabulapdf/tabula-java/blob/master/src/main/java/technology/tabula/Cell.java
    public class Cell : RectangularTextContainer<TextChunk>
    {
        public static Cell EMPTY = new Cell(new PdfRectangle());

        public Cell(TextChunk chunk) : this(chunk.BoundingBox)
        {
            setTextElements(new List<TextChunk>() { chunk });
        }

        public Cell(PdfRectangle pdfRectangle) : base(pdfRectangle)
        {
            this.setPlaceholder(false);
            this.setSpanning(false);
            this.setTextElements(new List<TextChunk>());
        }

        public Cell(double top, double left, double width, double height) : base(top, left, width, height)
        {
            //super(top, left, width, height);
            this.setPlaceholder(false);
            this.setSpanning(false);
            this.setTextElements(new List<TextChunk>());
            throw new ArgumentOutOfRangeException();
        }

        public Cell(PdfPoint topLeft, PdfPoint bottomRight)
            : base(topLeft.Y, topLeft.X, (bottomRight.X - topLeft.X), (bottomRight.Y - topLeft.Y))
        {
            //super((float)topLeft.getY(), (float)topLeft.getX(), (float)(bottomRight.getX() - topLeft.getX()), (float)(bottomRight.getY() - topLeft.getY()));
            this.setPlaceholder(false);
            this.setSpanning(false);
            this.setTextElements(new List<TextChunk>());
            throw new ArgumentOutOfRangeException();
        }

        private bool spanning;
        private bool placeholder;
        private List<TextChunk> textElements;

        public override string getText(bool useLineReturns)
        {
            if (this.textElements.Count == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            Utils.sort(this.textElements, new ILL_DEFINED_ORDER()); // this.textElements.Sort(new ILL_DEFINED_ORDER()); //Collections.sort(this.textElements, Rectangle.ILL_DEFINED_ORDER);
            double curTop = this.textElements[0].getBottom(); //.word.BoundingBox.Bottom; //.getTop();
            foreach (TextChunk tc in this.textElements)
            {
                if (useLineReturns && tc.getBottom()< curTop) //.getTop() < curTop)
                {
                    sb.Append('\r');
                }
                /*
                else
                {
                    sb.Append(' ');
                }
                */
                sb.Append(tc.getText());
                curTop = tc.getBottom(); //.word.BoundingBox.Bottom; //.getTop();
            }
            return sb.ToString().Trim();
        }

        public override string getText()
        {
            return getText(true);
        }

        public bool isSpanning()
        {
            return spanning;
        }

        public void setSpanning(bool spanning)
        {
            this.spanning = spanning;
        }

        public bool isPlaceholder()
        {
            return placeholder;
        }

        public void setPlaceholder(bool placeholder)
        {
            this.placeholder = placeholder;
        }

        public override List<TextChunk> getTextElements()
        {
            return textElements;
        }

        public void setTextElements(List<TextChunk> textElements)
        {
            this.textElements = textElements;
        }
    }
}
