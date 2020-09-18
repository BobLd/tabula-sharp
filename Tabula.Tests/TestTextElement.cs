using System.Collections.Generic;
using UglyToad.PdfPig.Core;
using Xunit;

namespace Tabula.Tests
{
    public class TestTextElement
    {
		[Fact]
		public void CreateTextElement()
		{
			TextElement textElement = new TextElement(new PdfRectangle(15, 5, 25, 25), UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 0); //5f, 15f, 10f, 20f, 
			Assert.Equal(10, textElement.Width);
			Assert.Equal(20, textElement.Height);

			Assert.NotNull(textElement);
			Assert.Equal("A", textElement.GetText());
			Assert.Equal(1f, textElement.FontSize, 0);
			Assert.Equal(15f, textElement.Left, 0);
			Assert.Equal(5f, textElement.Bottom, 0); // getTop()
			Assert.Equal(10f, textElement.Width, 0);
			Assert.Equal(20f, textElement.Height, 0);
			Assert.Equal(UtilsForTesting.HELVETICA_BOLD, textElement.Font);
			Assert.Equal(1f, textElement.WidthOfSpace, 0);
			Assert.Equal(0f, textElement.Direction, 0);
		}

		[Fact]
		public void CreateTextElementWithDirection()
		{
			TextElement textElement = new TextElement(new PdfRectangle(15, 5, 25, 25), UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f); // 5f, 15f, 10f, 20f,

			Assert.NotNull(textElement);
			Assert.Equal("A", textElement.GetText());
			Assert.Equal(1f, textElement.FontSize, 0);
			Assert.Equal(15f, textElement.Left, 0);
			Assert.Equal(5f, textElement.Bottom, 0); // .getTop()
			Assert.Equal(10f, textElement.Width, 0);
			Assert.Equal(20f, textElement.Height, 0);
			Assert.Equal(UtilsForTesting.HELVETICA_BOLD, textElement.Font);
			Assert.Equal(1f, textElement.WidthOfSpace, 0);
			Assert.Equal(6f, textElement.Direction, 0);
		}

		[Fact]
		public void MergeFourElementsIntoFourWords()
		{
            List<TextElement> elements = new List<TextElement>
            {
                new TextElement(new PdfRectangle(15, 0, 15 + 10, 0 + 20), UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f),   // 0f, 15f, 10f, 20f,
                new TextElement(new PdfRectangle(15, 20, 15 + 10, 20 + 20), UtilsForTesting.HELVETICA_BOLD, 1f, "B", 1f, 6f), // 20f, 15f, 10f, 20f,
                new TextElement(new PdfRectangle(15, 40, 15 + 10, 40 + 20), UtilsForTesting.HELVETICA_BOLD, 1f, "C", 1f, 6f), // 40f, 15f, 10f, 20f, 
                new TextElement(new PdfRectangle(15, 60, 15 + 10, 60 + 20), UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f) // 60f, 15f, 10f, 20f, 
            };

            List<TextChunk> words = TextElement.MergeWords(elements);

            List<TextChunk> expectedWords = new List<TextChunk>
            {
                new TextChunk(new TextElement(new PdfRectangle(15, 0, 15 + 10, 0 + 20), UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f)),   // 0f, 15f, 10f, 20f,
                new TextChunk(new TextElement(new PdfRectangle(15, 20, 15 + 10, 20 + 20), UtilsForTesting.HELVETICA_BOLD, 1f, "B", 1f, 6f)), // 20f, 15f, 10f, 20f,
                new TextChunk(new TextElement(new PdfRectangle(15, 40, 15 + 10, 40 + 20), UtilsForTesting.HELVETICA_BOLD, 1f, "C", 1f, 6f)), // 40f, 15f, 10f, 20f,
                new TextChunk(new TextElement(new PdfRectangle(15, 60, 15 + 10, 60 + 20), UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f)) // 60f, 15f, 10f, 20f
            };

            Assert.Equal(expectedWords, words);
		}

		[Fact]
		public void MergeFourElementsIntoOneWord()
		{
            List<TextElement> elements = new List<TextElement>
            {
                new TextElement(new PdfRectangle(15, 0, 15 + 10, 0 + 20), UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f), // 0f, 15f, 10f, 20f,
                new TextElement(new PdfRectangle(25, 0, 25 + 10, 0 + 20), UtilsForTesting.HELVETICA_BOLD, 1f, "B", 1f, 6f), // 0f, 25f, 10f, 20f, 
                new TextElement(new PdfRectangle(35, 0, 35 + 10, 0 + 20), UtilsForTesting.HELVETICA_BOLD, 1f, "C", 1f, 6f), // 0f, 35f, 10f, 20f,
                new TextElement(new PdfRectangle(45, 0, 45 + 10, 0 + 20), UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f) // 0f, 45f, 10f, 20f,
            };

            List<TextChunk> words = TextElement.MergeWords(elements);

			List<TextChunk> expectedWords = new List<TextChunk>();
			TextChunk textChunk = new TextChunk(new TextElement(new PdfRectangle(15, 0, 15 + 10, 0 + 20), UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f)); // 0f, 15f, 10f, 20f,
			textChunk.Add(new TextElement(new PdfRectangle(25, 0, 25 + 10, 0 + 20), UtilsForTesting.HELVETICA_BOLD, 1f, "B", 1f, 6f)); // 0f, 25f, 10f, 20f,
			textChunk.Add(new TextElement(new PdfRectangle(35, 0, 35 + 10, 0 + 20), UtilsForTesting.HELVETICA_BOLD, 1f, "C", 1f, 6f)); // 0f, 35f, 10f, 20f, 
			textChunk.Add(new TextElement(new PdfRectangle(45, 0, 45 + 10, 0 + 20), UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f)); // 0f, 45f, 10f, 20f,
			expectedWords.Add(textChunk);

			Assert.Equal(expectedWords, words);
		}

		[Fact]
		public void MergeElementsShouldBeIdempotent()
		{
            /*
			 * a bug in TextElement.merge_words would delete the first TextElement in the array
			 * it was called with. Discussion here: https://github.com/tabulapdf/tabula-java/issues/78
			 */

            List<TextElement> elements = new List<TextElement>
            {
                new TextElement(new PdfRectangle(15, 0, 25, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f), // 0f, 15f, 10f, 20f,
                new TextElement(new PdfRectangle(25, 0, 35, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "B", 1f, 6f), // 0f, 25f, 10f, 20f,
                new TextElement(new PdfRectangle(35, 0, 45, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "C", 1f, 6f), // 0f, 35f, 10f, 20f,
                new TextElement(new PdfRectangle(45, 0, 55, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f) // 0f, 45f, 10f, 20f,
            };

            List<TextChunk> words = TextElement.MergeWords(elements);
			List<TextChunk> words2 = TextElement.MergeWords(elements);
			Assert.Equal(words, words2);
		}

		[Fact]
		public void MergeElementsWithSkippingRules()
		{
            List<TextElement> elements = new List<TextElement>
            {
                new TextElement(new PdfRectangle(15, 0, 25, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f), // 0f, 15f, 10f, 20f,
                new TextElement(new PdfRectangle(17, 0, 27, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f), // 0f, 17f, 10f, 20f,
                new TextElement(new PdfRectangle(25, 0, 35, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "B", 1f, 6f), // 0f, 25f, 10f, 20f,
                new TextElement(new PdfRectangle(25, 0.001, 35, 20.001), UtilsForTesting.HELVETICA_BOLD, 1f, " ", 1f, 6f), // 0.001f, 25f, 10f, 20f,
                new TextElement(new PdfRectangle(35, 0, 45, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "C", 1f, 6f), // 0f, 35f, 10f, 20f,
                new TextElement(new PdfRectangle(45, 0, 55, 20), UtilsForTesting.HELVETICA_BOLD, 10f, "D", 1f, 6f) // 0f, 45f, 10f, 20f,
            };

            List<TextChunk> words = TextElement.MergeWords(elements);

			TextChunk textChunk = new TextChunk(new TextElement(new PdfRectangle(15, 0, 25, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f)); // 0f, 15f, 10f, 20f
			textChunk.Add(new TextElement(new PdfRectangle(25, 0, 35, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "B", 1f, 6f)); // 0f, 25f, 10f, 20f
			textChunk.Add(new TextElement(new PdfRectangle(35, 0, 45, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "C", 1f, 6f)); // 0f, 35f, 10f, 20f
			textChunk.Add(new TextElement(new PdfRectangle(45, 0, 55, 20), UtilsForTesting.HELVETICA_BOLD, 10f, "D", 1f, 6f)); // 0f, 45f, 10f, 20f
            List<TextChunk> expectedWords = new List<TextChunk>
            {
                textChunk
            };

            Assert.Equal(expectedWords, words);
		}

		[Fact]
		public void MergeTenElementsIntoTwoWords()
		{
			List<TextElement> elements = new List<TextElement>
			{
				new TextElement(new PdfRectangle(0, 0, 10, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "H", 1f, 6f), // 0f, 0f, 10f, 20f,
                new TextElement(new PdfRectangle(10, 0, 20 , 20), UtilsForTesting.HELVETICA_BOLD, 1f, "O", 1f, 6f), // 0f, 10f, 10f, 20f,
                new TextElement(new PdfRectangle(20, 0, 30, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "L", 1f, 6f), // 0f, 20f, 10f, 20f,
                new TextElement(new PdfRectangle(30, 0, 40, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f), // 0f, 30f, 10f, 20f,

				new TextElement(new PdfRectangle(60, 0, 70, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "M", 1f, 6f), // 0f, 60f, 10f, 20f,
                new TextElement(new PdfRectangle(70, 0, 80, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "U", 1f, 6f), // 0f, 70f, 10f, 20f,
                new TextElement(new PdfRectangle(80, 0, 90, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "N", 1f, 6f), // 0f, 80f, 10f, 20f,
                new TextElement(new PdfRectangle(90, 0, 100, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f), // 0f, 90f, 10f, 20f,
                new TextElement(new PdfRectangle(100, 0, 110, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "O", 1f, 6f) // 0f, 100f, 10f, 20f,
            };

			List<TextChunk> words = TextElement.MergeWords(elements);

			List<TextChunk> expectedWords = new List<TextChunk>();
			TextChunk textChunk = new TextChunk(new TextElement(new PdfRectangle(0, 0, 10, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "H", 1f, 6f)); // 0f, 10f, 10f, 20f,
			textChunk.Add(new TextElement(new PdfRectangle(10, 0, 20, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "O", 1f, 6f)); // 0f, 20f, 10f, 20f,
			textChunk.Add(new TextElement(new PdfRectangle(20, 0, 30, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "L", 1f, 6f)); // 0f, 30f, 10f, 20f,
			textChunk.Add(new TextElement(new PdfRectangle(30, 0, 40, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f)); // 0f, 30f, 10.5f, 20f, 
			textChunk.Add(new TextElement(new PdfRectangle(30, 0, 40.5, 20), UtilsForTesting.HELVETICA_BOLD, 1f, " ", 1f, 0)); //Check why width=10.5?  0f, 30f, 10.5f, 20f
			expectedWords.Add(textChunk);
			TextChunk textChunk2 = new TextChunk(new TextElement(new PdfRectangle(60, 0, 70, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "M", 1f, 6f)); // 0f, 60f, 10f, 20f, 
			textChunk2.Add(new TextElement(new PdfRectangle(70, 0, 80, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "U", 1f, 6f)); // 0f, 70f, 10f, 20f,
			textChunk2.Add(new TextElement(new PdfRectangle(80, 0, 90, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "N", 1f, 6f)); // 0f, 80f, 10f, 20f, 
			textChunk2.Add(new TextElement(new PdfRectangle(90, 0, 100, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f)); // 0f, 90f, 10f, 20f,
			textChunk2.Add(new TextElement(new PdfRectangle(100, 0, 110, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "O", 1f, 6f)); // 0f, 100f, 10f, 20f, 
			expectedWords.Add(textChunk2);

			Assert.Equal(2, words.Count);
			Assert.Equal(expectedWords, words);
		}

		[Fact]
		public void MergeTenElementsIntoTwoLines()
		{
			List<TextElement> elements = new List<TextElement>
			{
				new TextElement(new PdfRectangle(0, 0, 10, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "H", 1f, 6f), // 0f, 0f, 10f, 20f,
                new TextElement(new PdfRectangle(10, 0, 20 , 20), UtilsForTesting.HELVETICA_BOLD, 1f, "O", 1f, 6f), // 0f, 10f, 10f, 20f,
                new TextElement(new PdfRectangle(20, 0, 30, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "L", 1f, 6f), // 0f, 20f, 10f, 20f,
                new TextElement(new PdfRectangle(30, 0, 40, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f), // 0f, 30f, 10f, 20f,

                new TextElement(new PdfRectangle(0, 20, 10, 40), UtilsForTesting.HELVETICA_BOLD, 1f, "M", 1f, 6f), // 20f, 0f, 10f, 20f,
                new TextElement(new PdfRectangle(10, 20, 20, 40), UtilsForTesting.HELVETICA_BOLD, 1f, "U", 1f, 6f), // 20f, 10f, 10f, 20f, 
                new TextElement(new PdfRectangle(20, 20, 30, 40), UtilsForTesting.HELVETICA_BOLD, 1f, "N", 1f, 6f), // 20f, 20f, 10f, 20f,
                new TextElement(new PdfRectangle(30, 20, 40, 40), UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f), // 20f, 30f, 10f, 20f,
                new TextElement(new PdfRectangle(40, 20, 50, 40), UtilsForTesting.HELVETICA_BOLD, 1f, "O", 1f, 6f) // 20f, 40f, 10f, 20f,
            };

			List<TextChunk> words = TextElement.MergeWords(elements);

			List<TextChunk> expectedWords = new List<TextChunk>();
			TextChunk textChunk = new TextChunk(new TextElement(new PdfRectangle(0, 0, 10, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "H", 1f, 6f)); // 0f, 0f, 10f, 20f, 
			textChunk.Add(new TextElement(new PdfRectangle(10, 0, 20, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "O", 1f, 6f)); // 0f, 10f, 10f, 20f,
			textChunk.Add(new TextElement(new PdfRectangle(20, 0, 30, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "L", 1f, 6f)); // 0f, 20f, 10f, 20f,
			textChunk.Add(new TextElement(new PdfRectangle(30, 0, 40, 20), UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f)); // 0f, 30f, 10f, 20f,
			expectedWords.Add(textChunk);
			TextChunk textChunk2 = new TextChunk(new TextElement(new PdfRectangle(0, 20, 10, 40), UtilsForTesting.HELVETICA_BOLD, 1f, "M", 1f, 6f)); // 20f, 0f, 10f, 20f,
			textChunk2.Add(new TextElement(new PdfRectangle(10, 20, 20, 40), UtilsForTesting.HELVETICA_BOLD, 1f, "U", 1f, 6f)); // 20f, 10f, 10f, 20f,
			textChunk2.Add(new TextElement(new PdfRectangle(20, 20, 30, 40), UtilsForTesting.HELVETICA_BOLD, 1f, "N", 1f, 6f)); // 20f, 20f, 10f, 20f,
			textChunk2.Add(new TextElement(new PdfRectangle(30, 20, 40, 40), UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f)); // 20f, 30f, 10f, 20f,
			textChunk2.Add(new TextElement(new PdfRectangle(40, 20, 50, 40), UtilsForTesting.HELVETICA_BOLD, 1f, "O", 1f, 6f)); // 20f, 40f, 10f, 20f,
			expectedWords.Add(textChunk2);

			Assert.Equal(2, words.Count);
			Assert.Equal(expectedWords, words);
		}
	}
}
