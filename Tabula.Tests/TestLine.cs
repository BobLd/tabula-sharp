using System;
using System.Collections.Generic;
using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
	public class TestLine
	{
		[Fact]
		public void testSetTextElements()
		{
			TableLine line = new TableLine();

			TextElement tElement = new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "testTabula1", 5, 0);
			TextChunk tChunk = new TextChunk(tElement);
            List<TextChunk> tList = new List<TextChunk>
            {
                tChunk
            };
            line.setTextElements(tList);

			Assert.Equal("testTabula1", line.getTextElements()[0].getText());
		}

		[Fact]
		public void testAddTextChunkIntTextChunk()
		{
			TableLine line = new TableLine();

			TextElement tElement = new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "testTabula2", 5, 0);
			TextChunk tChunk = new TextChunk(tElement);
			line.addTextChunk(3, tChunk);
			Assert.Equal("testTabula2", line.getTextElements()[3].getText());
		}

		[Fact]
		public void testLessThanAddTextChunkIntTextChunk()
		{
			TableLine line = new TableLine();

			TextElement tElement = new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "test", 5, 0);
			TextChunk tChunk = new TextChunk(tElement);
			line.addTextChunk(0, tChunk);
			line.addTextChunk(0, tChunk);

			Assert.Equal("testtest", line.getTextElements()[0].getText());
		}

		[Fact]
		public void testErrorAddTextChunkIntTextChunk()
		{
            TableLine line = new TableLine();
            TextElement tElement = new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "testTabula3", 5, 0);
            TextChunk tChunk = new TextChunk(tElement);
            Assert.Throws<ArgumentException>(() => line.addTextChunk(-1, tChunk));
        }

		[Fact]
		public void testToString()
		{
			TableLine line = new TableLine();

			TextElement tElement = new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "test", 5, 0);
			TextChunk tChunk = new TextChunk(tElement);
			line.addTextChunk(0, tChunk);
			line.addTextChunk(0, tChunk);
			Assert.Equal("Tabula.TableLine[(x:0, y:0), 0, 0,bottom=0.00,right=0.00,chunks='testtest', ]", line.ToString());
			//Assert.Equal("technology.tabula.Line[x=0.0,y=0.0,w=0.0,h=0.0,bottom=0.000000,right=0.000000,chunks='testtest', ]", line.ToString());
		}
	}
}
