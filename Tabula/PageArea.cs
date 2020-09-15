using System;
using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Geometry;

namespace Tabula
{
    // https://github.com/tabulapdf/tabula-java/blob/master/src/main/java/technology/tabula/Page.java
    // TODO: this class should probably be called "PageArea" or something like that

    public class PageArea : TableRectangle
    {
        private int rotation;
        private int pageNumber;
        private List<TextElement> texts;
        private List<Ruling> rulings, cleanRulings = null, verticalRulingLines = null, horizontalRulingLines = null;
        private double minCharWidth;
        private double minCharHeight;
        private RectangleSpatialIndex<TextElement> spatial_index;
        private Page pdPage;
        private PdfDocument pdDoc;

        [Obsolete("Use PageArea(PdfRectangle, ...) instead.")]
        public PageArea(double top, double left, double width, double height, int rotation, int page_number, Page pdPage, PdfDocument doc)
            : base(top, left, width, height)
        {
            //super(top, left, width, height);
            this.rotation = rotation;
            this.pageNumber = page_number;
            this.pdPage = pdPage;
            this.pdDoc = doc;
        }

        [Obsolete("Use PageArea(PdfRectangle, ...) instead.")]
        public PageArea(double top, double left, double width, double height, int rotation, int page_number, Page pdPage, PdfDocument doc,
                    List<TextElement> characters, List<Ruling> rulings)
            : this(top, left, width, height, rotation, page_number, pdPage, doc)
        {
            //this(top, left, width, height, rotation, page_number, pdPage, doc);
            this.texts = characters;
            this.rulings = rulings;
        }

        [Obsolete("Use PageArea(PdfRectangle, ...) instead.")]
        public PageArea(double top, double left, double width, double height, int rotation, int page_number, Page pdPage, PdfDocument doc,
             List<TextElement> characters, List<Ruling> rulings,
             double minCharWidth, double minCharHeight, RectangleSpatialIndex<TextElement> index)
            : this(top, left, width, height, rotation, page_number, pdPage, doc, characters, rulings)
        {

            //this(top, left, width, height, rotation, page_number, pdPage, doc, characters, rulings);
            this.minCharHeight = minCharHeight;
            this.minCharWidth = minCharWidth;
            this.spatial_index = index;
        }

        public PageArea(PdfRectangle area, int rotation, int page_number, Page pdPage, PdfDocument doc,
             List<TextElement> characters, List<Ruling> rulings,
             double minCharWidth, double minCharHeight, RectangleSpatialIndex<TextElement> index) : base(area)
        {
            this.rotation = rotation;
            this.pageNumber = page_number;
            this.pdPage = pdPage;
            this.pdDoc = doc;
            this.texts = characters;
            this.rulings = rulings;
            this.minCharHeight = minCharHeight;
            this.minCharWidth = minCharWidth;
            this.spatial_index = index;
        }

        public PageArea getArea(PdfRectangle area)
        {
            List<TextElement> t = getText(area);
            double min_char_width = 7;
            double min_char_height = 7;

            if (t.Count > 0)
            {
                min_char_width = t.Min(x => x.width);
                min_char_height = t.Min(x => x.height);
            }

            PageArea rv = new PageArea(area,
                                       rotation,
                                       pageNumber,
                                       pdPage,
                                       pdDoc,
                                       t,
                                       Ruling.cropRulingsToArea(getRulings(), area),
                                       min_char_width,
                                       min_char_height,
                                       spatial_index);

            rv.addRuling(new Ruling(
                new PdfPoint(rv.getLeft(), rv.getTop()),
                new PdfPoint(rv.getRight(), rv.getTop())));

            rv.addRuling(new Ruling(
                new PdfPoint(rv.getRight(), rv.getBottom()),    // getTop
                new PdfPoint(rv.getRight(), rv.getTop())));     // getBottom

            rv.addRuling(new Ruling(
                new PdfPoint(rv.getRight(), rv.getBottom()),
                new PdfPoint(rv.getLeft(), rv.getBottom())));

            rv.addRuling(new Ruling(
                new PdfPoint(rv.getLeft(), rv.getBottom()),
                new PdfPoint(rv.getLeft(), rv.getTop())));

            return rv;
        }

        public PageArea getArea(double top, double left, double bottom, double right)
        {
            //TableRectangle area = new TableRectangle(top, left, right - left, bottom - top);
            PdfRectangle area = new PdfRectangle(left, bottom, right, top);
            var normzed = area.Normalise();
            return this.getArea(area);
        }

        public List<TextElement> getText()
        {
            return texts;
        }

        public List<TextElement> getText(PdfRectangle area)
        {
            return this.spatial_index.contains(area);
        }

        /*
        [Obsolete("use {@linkplain #getText(Rectangle)} instead")]
        public List<TextElement> getText(float top, float left, float bottom, float right)
        {
            return this.getText(new TableRectangle(top, left, right - left, bottom - top));
        }
        */

        public int getRotation()
        {
            return rotation;
        }

        public int getPageNumber()
        {
            return pageNumber;
        }

        [Obsolete("use getText() instead.")]
        public List<TextElement> getTexts()
        {
            return texts;
        }

        public TableRectangle getTextBounds()
        {
            List<TextElement> texts = this.getText();
            if (texts.Count > 0)
            {
                return Utils.bounds(texts);
            }
            else
            {
                return new TableRectangle();
            }
        }

        public List<Ruling> getRulings()
        {
            if (this.cleanRulings != null)
            {
                return this.cleanRulings;
            }

            if (this.rulings == null || this.rulings.Count == 0)
            {
                this.verticalRulingLines = new List<Ruling>();
                this.horizontalRulingLines = new List<Ruling>();
                return new List<Ruling>();
            }

            Utils.snapPoints(this.rulings, this.minCharWidth, this.minCharHeight);

            List<Ruling> vrs = new List<Ruling>();
            foreach (Ruling vr in this.rulings)
            {
                if (vr.vertical())
                {
                    vrs.Add(vr);
                }
            }
            this.verticalRulingLines = Ruling.collapseOrientedRulings(vrs);

            List<Ruling> hrs = new List<Ruling>();
            foreach (Ruling hr in this.rulings)
            {
                if (hr.horizontal())
                {
                    hrs.Add(hr);
                }
            }
            this.horizontalRulingLines = Ruling.collapseOrientedRulings(hrs);

            this.cleanRulings = new List<Ruling>(this.verticalRulingLines);
            this.cleanRulings.AddRange(this.horizontalRulingLines);

            return this.cleanRulings;
        }

        public List<Ruling> getVerticalRulings()
        {
            if (this.verticalRulingLines != null)
            {
                return this.verticalRulingLines;
            }
            this.getRulings();
            return this.verticalRulingLines;
        }

        public List<Ruling> getHorizontalRulings()
        {
            if (this.horizontalRulingLines != null)
            {
                return this.horizontalRulingLines;
            }
            this.getRulings();
            return this.horizontalRulingLines;
        }

        public void addRuling(Ruling r)
        {
            if (r.oblique())
            {
                throw new InvalidOperationException("Can't add an oblique ruling");
            }
            this.rulings.Add(r);
            // clear caches
            this.verticalRulingLines = null;
            this.horizontalRulingLines = null;
            this.cleanRulings = null;
        }

        public List<Ruling> getUnprocessedRulings()
        {
            return this.rulings;
        }

        [Obsolete("no replacement")]
        public double getMinCharWidth()
        {
            return minCharWidth;
        }

        [Obsolete("no replacement")]
        public double getMinCharHeight()
        {
            return minCharHeight;
        }

        public Page getPDPage()
        {
            return pdPage;
        }

        public PdfDocument getPDDoc()
        {
            return pdDoc;
        }

        [Obsolete("no replacement")]
        public RectangleSpatialIndex<TextElement> getSpatialIndex()
        {
            return this.spatial_index;
        }

        [Obsolete("no replacement")]
        public bool hasText()
        {
            return this.texts.Count > 0;
        }
    }
}
