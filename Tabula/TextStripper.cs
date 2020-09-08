using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Tabula
{
    public class TextStripper
    {
        public List<TextElement> textElements;
        public double minCharWidth;
        public double minCharHeight;
        public RectangleSpatialIndex<TextElement> spatialIndex;
        public int pageNumber;

        private PdfDocument document;

        public TextStripper(PdfDocument document, int pageNumber)
        {
            this.document = document;
            this.pageNumber = pageNumber;
        }

        public void process()
        {
            var page = document.GetPage(pageNumber);
            textElements = page.Letters.Select(l => new TextElement(l)).ToList();

            spatialIndex = new RectangleSpatialIndex<TextElement>();
            foreach (var te in textElements)
            {
                spatialIndex.add(te);
            }

            minCharWidth = page.Letters.Min(l => l.GlyphRectangle.Width);
            minCharHeight = page.Letters.Min(l => l.GlyphRectangle.Height);
        }
    }
}
