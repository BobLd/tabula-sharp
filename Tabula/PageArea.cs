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

        public PageArea GetArea(PdfRectangle area)
        {
            List<TextElement> t = GetText(area);
            double min_char_width = 7;
            double min_char_height = 7;

            if (t.Count > 0)
            {
                min_char_width = t.Min(x => x.Width);
                min_char_height = t.Min(x => x.Height);
            }

            PageArea rv = new PageArea(area,
                                       rotation,
                                       pageNumber,
                                       pdPage,
                                       pdDoc,
                                       t,
                                       Ruling.CropRulingsToArea(GetRulings(), area),
                                       min_char_width,
                                       min_char_height,
                                       spatial_index);

            rv.AddRuling(new Ruling(
                new PdfPoint(rv.GetLeft(), rv.GetTop()),
                new PdfPoint(rv.GetRight(), rv.GetTop())));

            rv.AddRuling(new Ruling(
                new PdfPoint(rv.GetRight(), rv.GetBottom()),    // getTop
                new PdfPoint(rv.GetRight(), rv.GetTop())));     // getBottom

            rv.AddRuling(new Ruling(
                new PdfPoint(rv.GetRight(), rv.GetBottom()),
                new PdfPoint(rv.GetLeft(), rv.GetBottom())));

            rv.AddRuling(new Ruling(
                new PdfPoint(rv.GetLeft(), rv.GetBottom()),
                new PdfPoint(rv.GetLeft(), rv.GetTop())));

            return rv;
        }

        public PageArea GetArea(double top, double left, double bottom, double right)
        {
            //TableRectangle area = new TableRectangle(top, left, right - left, bottom - top);
            PdfRectangle area = new PdfRectangle(left, bottom, right, top);
            var normzed = area.Normalise();
            return this.GetArea(area);
        }

        public List<TextElement> GetText()
        {
            return texts;
        }

        public List<TextElement> GetText(PdfRectangle area)
        {
            return this.spatial_index.Contains(area);
        }

        public int GetRotation()
        {
            return rotation;
        }

        public int GetPageNumber()
        {
            return pageNumber;
        }

        [Obsolete("use getText() instead.")]
        public List<TextElement> GetTexts()
        {
            return texts;
        }

        public TableRectangle GetTextBounds()
        {
            List<TextElement> texts = this.GetText();
            if (texts.Count > 0)
            {
                return Utils.Bounds(texts);
            }
            else
            {
                return new TableRectangle();
            }
        }

        public List<Ruling> GetRulings()
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

            Utils.SnapPoints(this.rulings, this.minCharWidth, this.minCharHeight);

            List<Ruling> vrs = new List<Ruling>();
            foreach (Ruling vr in this.rulings)
            {
                if (vr.Vertical())
                {
                    vrs.Add(vr);
                }
            }
            this.verticalRulingLines = Ruling.CollapseOrientedRulings(vrs);

            List<Ruling> hrs = new List<Ruling>();
            foreach (Ruling hr in this.rulings)
            {
                if (hr.Horizontal())
                {
                    hrs.Add(hr);
                }
            }
            this.horizontalRulingLines = Ruling.CollapseOrientedRulings(hrs);

            this.cleanRulings = new List<Ruling>(this.verticalRulingLines);
            this.cleanRulings.AddRange(this.horizontalRulingLines);

            return this.cleanRulings;
        }

        public List<Ruling> GetVerticalRulings()
        {
            if (this.verticalRulingLines != null)
            {
                return this.verticalRulingLines;
            }
            this.GetRulings();
            return this.verticalRulingLines;
        }

        public List<Ruling> GetHorizontalRulings()
        {
            if (this.horizontalRulingLines != null)
            {
                return this.horizontalRulingLines;
            }
            this.GetRulings();
            return this.horizontalRulingLines;
        }

        public void AddRuling(Ruling r)
        {
            if (r.Oblique())
            {
                throw new InvalidOperationException("Can't add an oblique ruling");
            }
            this.rulings.Add(r);
            // clear caches
            this.verticalRulingLines = null;
            this.horizontalRulingLines = null;
            this.cleanRulings = null;
        }

        public List<Ruling> GetUnprocessedRulings()
        {
            return this.rulings;
        }

        [Obsolete("no replacement")]
        public double GetMinCharWidth()
        {
            return minCharWidth;
        }

        [Obsolete("no replacement")]
        public double GetMinCharHeight()
        {
            return minCharHeight;
        }

        public Page GetPDPage()
        {
            return pdPage;
        }

        public PdfDocument GetPDDoc()
        {
            return pdDoc;
        }

        [Obsolete("no replacement")]
        public RectangleSpatialIndex<TextElement> GetSpatialIndex()
        {
            return this.spatial_index;
        }

        [Obsolete("no replacement")]
        public bool HasText()
        {
            return this.texts.Count > 0;
        }
    }
}
