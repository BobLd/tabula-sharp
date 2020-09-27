using System;
using System.Collections.Generic;
using Tabula.Extractors;
using UglyToad.PdfPig.Core;

namespace Tabula
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/TableWithRulingLines.java
    /// <summary>
    /// A tabula table with ruling lines.
    /// </summary>
    public class TableWithRulingLines : Table
    {
        private class CellComparer : IComparer<Cell>
        {
            public int Compare(Cell arg0, Cell arg1)
            {
                return -arg0.Top.CompareTo(arg1.Top); // bobld multiply by -1 to sort from top to bottom (reading order)
            }
        }

        private readonly List<Ruling> verticalRulings;
        private readonly List<Ruling> horizontalRulings;
        private readonly RectangleSpatialIndex<Cell> si = new RectangleSpatialIndex<Cell>();

        /// <summary>
        /// Gets the vertical rulings.
        /// <para>This is a read-only list.</para>
        /// </summary>
        public IReadOnlyList<Ruling> VerticalRulings => verticalRulings;

        /// <summary>
        /// Gets the horizontal rulings.
        /// <para>This is a read-only list.</para>
        /// </summary>
        public IReadOnlyList<Ruling> HorizontalRulings => horizontalRulings;

        /// <summary>
        /// Create a table.
        /// </summary>
        /// <param name="area"></param>
        /// <param name="cells"></param>
        /// <param name="horizontalRulings"></param>
        /// <param name="verticalRulings"></param>
        /// <param name="extractionAlgorithm"></param>
        public TableWithRulingLines(TableRectangle area, List<Cell> cells, List<Ruling> horizontalRulings, List<Ruling> verticalRulings, IExtractionAlgorithm extractionAlgorithm)
            : base(extractionAlgorithm)
        {
            this.SetRect(area);
            this.verticalRulings = verticalRulings;
            this.horizontalRulings = horizontalRulings;
            this.AddCells(cells);
        }

        private void AddCells(List<Cell> cells)
        {
            if (cells.Count == 0)
            {
                return;
            }

            foreach (Cell ce in cells)
            {
                si.Add(ce);
            }

            List<List<Cell>> rowsOfCells = RowsOfCells(cells);
            var siBounds = si.GetBounds();

            for (int i = 0; i < rowsOfCells.Count; i++)
            {
                List<Cell> row = rowsOfCells[i];
                var rowCells = row.GetEnumerator();

                rowCells.MoveNext();
                Cell cell = rowCells.Current;

                // BobLd: careaful here!!
                List<List<Cell>> others = RowsOfCells(
                        si.Contains(
                                //new TableRectangle(cell.getBottom(), //top
                                //                   si.getBounds().getLeft(), // left
                                //                   cell.getLeft() - si.getBounds().getLeft(),//width
                                //                   si.getBounds().getBottom() - cell.getBottom()) // height

                                // BobLd: really not sure here
                                new PdfRectangle(siBounds.Left, siBounds.Bottom, cell.Left, cell.Bottom)
                                ));
                int startColumn = 0;
                foreach (List<Cell> r in others)
                {
                    startColumn = Math.Max(startColumn, r.Count);
                }

                this.Add(cell, i, startColumn++);
                while (rowCells.MoveNext())
                {
                    this.Add(rowCells.Current, i, startColumn++);
                }
            }
        }

        private static List<List<Cell>> RowsOfCells(List<Cell> cells)
        {
            Cell c;
            double lastTop;
            List<List<Cell>> rv = new List<List<Cell>>();
            List<Cell> lastRow;

            if (cells.Count == 0)
            {
                return rv;
            }

            Utils.Sort(cells, new CellComparer());

            var iter = cells.GetEnumerator();

            iter.MoveNext();
            c = iter.Current;

            lastTop = c.Top;
            lastRow = new List<Cell> { c };
            rv.Add(lastRow);

            while (iter.MoveNext())
            {
                c = iter.Current;
                if (!Utils.Feq(c.Top, lastTop))
                {
                    lastRow = new List<Cell>();
                    rv.Add(lastRow);
                }
                lastRow.Add(c);
                lastTop = c.Top;
            }
            return rv;
        }
    }
}
