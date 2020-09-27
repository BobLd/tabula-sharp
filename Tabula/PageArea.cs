using System;
using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

namespace Tabula
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/Page.java
    /// <summary>
    /// A tabula page.
    /// </summary>
    public class PageArea : TableRectangle
    {
        private readonly List<Ruling> rulings;
        private readonly List<TextElement> texts;
        private List<Ruling> cleanRulings;
        private List<Ruling> verticalRulingLines;
        private List<Ruling> horizontalRulingLines;
        private readonly RectangleSpatialIndex<TextElement> spatial_index;

        /// <summary>
        /// The page rotation.
        /// </summary>
        public int Rotation { get; }

        /// <summary>
        /// The page number.
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// The original page.
        /// </summary>
        public Page PdfPage { get; }

        /// <summary>
        /// The original document.
        /// </summary>
        public PdfDocument PdfDocument { get; }

        /// <summary>
        /// The minimum character width.
        /// </summary>
        public double MinCharWidth { get; }

        /// <summary>
        /// The minimum character height.
        /// </summary>
        public double MinCharHeight { get; }

        /// <summary>
        /// True if the page contains text.
        /// </summary>
        public bool HasText => this.texts.Count > 0;

        /// <summary>
        /// Gets the vertical rulings.
        /// <para>This is a read-only list. Use <see cref="AddRuling(Ruling)"/> to add a <see cref="Ruling"/>.</para>
        /// </summary>
        public IReadOnlyList<Ruling> VerticalRulings
        {
            get
            {
                if (this.verticalRulingLines != null)
                {
                    return this.verticalRulingLines;
                }
                this.GetRulings();
                return this.verticalRulingLines;
            }
        }

        /// <summary>
        /// Gets the horizontal rulings.
        /// <para>This is a read-only list. Use <see cref="AddRuling(Ruling)"/> to add a <see cref="Ruling"/>.</para>
        /// </summary>
        public IReadOnlyList<Ruling> HorizontalRulings
        {
            get
            {
                if (this.horizontalRulingLines != null)
                {
                    return this.horizontalRulingLines;
                }
                this.GetRulings();
                return this.horizontalRulingLines;
            }
        }

        /// <summary>
        /// Gets the unprocessed rulings.
        /// <para>This is a read-only list. Use <see cref="AddRuling(Ruling)"/> to add a <see cref="Ruling"/>.</para>
        /// </summary>
        public IReadOnlyList<Ruling> UnprocessedRulings => this.rulings;

        /// <summary>
        /// Create a new page area.
        /// </summary>
        /// <param name="area"></param>
        /// <param name="rotation"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pdPage"></param>
        /// <param name="doc"></param>
        /// <param name="characters"></param>
        /// <param name="rulings"></param>
        /// <param name="minCharWidth"></param>
        /// <param name="minCharHeight"></param>
        /// <param name="index"></param>
        public PageArea(PdfRectangle area, int rotation, int pageNumber, Page pdPage, PdfDocument doc,
             List<TextElement> characters, List<Ruling> rulings,
             double minCharWidth, double minCharHeight, RectangleSpatialIndex<TextElement> index) : base(area)
        {
            this.Rotation = rotation;
            this.PageNumber = pageNumber;
            this.PdfPage = pdPage;
            this.PdfDocument = doc;
            this.texts = characters;
            this.rulings = rulings;
            this.MinCharHeight = minCharHeight;
            this.MinCharWidth = minCharWidth;
            this.spatial_index = index;
        }

        /// <summary>
        /// Gets the page area from the given area.
        /// </summary>
        /// <param name="area"></param>
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
                                       Rotation,
                                       PageNumber,
                                       PdfPage,
                                       PdfDocument,
                                       t,
                                       Ruling.CropRulingsToArea(GetRulings(), area),
                                       min_char_width,
                                       min_char_height,
                                       spatial_index);

            rv.AddRuling(new Ruling(
                new PdfPoint(rv.Left, rv.Top),
                new PdfPoint(rv.Right, rv.Top)));

            rv.AddRuling(new Ruling(
                new PdfPoint(rv.Right, rv.Bottom),    // getTop
                new PdfPoint(rv.Right, rv.Top)));     // getBottom

            rv.AddRuling(new Ruling(
                new PdfPoint(rv.Right, rv.Bottom),
                new PdfPoint(rv.Left, rv.Bottom)));

            rv.AddRuling(new Ruling(
                new PdfPoint(rv.Left, rv.Bottom),
                new PdfPoint(rv.Left, rv.Top)));

            return rv;
        }

        /// <summary>
        /// Gets the page area from the given area.
        /// </summary>
        /// <param name="top"></param>
        /// <param name="left"></param>
        /// <param name="bottom"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public PageArea GetArea(double top, double left, double bottom, double right)
        {
            return this.GetArea(new PdfRectangle(left, bottom, right, top));
        }

        /// <summary>
        /// Gets the page's text.
        /// </summary>
        public List<TextElement> GetText()
        {
            return texts;
        }

        /// <summary>
        /// Gets the page's text contained in the area.
        /// </summary>
        /// <param name="area"></param>
        public List<TextElement> GetText(PdfRectangle area)
        {
            return this.spatial_index.Contains(area);
        }

        /// <summary>
        /// Gets the bounding box containing the text.
        /// </summary>
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

        /// <summary>
        /// Get the cleaned rulings.
        /// </summary>
        public IReadOnlyList<Ruling> GetRulings()
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

            Utils.SnapPoints(this.rulings, this.MinCharWidth, this.MinCharHeight);

            List<Ruling> vrs = new List<Ruling>();
            foreach (Ruling vr in this.rulings)
            {
                if (vr.IsVertical)
                {
                    vrs.Add(vr);
                }
            }
            this.verticalRulingLines = Ruling.CollapseOrientedRulings(vrs);

            List<Ruling> hrs = new List<Ruling>();
            foreach (Ruling hr in this.rulings)
            {
                if (hr.IsHorizontal)
                {
                    hrs.Add(hr);
                }
            }
            this.horizontalRulingLines = Ruling.CollapseOrientedRulings(hrs);

            this.cleanRulings = new List<Ruling>(this.verticalRulingLines);
            this.cleanRulings.AddRange(this.horizontalRulingLines);

            return this.cleanRulings;
        }

        /// <summary>
        /// Add a vertical or a horizontal ruling lines.
        /// </summary>
        /// <param name="r"></param>
        public void AddRuling(Ruling r)
        {
            if (r.IsOblique)
            {
                throw new InvalidOperationException("Can't add an oblique ruling");
            }

            this.rulings.Add(r);

            // clear caches
            this.verticalRulingLines = null;
            this.horizontalRulingLines = null;
            this.cleanRulings = null;
        }
    }
}
