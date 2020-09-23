using System;
using System.Collections.Generic;
using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
    public class TestLine
	{
		[Fact]
		public void TestSetTextElements()
		{
			TableLine line = new TableLine();

			TextElement tElement = new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "testTabula1", 5, 0);
			TextChunk tChunk = new TextChunk(tElement);
            List<TextChunk> tList = new List<TextChunk>
            {
                tChunk
            };
            line.SetTextElements(tList);

			Assert.Equal("testTabula1", line.TextElements[0].GetText());
		}

		[Fact]
		public void TestAddTextChunkIntTextChunk()
		{
			TableLine line = new TableLine();

			TextElement tElement = new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "testTabula2", 5, 0);
			TextChunk tChunk = new TextChunk(tElement);
			line.AddTextChunk(3, tChunk);
			Assert.Equal("testTabula2", line.TextElements[3].GetText());
		}

		[Fact]
		public void TestLessThanAddTextChunkIntTextChunk()
		{
			TableLine line = new TableLine();

			TextElement tElement = new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "test", 5, 0);
			TextChunk tChunk = new TextChunk(tElement);
			line.AddTextChunk(0, tChunk);
			line.AddTextChunk(0, tChunk);

			Assert.Equal("testtest", line.TextElements[0].GetText());
		}

		[Fact]
		public void TestErrorAddTextChunkIntTextChunk()
		{
            TableLine line = new TableLine();
            TextElement tElement = new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "testTabula3", 5, 0);
            TextChunk tChunk = new TextChunk(tElement);
            Assert.Throws<ArgumentException>(() => line.AddTextChunk(-1, tChunk));
        }

		[Fact]
		public void TestToString()
		{
			TableLine line = new TableLine();

			TextElement tElement = new TextElement(new PdfRectangle(), UtilsForTesting.HELVETICA_BOLD, 10, "test", 5, 0);
			TextChunk tChunk = new TextChunk(tElement);
			line.AddTextChunk(0, tChunk);
			line.AddTextChunk(0, tChunk);
			Assert.Equal("Tabula.TableLine[left=0.00,bottom=0.00,right=0.00,top=0.00,chunks='testtest', ]", line.ToString());
			//Assert.Equal("technology.tabula.Line[x=0.0,y=0.0,w=0.0,h=0.0,bottom=0.000000,right=0.000000,chunks='testtest', ]", line.ToString());
		}
	}
}
