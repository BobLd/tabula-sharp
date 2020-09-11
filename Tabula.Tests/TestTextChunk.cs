using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
	public class TestTextChunk
	{
		[Fact]
		public static void testCellEmpty()
		{
			TextChunk textChunk = TextChunk.EMPTY;
			textChunk.merge(new TextChunk(new TextElement(new PdfRectangle(0, 0, 10, 10), UtilsForTesting.HELVETICA_BOLD, 10, "not_empty", 5, 0)));
			Assert.NotEqual(new PdfRectangle(0, 0, 10, 10), TextChunk.EMPTY.BoundingBox);
			Assert.NotEqual("not_empty", TextChunk.EMPTY.getText());

			TextChunk.EMPTY.merge(new TextChunk(new TextElement(new PdfRectangle(0, 0, 10, 10), UtilsForTesting.HELVETICA_BOLD, 10, "not_empty", 5, 0)));
			Assert.NotEqual(new PdfRectangle(0, 0, 10, 10), TextChunk.EMPTY.BoundingBox);
			Assert.NotEqual("not_empty", TextChunk.EMPTY.getText());
		}
	}
}
