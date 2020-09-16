using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Tabula.Extractors;
using UglyToad.PdfPig.Core;

namespace Tabula
{
    public class TableWithRulingLines : Table
    {
        private class CellComparator : IComparer<Cell>
        {
            public int Compare(Cell arg0, Cell arg1)
            {
                return -arg0.getTop().CompareTo(arg1.getTop()); // bobld multiply by -1 to sort from top to bottom (reading order)
            }
        }

        private readonly List<Ruling> verticalRulings;
        private readonly List<Ruling> horizontalRulings;
        readonly RectangleSpatialIndex<Cell> si = new RectangleSpatialIndex<Cell>();

        public TableWithRulingLines(TableRectangle area, List<Cell> cells, List<Ruling> horizontalRulings, List<Ruling> verticalRulings, ExtractionAlgorithm extractionAlgorithm)
            : base(extractionAlgorithm)
        {
            this.setRect(area);
            this.verticalRulings = verticalRulings;
            this.horizontalRulings = horizontalRulings;
            this.addCells(cells);
        }

        private void addCells(List<Cell> cells)
        {
            if (cells.Count == 0)
            {
                return;
            }

            foreach (Cell ce in cells)
            {
                si.add(ce);
            }

            List<List<Cell>> rowsOfCells = TableWithRulingLines.rowsOfCells(cells);
            for (int i = 0; i < rowsOfCells.Count; i++)
            {
                List<Cell> row = rowsOfCells[i];
                var rowCells = row.GetEnumerator();

                rowCells.MoveNext();
                Cell cell = rowCells.Current;

                // BobLd: careaful here!!
                List<List<Cell>> others = TableWithRulingLines.rowsOfCells(
                        si.contains(
                                //new TableRectangle(cell.getBottom(), //top
                                //                   si.getBounds().getLeft(), // left
                                //                   cell.getLeft() - si.getBounds().getLeft(),//width
                                //                   si.getBounds().getBottom() - cell.getBottom()) // height

                                // BobLd: really not sure here
                                new PdfRectangle(si.getBounds().getLeft(), si.getBounds().getBottom(), cell.getLeft(), cell.getBottom())
                                ));
                int startColumn = 0;
                foreach (List<Cell> r in others)
                {
                    startColumn = Math.Max(startColumn, r.Count);
                }

                this.add(cell, i, startColumn++);
                while (rowCells.MoveNext())
                {
                    this.add(rowCells.Current, i, startColumn++);
                }
            }
        }

        private static List<List<Cell>> rowsOfCells(List<Cell> cells)
        {
            Cell c;
            double lastTop;
            List<List<Cell>> rv = new List<List<Cell>>();
            List<Cell> lastRow;

            if (cells.Count == 0)
            {
                return rv;
            }

            Utils.sort(cells, new CellComparator());

            var iter = cells.GetEnumerator();

            iter.MoveNext();
            c = iter.Current;

            lastTop = c.getTop();
            lastRow = new List<Cell>();
            lastRow.Add(c);
            rv.Add(lastRow);

            while (iter.MoveNext())
            {
                c = iter.Current;
                if (!Utils.feq(c.getTop(), lastTop))
                {
                    lastRow = new List<Cell>();
                    rv.Add(lastRow);
                }
                lastRow.Add(c);
                lastTop = c.getTop();
            }
            return rv;
        }
    }
}
