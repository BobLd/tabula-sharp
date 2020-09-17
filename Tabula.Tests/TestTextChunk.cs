using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
    public class TestTextChunk
	{
		[Fact]
		public static void TestCellEmpty()
		{
			TextChunk textChunk = TextChunk.EMPTY;
			textChunk.Merge(new TextChunk(new TextElement(new PdfRectangle(0, 0, 10, 10), UtilsForTesting.HELVETICA_BOLD, 10, "not_empty", 5, 0)));
			Assert.NotEqual(new PdfRectangle(0, 0, 10, 10), TextChunk.EMPTY.BoundingBox);
			Assert.NotEqual("not_empty", TextChunk.EMPTY.GetText());

			TextChunk.EMPTY.Merge(new TextChunk(new TextElement(new PdfRectangle(0, 0, 10, 10), UtilsForTesting.HELVETICA_BOLD, 10, "not_empty", 5, 0)));
			Assert.NotEqual(new PdfRectangle(0, 0, 10, 10), TextChunk.EMPTY.BoundingBox);
			Assert.NotEqual("not_empty", TextChunk.EMPTY.GetText());
		}
	}
}
