using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Tabula.Extractors
{
    public class BasicExtractionAlgorithm : ExtractionAlgorithm
    {
        private List<Ruling> verticalRulings = null;

        public BasicExtractionAlgorithm()
        {
        }

        public BasicExtractionAlgorithm(List<Ruling> verticalRulings)
        {
            this.verticalRulings = verticalRulings;
        }

        public List<Table> extract(PageArea page, List<float> verticalRulingPositions)
        {
            List<Ruling> verticalRulings = new List<Ruling>(verticalRulingPositions.Count);
            foreach (float p in verticalRulingPositions)
            {
                verticalRulings.Add(new Ruling(page.getHeight(), p, 0.0f, page.getHeight()));
            }
            this.verticalRulings = verticalRulings;
            return this.extract(page);
        }

        private class VerticalRulingComparer : IComparer<Ruling>
        {
            public int Compare([AllowNull] Ruling arg0, [AllowNull] Ruling arg1)
            {
                return arg0.getLeft().CompareTo(arg1.getLeft());
            }
        }

        private class TextChunkComparer : IComparer<TextChunk>
        {
            public int Compare([AllowNull] TextChunk o1, [AllowNull] TextChunk o2)
            {
                return o1.getLeft().CompareTo(o2.getLeft());
            }
        }

        public List<Table> extract(PageArea page)
        {
            List<TextElement> textElements = page.getText();

            if (textElements.Count == 0)
            {
                return new Table[] { Table.empty() }.ToList();
            }

            List<TextChunk> textChunks = this.verticalRulings == null ? TextElement.mergeWords(page.getText()) : TextElement.mergeWords(page.getText(), this.verticalRulings);
            List<TableLine> lines = TextChunk.groupByLines(textChunks);

            List<double> columns;
            if (this.verticalRulings != null)
            {
                this.verticalRulings.Sort(new VerticalRulingComparer());
                // need to filter/clip only for area

                columns = new List<double>(this.verticalRulings.Count);
                foreach (Ruling vr in this.verticalRulings)
                {
                    columns.Add(vr.getLeft());
                }
            }
            else
            {
                columns = columnPositions(lines);
            }
            columns = columns.Distinct().ToList(); // added by bobld: remove duplicates

            Table table = new Table(this);
            table.setRect(page.BoundingBox);

            for (int i = 0; i < lines.Count; i++)
            {
                TableLine line = lines[i];
                List<TextChunk> elements = line.getTextElements();

                elements.Sort(new TextChunkComparer());

                foreach (TextChunk tc in elements)
                {
                    if (tc.isSameChar(TableLine.WHITE_SPACE_CHARS))
                    {
                        continue;
                    }

                    int j = 0;
                    bool found = false;
                    for (; j < columns.Count; j++)
                    {
                        if (tc.getLeft() <= columns[j])
                        {
                            found = true;
                            break;
                        }
                    }

                    table.add(new Cell(tc), i, found ? j : columns.Count);
                }
            }

            return new Table[] { table }.ToList();
        }

        public override string ToString()
        {
            return "stream";
        }

        public static List<double> columnPositions(List<TableLine> lines)
        {
            List<TableRectangle> regions = new List<TableRectangle>();
            foreach (TextChunk tc in lines[0].getTextElements())
            {
                if (tc.isSameChar(TableLine.WHITE_SPACE_CHARS))
                {
                    continue;
                }
                TableRectangle r = new TableRectangle();
                r.setRect(tc);
                regions.Add(r);
            }

            foreach (TableLine l in lines.subList(1, lines.Count))
            {
                List<TextChunk> lineTextElements = new List<TextChunk>();
                foreach (TextChunk tc in l.getTextElements())
                {
                    if (!tc.isSameChar(TableLine.WHITE_SPACE_CHARS))
                    {
                        lineTextElements.Add(tc);
                    }
                }

                foreach (TableRectangle cr in regions)
                {
                    List<TextChunk> overlaps = new List<TextChunk>();
                    foreach (TextChunk te in lineTextElements)
                    {
                        if (cr.horizontallyOverlaps(te))
                        {
                            overlaps.Add(te);
                        }
                    }

                    foreach (TextChunk te in overlaps)
                    {
                        cr.merge(te);
                    }

                    //lineTextElements.removeAll(overlaps);
                    foreach (var rem in overlaps)
                    {
                        lineTextElements.Remove(rem);
                    }
                }

                foreach (TextChunk te in lineTextElements)
                {
                    TableRectangle r = new TableRectangle();
                    r.setRect(te);
                    regions.Add(r);
                }
            }

            List<double> rv = new List<double>();
            foreach (TableRectangle r in regions)
            {
                rv.Add(r.getRight());
            }

            rv.Sort(); //Collections.sort(rv);

            return rv;
        }
    }
}
