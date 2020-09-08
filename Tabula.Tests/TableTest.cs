using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Tabula.Tests
{
    public class TableTest
    {
		[Fact]
		public void testEmpty()
		{
			Table empty = Table.empty();

			Assert.Equal(TextChunk.EMPTY, empty.getCell(0, 0));
			Assert.Equal(TextChunk.EMPTY, empty.getCell(1, 1));

			Assert.Equal(0, empty.getRowCount());
			Assert.Equal(0, empty.getColCount());

			Assert.Equal("", empty.getExtractionMethod());

			Assert.Equal(0, empty.getTop(), 0);
			Assert.Equal(0, empty.getRight(), 0);
			Assert.Equal(0, empty.getBottom(), 0);
			Assert.Equal(0, empty.getLeft(), 0);

			Assert.Equal(0, empty.getArea(), 0);
		}
		
		[Fact]
		public void testRowColCounts()
		{
			Table table = Table.empty();

			Assert.Equal(0, table.getRowCount());
			Assert.Equal(0, table.getColCount());

			table.add(Cell.EMPTY, 0, 0);

			Assert.Equal(1, table.getRowCount());
			Assert.Equal(1, table.getColCount());

			table.add(Cell.EMPTY, 9, 9);

			Assert.Equal(10, table.getRowCount());
			Assert.Equal(10, table.getColCount());
		}
	}
}
