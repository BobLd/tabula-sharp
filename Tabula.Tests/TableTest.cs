using Xunit;

namespace Tabula.Tests
{
    public class TableTest
    {
		[Fact]
		public void TestEmpty()
		{
			Table empty = Table.EMPTY;

			Assert.Equal(Cell.EMPTY, empty[0, 0]);
			Assert.Equal(Cell.EMPTY, empty[1, 1]);

			Assert.Equal(0, empty.RowCount);
			Assert.Equal(0, empty.ColumnCount);

			Assert.Equal("", empty.ExtractionMethod);

			Assert.Equal(0, empty.Top, 0);
			Assert.Equal(0, empty.Right, 0);
			Assert.Equal(0, empty.Bottom, 0);
			Assert.Equal(0, empty.Left, 0);

			Assert.Equal(0, empty.Area, 0);
		}

		[Fact]
		public void TestRowColCounts()
		{
			Table table = Table.EMPTY;

			Assert.Equal(0, table.RowCount);
			Assert.Equal(0, table.ColumnCount);

			table.Add(Cell.EMPTY, 0, 0);

			Assert.Equal(1, table.RowCount);
			Assert.Equal(1, table.ColumnCount);

			table.Add(Cell.EMPTY, 9, 9);

			Assert.Equal(10, table.RowCount);
			Assert.Equal(10, table.ColumnCount);
		}
	}
}
