using System;
using System.Collections.Generic;
using System.Linq;
using Tabula.Extractors;

namespace Tabula
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/Table.java
    /// <summary>
    /// A tabula table.
    /// </summary>
    public class Table : TableRectangle
    {
        /// <summary>
        /// An empty table.
        /// </summary>
        public static Table EMPTY => new Table("");

        /// <summary>
        /// Create a table.
        /// </summary>
        /// <param name="extractionMethod"></param>
        private Table(string extractionMethod) : base()
        {
            this.ExtractionMethod = extractionMethod;
        }

        /// <summary>
        /// Create a table.
        /// </summary>
        /// <param name="extractionAlgorithm"></param>
        public Table(IExtractionAlgorithm extractionAlgorithm)
            : this(extractionAlgorithm.ToString())
        { }

        //TreeMap<CellPosition, RectangularTextContainer> cells = new TreeMap<>();
        private SortedDictionary<CellPosition, Cell> cells = new SortedDictionary<CellPosition, Cell>();

        /// <summary>
        /// Get the list of cells.
        /// <para>This is a read-only list. Use <see cref="Add(RectangularTextContainer, int, int)"/> to add a <see cref="Cell"/>.</para>
        /// </summary>
        public IReadOnlyList<Cell> Cells => cells.Values.ToList();

        /// <summary>
        /// Gets the number of rows in the table.
        /// </summary>
        public int RowCount { get; private set; }

        /// <summary>
        /// Gets the number of columns in the table.
        /// </summary>
        public int ColumnCount { get; private set; }

        /// <summary>
        /// Gets the extraction method used to build to table.
        /// </summary>
        public string ExtractionMethod { get; }

        /// <summary>
        /// Add a cell at the given [row, column] position.
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public void Add(RectangularTextContainer chunk, int row, int col)
        {
            if (chunk is Cell cell)
            {
                this.Merge(cell);

                RowCount = Math.Max(RowCount, row + 1);
                ColumnCount = Math.Max(ColumnCount, col + 1);

                CellPosition cp = new CellPosition(row, col);

                if (cells.TryGetValue(cp, out var old))
                {
                    cell.Merge(old);
                }

                cells[cp] = cell;

                this.memoizedRows = null;
            }
            else
            {
                throw new ArgumentException("Cannot add a chunk that is not a Cell.", nameof(chunk));
            }
        }

        private List<List<Cell>> memoizedRows;

        /// <summary>
        /// Gets the table's rows.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<Cell>> Rows
        {
            get
            {
                if (this.memoizedRows == null)
                {
                    this.memoizedRows = ComputeRows();
                }
                return this.memoizedRows;
            }
        }

        private List<List<Cell>> ComputeRows()
        {
            List<List<Cell>> rows = new List<List<Cell>>();
            for (int i = 0; i < RowCount; i++)
            {
                List<Cell> lastRow = new List<Cell>();
                rows.Add(lastRow);
                for (int j = 0; j < ColumnCount; j++)
                {
                    lastRow.Add(this[i, j]);
                }
            }
            return rows;
        }

        /// <summary>
        /// Get the cell at position [i, j].
        /// </summary>
        /// <param name="i">Row.</param>
        /// <param name="j">Column.</param>
        public Cell this[int i, int j]
        {
            get
            {
                if (cells.TryGetValue(new CellPosition(i, j), out var cell))
                {
                    return cell;
                }
                return Cell.EMPTY;
            }
        }

        private class CellPosition : IComparable<CellPosition>
        {
            private readonly int row, col;

            public CellPosition(int row, int col)
            {
                this.row = row;
                this.col = col;
            }

            public int CompareTo(CellPosition other)
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
                if (obj == null) return false;
                if (obj is CellPosition other)
                {
                    return row == other.row && col == other.col;
                }
                return false;
            }
        }
    }
}
