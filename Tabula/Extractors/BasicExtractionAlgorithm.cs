using System;
using System.Collections.Generic;
using System.Linq;

namespace Tabula.Extractors
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/extractors/BasicExtractionAlgorithm.java
    /// <summary>
    /// Stream extraction algorithm.
    /// </summary>
    public class BasicExtractionAlgorithm : IExtractionAlgorithm
    {
        private IReadOnlyList<Ruling> verticalRulings;

        /// <summary>
        /// Stream extraction algorithm.
        /// </summary>
        public BasicExtractionAlgorithm()
        {
        }

        /// <summary>
        /// Stream extraction algorithm.
        /// </summary>
        /// <param name="verticalRulings">List of vertical rulings.</param>
        public BasicExtractionAlgorithm(IReadOnlyList<Ruling> verticalRulings)
        {
            this.verticalRulings = verticalRulings;
        }

        /// <summary>
        /// Extracts the tables in the page.
        /// </summary>
        /// <param name="page">The page where to extract the tables.</param>
        /// <param name="verticalRulingPositions">List of vertical rulings, indicated by there x position.</param>
        public List<Table> Extract(PageArea page, IReadOnlyList<float> verticalRulingPositions)
        {
            List<Ruling> verticalRulings = new List<Ruling>(verticalRulingPositions.Count);
            foreach (float p in verticalRulingPositions)
            {
                verticalRulings.Add(new Ruling(page.Height, p, 0.0f, page.Height)); // wrong here???
            }
            this.verticalRulings = verticalRulings;
            return this.Extract(page);
        }

        /// <summary>
        /// Extracts the tables in the page.
        /// </summary>
        /// <param name="page">The page where to extract the tables.</param>
        /// <param name="verticalRulings">List of vertical rulings.</param>
        public List<Table> Extract(PageArea page, IReadOnlyList<Ruling> verticalRulings)
        {
            foreach (var v in verticalRulings)
            {
                if (!v.IsVertical)
                {
                    throw new ArgumentException("BasicExtractionAlgorithm.Extract(): trying to use not vertical rulings", nameof(verticalRulings));
                }
            }
            this.verticalRulings = verticalRulings;
            return this.Extract(page);
        }

        /// <summary>
        /// Extracts the tables in the page.
        /// </summary>
        /// <param name="page">The page where to extract the tables.</param>
        public List<Table> Extract(PageArea page)
        {
            List<TextElement> textElements = page.GetText();

            if (textElements.Count == 0)
            {
                return new Table[] { Table.EMPTY }.ToList();
            }

            List<TextChunk> textChunks = this.verticalRulings == null ? TextElement.MergeWords(page.GetText()) : TextElement.MergeWords(page.GetText(), this.verticalRulings);
            List<TableLine> lines = TextChunk.GroupByLines(textChunks);

            List<double> columns;
            if (this.verticalRulings != null)
            {
                // added by bobld: clipping verticalRulings because testExtractColumnsCorrectly2() fails
                var clippedVerticalRulings = Ruling.CropRulingsToArea(this.verticalRulings, page.BoundingBox);
                clippedVerticalRulings.Sort(new VerticalRulingComparer());
                columns = new List<double>(clippedVerticalRulings.Count);
                foreach (Ruling vr in clippedVerticalRulings)
                {
                    columns.Add(vr.Left);
                }

                /*
                this.verticalRulings.Sort(new VerticalRulingComparer());
                columns = new List<double>(this.verticalRulings.Count);
                foreach (Ruling vr in this.verticalRulings)
                {
                    columns.Add(vr.getLeft());
                }
                */
            }
            else
            {
                columns = ColumnPositions(lines);
            }

            // added by bobld: remove duplicates because testExtractColumnsCorrectly2() fails, 
            // why do we need it here and not in the java version??
            columns = columns.Distinct().ToList();

            Table table = new Table(this);
            table.SetRect(page.BoundingBox);

            for (int i = 0; i < lines.Count; i++)
            {
                TableLine line = lines[i];
                List<TextChunk> elements = line.TextElements.ToList();

                elements.Sort(new TextChunkComparer());

                foreach (TextChunk tc in elements)
                {
                    if (tc.IsSameChar(TableLine.WHITE_SPACE_CHARS))
                    {
                        continue;
                    }

                    int j = 0;
                    bool found = false;
                    for (; j < columns.Count; j++)
                    {
                        if (tc.Left <= columns[j])
                        {
                            found = true;
                            break;
                        }
                    }

                    table.Add(new Cell(tc), i, found ? j : columns.Count);
                }
            }

            return new Table[] { table }.ToList();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "stream";
        }

        /// <summary>
        /// Gets columns positions.
        /// </summary>
        /// <param name="lines">Must be an array of lines sorted by their +top+ attribute.</param>
        /// <returns>a list of column boundaries (x axis).</returns>
        public static List<double> ColumnPositions(IReadOnlyList<TableLine> lines)
        {
            List<TableRectangle> regions = new List<TableRectangle>();
            foreach (TextChunk tc in lines[0].TextElements)
            {
                if (tc.IsSameChar(TableLine.WHITE_SPACE_CHARS))
                {
                    continue;
                }
                TableRectangle r = new TableRectangle();
                r.SetRect(tc);
                regions.Add(r);
            }

            foreach (TableLine l in lines.SubList(1, lines.Count))
            {
                List<TextChunk> lineTextElements = new List<TextChunk>();
                foreach (TextChunk tc in l.TextElements)
                {
                    if (!tc.IsSameChar(TableLine.WHITE_SPACE_CHARS))
                    {
                        lineTextElements.Add(tc);
                    }
                }

                foreach (TableRectangle cr in regions)
                {
                    List<TextChunk> overlaps = new List<TextChunk>();
                    foreach (TextChunk te in lineTextElements)
                    {
                        if (cr.HorizontallyOverlaps(te))
                        {
                            overlaps.Add(te);
                        }
                    }

                    foreach (TextChunk te in overlaps)
                    {
                        cr.Merge(te);
                    }

                    foreach (var rem in overlaps)
                    {
                        lineTextElements.Remove(rem);
                    }
                }

                // added by bobld
                // We need more checks here
                /*
                foreach (TextChunk te in lineTextElements)
                {
                    TableRectangle r = new TableRectangle();
                    r.setRect(te);
                    regions.Add(r);
                }
                */

                if (lineTextElements.Count > 0)
                {
                    // because testExtractColumnsCorrectly3() fails
                    // need to check here if the remaining te in lineTextElements do overlap among themselves
                    // might happen with multiline cell
                    TableRectangle r = new TableRectangle();
                    r.SetRect(lineTextElements[0]);
                    foreach (var rem in lineTextElements.SubList(1, lineTextElements.Count))
                    {
                        if (r.HorizontallyOverlaps(rem))
                        {
                            // they overlap!
                            // so this is multiline cell
                            r.Merge(rem);
                        }
                        else
                        {
                            regions.Add(r); // do not overlap (anymore), so add it 
                            r = new TableRectangle();
                            r.SetRect(rem);
                            //regions.Add(r);
                        }
                    }
                    regions.Add(r);
                }
                // end added
            }

            List<double> rv = new List<double>();
            foreach (TableRectangle r in regions)
            {
                rv.Add(r.Right);
            }

            rv.Sort(); //Collections.sort(rv);

            return rv;
        }

        #region Comparers
        private class VerticalRulingComparer : IComparer<Ruling>
        {
            public int Compare(Ruling arg0, Ruling arg1)
            {
                return arg0.Left.CompareTo(arg1.Left);
            }
        }

        private class TextChunkComparer : IComparer<TextChunk>
        {
            public int Compare(TextChunk o1, TextChunk o2)
            {
                return o1.Left.CompareTo(o2.Left);
            }
        }
        #endregion
    }
}
