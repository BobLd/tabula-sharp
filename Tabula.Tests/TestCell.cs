using System.Collections.Generic;
using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
    public class TestCell
    {
		[Fact]
		public void testIsSpanning()
		{
			Cell cell = Cell.EMPTY; //new Cell(0, 0, 0, 0);
			Assert.False(cell.isSpanning());
			cell.setSpanning(true);
			Assert.True(cell.isSpanning());
		}

		[Fact]
		public void testIsPlaceholder()
		{
			Cell cell = new Cell(new PdfRectangle()); // Cell(0, 0, 0, 0);
			Assert.False(cell.isPlaceholder());
			cell.setPlaceholder(true);
			Assert.True(cell.isPlaceholder());
		}

		[Fact]
		public void testGetTextElements()
		{
			Cell cell = Cell.EMPTY; // new Cell(0, 0, 0, 0);
			Assert.True(cell.getTextElements().Count == 0);

			TextElement tElement = new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "test", 5, 0); //PDType1Font.HELVETICA_BOLD
			TextChunk tChunk = new TextChunk(tElement);
			List<TextChunk> tList = new List<TextChunk>();
			tList.Add(tChunk);
			cell.setTextElements(tList);
			Assert.Equal("test", cell.getTextElements()[0].getText());
		}
	}
}
