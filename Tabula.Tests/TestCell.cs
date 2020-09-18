using System.Collections.Generic;
using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
    public class TestCell
	{
		[Fact]
		public void TestIsSpanning()
		{
			Cell cell = Cell.EMPTY;
			Assert.False(cell.IsSpanning);
			cell.SetSpanning(true);
			Assert.True(cell.IsSpanning);
		}

		[Fact]
		public void TestIsPlaceholder()
		{
			Cell cell = new Cell(new PdfRectangle());
			Assert.False(cell.IsPlaceholder);
			cell.SetPlaceholder(true);
			Assert.True(cell.IsPlaceholder);
		}

		[Fact]
		public void TestGetTextElements()
		{
			Cell cell = Cell.EMPTY;
            Assert.True(cell.TextElements.Count == 0);

            TextElement tElement = new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "testTabula4", 5, 0); //PDType1Font.HELVETICA_BOLD
            TextChunk tChunk = new TextChunk(tElement);
            List<TextChunk> tList = new List<TextChunk>();
            tList.Add(tChunk);
            cell.SetTextElements(tList);
            Assert.Equal("testTabula4", cell.TextElements[0].GetText());
        }

		[Fact]
		public void TestCellEmpty()
        {
			Cell cell = Cell.EMPTY;
			cell.SetPlaceholder(true);
			cell.SetTextElements(new List<TextChunk>() { new TextChunk(new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "not_empty", 5, 0)) });
			Assert.False(Cell.EMPTY.IsPlaceholder);
			Assert.NotEqual("not_empty", Cell.EMPTY.GetText());

			Cell.EMPTY.SetPlaceholder(true);
			Cell.EMPTY.SetTextElements(new List<TextChunk>() { new TextChunk(new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "not_empty", 5, 0)) });
			Assert.False(Cell.EMPTY.IsPlaceholder);
			Assert.NotEqual("not_empty", Cell.EMPTY.GetText());
		}
	}
}
