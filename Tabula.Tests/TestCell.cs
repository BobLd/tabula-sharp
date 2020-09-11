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
			Cell cell = Cell.EMPTY;
			Assert.False(cell.isSpanning());
			cell.setSpanning(true);
			Assert.True(cell.isSpanning());
		}

		[Fact]
		public void testIsPlaceholder()
		{
			Cell cell = new Cell(new PdfRectangle());
			Assert.False(cell.isPlaceholder());
			cell.setPlaceholder(true);
			Assert.True(cell.isPlaceholder());
		}

		[Fact]
		public void testGetTextElements()
		{
			Cell cell = Cell.EMPTY;
            Assert.True(cell.getTextElements().Count == 0);

            TextElement tElement = new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "testTabula4", 5, 0); //PDType1Font.HELVETICA_BOLD
            TextChunk tChunk = new TextChunk(tElement);
            List<TextChunk> tList = new List<TextChunk>();
            tList.Add(tChunk);
            cell.setTextElements(tList);
            Assert.Equal("testTabula4", cell.getTextElements()[0].getText());
        }

		[Fact]
		public static void testCellEmpty()
        {
			Cell cell = Cell.EMPTY;
			cell.setPlaceholder(true);
			cell.setTextElements(new List<TextChunk>() { new TextChunk(new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "not_empty", 5, 0)) });
			Assert.False(Cell.EMPTY.isPlaceholder());
			Assert.NotEqual("not_empty", Cell.EMPTY.getText());

			Cell.EMPTY.setPlaceholder(true);
			Cell.EMPTY.setTextElements(new List<TextChunk>() { new TextChunk(new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "not_empty", 5, 0)) });
			Assert.False(Cell.EMPTY.isPlaceholder());
			Assert.NotEqual("not_empty", Cell.EMPTY.getText());
		}
	}
}
