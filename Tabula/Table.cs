using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Tabula.Extractors;

namespace Tabula
{
    public class Table : TableRectangle
    {
        public static Table empty() { return new Table(""); }

        private Table(string extractionMethod)
        {
            this.extractionMethod = extractionMethod;
        }

        public Table(ExtractionAlgorithm extractionAlgorithm)
            : this(extractionAlgorithm.ToString())
        { }

        private string extractionMethod;

        private int rowCount = 0;
        private int colCount = 0;

        /* visible for testing */
        //TreeMap<CellPosition, RectangularTextContainer> cells = new TreeMap<>();
        public SortedDictionary<CellPosition, Cell> cells = new SortedDictionary<CellPosition, Cell>();

        public int getRowCount() { return rowCount; }
        public int getColCount() { return colCount; }

        public string getExtractionMethod() { return extractionMethod; }

        public void add(RectangularTextContainer chunk, int row, int col)
        {
            if (chunk is Cell cell)
            {
                this.merge(cell);

                rowCount = Math.Max(rowCount, row + 1);
                colCount = Math.Max(colCount, col + 1);

                CellPosition cp = new CellPosition(row, col);

                //RectangularTextContainer old = cells[cp]; //.get(cp);
                //if (old != null) chunk.merge(old);
                if (cells.TryGetValue(cp, out var old))
                {
                    cell.merge(old);
                }

                cells[cp] = cell; //cells.put(cp, chunk);

                this.memoizedRows = null;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        private List<List<Cell>> memoizedRows = null;

        public List<List<Cell>> getRows()
        {
            if (this.memoizedRows == null) this.memoizedRows = computeRows();
            return this.memoizedRows;
        }

        private List<List<Cell>> computeRows()
        {
            List<List<Cell>> rows = new List<List<Cell>>();
            for (int i = 0; i < rowCount; i++)
            {
                List<Cell> lastRow = new List<Cell>();
                rows.Add(lastRow);
                for (int j = 0; j < colCount; j++)
                {
                    //RectangularTextContainer cell = cells.get(new CellPosition(i, j)); // JAVA_8 use getOrDefault()
                    //lastRow.add(cell != null ? cell : TextChunk.EMPTY);
                    if (cells.TryGetValue(new CellPosition(i, j), out var cell))
                    {
                        lastRow.Add(cell);
                    }
                    else
                    {
                        lastRow.Add(Cell.EMPTY);
                    }
                }
            }
            return rows;
        }

        public RectangularTextContainer getCell(int i, int j)
        {
            //RectangularTextContainer cell = cells[new CellPosition(i, j)]; // JAVA_8 use getOrDefault()
            //return cell != null ? cell : TextChunk.EMPTY;
            if (cells.TryGetValue(new CellPosition(i, j), out var cell))
            {
                return cell;
            }
            return TextChunk.EMPTY;
        }
    }

    public class CellPosition : IComparable<CellPosition>
    {
        public CellPosition(int row, int col)
        {
            this.row = row;
            this.col = col;
        }

        readonly int row, col;

        public int CompareTo([AllowNull] CellPosition other)
        {
            int rowdiff = row - other.row;
            return rowdiff != 0 ? rowdiff : col - other.col;
        }

        public override int GetHashCode()
        {
            return row + 101 * col;
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj == null) return false;
            if (GetType() != obj.GetType()) return false;
            CellPosition other = (CellPosition)obj;
            return row == other.row && col == other.col;
        }
    }
}
