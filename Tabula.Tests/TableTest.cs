using Xunit;

namespace Tabula.Tests
{
    public class TableTest
    {
		[Fact]
		public void TestEmpty()
		{
			Table empty = Table.Empty();

			Assert.Equal(Cell.EMPTY, empty.GetCell(0, 0));
			Assert.Equal(Cell.EMPTY, empty.GetCell(1, 1));

			Assert.Equal(0, empty.GetRowCount());
			Assert.Equal(0, empty.GetColCount());

			Assert.Equal("", empty.GetExtractionMethod());

			Assert.Equal(0, empty.GetTop(), 0);
			Assert.Equal(0, empty.GetRight(), 0);
			Assert.Equal(0, empty.GetBottom(), 0);
			Assert.Equal(0, empty.GetLeft(), 0);

			Assert.Equal(0, empty.GetArea(), 0);
		}

		[Fact]
		public void TestRowColCounts()
		{
			Table table = Table.Empty();

			Assert.Equal(0, table.GetRowCount());
			Assert.Equal(0, table.GetColCount());

			table.Add(Cell.EMPTY, 0, 0);

			Assert.Equal(1, table.GetRowCount());
			Assert.Equal(1, table.GetColCount());

			table.Add(Cell.EMPTY, 9, 9);

			Assert.Equal(10, table.GetRowCount());
			Assert.Equal(10, table.GetColCount());
		}
	}
}
