using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Tabula.Tests
{
    public class TestTextElement
    {
		[Fact]
		public void createTextElement()
		{

			TextElement textElement = new TextElement(5f, 15f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f);

			Assert.NotNull(textElement);
			Assert.Equal("A", textElement.getText());
			Assert.Equal(1f, textElement.getFontSize(), 0);
			Assert.Equal(15f, textElement.getLeft(), 0);
			Assert.Equal(5f, textElement.getTop(), 0);
			Assert.Equal(10f, textElement.getWidth(), 0);
			Assert.Equal(20f, textElement.getHeight(), 0);
			Assert.Equal(UtilsForTesting.HELVETICA_BOLD, textElement.getFont());
			Assert.Equal(1f, textElement.getWidthOfSpace(), 0);
			Assert.Equal(0f, textElement.getDirection(), 0);


		}

		[Fact]
		public void createTextElementWithDirection()
		{

			TextElement textElement = new TextElement(5f, 15f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f);

			Assert.NotNull(textElement);
			Assert.Equal("A", textElement.getText());
			Assert.Equal(1f, textElement.getFontSize(), 0);
			Assert.Equal(15f, textElement.getLeft(), 0);
			Assert.Equal(5f, textElement.getTop(), 0);
			Assert.Equal(10f, textElement.getWidth(), 0);
			Assert.Equal(20f, textElement.getHeight(), 0);
			Assert.Equal(UtilsForTesting.HELVETICA_BOLD, textElement.getFont());
			Assert.Equal(1f, textElement.getWidthOfSpace(), 0);
			Assert.Equal(6f, textElement.getDirection(), 0);


		}

		[Fact]
		public void mergeFourElementsIntoFourWords()
		{

			List<TextElement> elements = new List<TextElement>();
			elements.Add(new TextElement(0f, 15f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f));
			elements.Add(new TextElement(20f, 15f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "B", 1f, 6f));
			elements.Add(new TextElement(40f, 15f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "C", 1f, 6f));
			elements.Add(new TextElement(60f, 15f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f));

			List<TextChunk> words = TextElement.mergeWords(elements);

			List<TextChunk> expectedWords = new List<TextChunk>();
			expectedWords.Add(new TextChunk(new TextElement(0f, 15f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f)));
			expectedWords.Add(new TextChunk(new TextElement(20f, 15f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "B", 1f, 6f)));
			expectedWords.Add(new TextChunk(new TextElement(40f, 15f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "C", 1f, 6f)));
			expectedWords.Add(new TextChunk(new TextElement(60f, 15f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f)));

			Assert.Equal(expectedWords, words);

		}

		[Fact]
		public void mergeFourElementsIntoOneWord()
		{

			List<TextElement> elements = new List<TextElement>();
			elements.Add(new TextElement(0f, 15f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f));
			elements.Add(new TextElement(0f, 25f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "B", 1f, 6f));
			elements.Add(new TextElement(0f, 35f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "C", 1f, 6f));
			elements.Add(new TextElement(0f, 45f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f));

			List<TextChunk> words = TextElement.mergeWords(elements);

			List<TextChunk> expectedWords = new List<TextChunk>();
			TextChunk textChunk = new TextChunk(new TextElement(0f, 15f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f));
			textChunk.add(new TextElement(0f, 25f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "B", 1f, 6f));
			textChunk.add(new TextElement(0f, 35f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "C", 1f, 6f));
			textChunk.add(new TextElement(0f, 45f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f));
			expectedWords.Add(textChunk);

			Assert.Equal(expectedWords, words);

		}

		[Fact]
		public void mergeElementsShouldBeIdempotent()
		{
			/*
		   * a bug in TextElement.merge_words would delete the first TextElement in the array
		   * it was called with. Discussion here: https://github.com/tabulapdf/tabula-java/issues/78
		   */

			List<TextElement> elements = new List<TextElement>();
			elements.Add(new TextElement(0f, 15f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f));
			elements.Add(new TextElement(0f, 25f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "B", 1f, 6f));
			elements.Add(new TextElement(0f, 35f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "C", 1f, 6f));
			elements.Add(new TextElement(0f, 45f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f));

			List<TextChunk> words = TextElement.mergeWords(elements);
			List<TextChunk> words2 = TextElement.mergeWords(elements);
			Assert.Equal(words, words2);
		}

		[Fact]
		public void mergeElementsWithSkippingRules()
		{

			List<TextElement> elements = new List<TextElement>();
			elements.Add(new TextElement(0f, 15f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f));
			elements.Add(new TextElement(0f, 17f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f));
			elements.Add(new TextElement(0f, 25f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "B", 1f, 6f));
			elements.Add(new TextElement(0.001f, 25f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, " ", 1f, 6f));
			elements.Add(new TextElement(0f, 35f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "C", 1f, 6f));
			elements.Add(new TextElement(0f, 45f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 10f, "D", 1f, 6f));

			List<TextChunk> words = TextElement.mergeWords(elements);

			List<TextChunk> expectedWords = new List<TextChunk>();
			TextChunk textChunk = new TextChunk(new TextElement(0f, 15f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f));
			textChunk.add(new TextElement(0f, 25f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "B", 1f, 6f));
			textChunk.add(new TextElement(0f, 35f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "C", 1f, 6f));
			textChunk.add(new TextElement(0f, 45f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 10f, "D", 1f, 6f));
			expectedWords.Add(textChunk);

			Assert.Equal(expectedWords, words);

		}

		[Fact]
		public void mergeTenElementsIntoTwoWords()
		{

			List<TextElement> elements = new List<TextElement>();
			elements.Add(new TextElement(0f, 0f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "H", 1f, 6f));
			elements.Add(new TextElement(0f, 10f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "O", 1f, 6f));
			elements.Add(new TextElement(0f, 20f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "L", 1f, 6f));
			elements.Add(new TextElement(0f, 30f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f));
			elements.Add(new TextElement(0f, 60f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "M", 1f, 6f));
			elements.Add(new TextElement(0f, 70f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "U", 1f, 6f));
			elements.Add(new TextElement(0f, 80f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "N", 1f, 6f));
			elements.Add(new TextElement(0f, 90f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f));
			elements.Add(new TextElement(0f, 100f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "O", 1f, 6f));

			List<TextChunk> words = TextElement.mergeWords(elements);

			List<TextChunk> expectedWords = new List<TextChunk>();
			TextChunk textChunk = new TextChunk(new TextElement(0f, 0f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "H", 1f, 6f));
			textChunk.add(new TextElement(0f, 10f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "O", 1f, 6f));
			textChunk.add(new TextElement(0f, 20f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "L", 1f, 6f));
			textChunk.add(new TextElement(0f, 30f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f));
			textChunk.add(new TextElement(0f, 30f, 10.5f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, " ", 1f)); //Check why width=10.5?
			expectedWords.Add(textChunk);
			TextChunk textChunk2 = new TextChunk(new TextElement(0f, 60f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "M", 1f, 6f));
			textChunk2.add(new TextElement(0f, 70f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "U", 1f, 6f));
			textChunk2.add(new TextElement(0f, 80f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "N", 1f, 6f));
			textChunk2.add(new TextElement(0f, 90f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f));
			textChunk2.add(new TextElement(0f, 100f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "O", 1f, 6f));
			expectedWords.Add(textChunk2);

			Assert.Equal(2, words.Count);
			Assert.Equal(expectedWords, words);

		}

		[Fact]
		public void mergeTenElementsIntoTwoLines()
		{

			List<TextElement> elements = new List<TextElement>();
			elements.Add(new TextElement(0f, 0f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "H", 1f, 6f));
			elements.Add(new TextElement(0f, 10f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "O", 1f, 6f));
			elements.Add(new TextElement(0f, 20f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "L", 1f, 6f));
			elements.Add(new TextElement(0f, 30f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f));
			elements.Add(new TextElement(20f, 0f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "M", 1f, 6f));
			elements.Add(new TextElement(20f, 10f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "U", 1f, 6f));
			elements.Add(new TextElement(20f, 20f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "N", 1f, 6f));
			elements.Add(new TextElement(20f, 30f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f));
			elements.Add(new TextElement(20f, 40f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "O", 1f, 6f));

			List<TextChunk> words = TextElement.mergeWords(elements);

			List<TextChunk> expectedWords = new List<TextChunk>();
			TextChunk textChunk = new TextChunk(new TextElement(0f, 0f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "H", 1f, 6f));
			textChunk.add(new TextElement(0f, 10f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "O", 1f, 6f));
			textChunk.add(new TextElement(0f, 20f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "L", 1f, 6f));
			textChunk.add(new TextElement(0f, 30f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "A", 1f, 6f));
			expectedWords.Add(textChunk);
			TextChunk textChunk2 = new TextChunk(new TextElement(20f, 0f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "M", 1f, 6f));
			textChunk2.add(new TextElement(20f, 10f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "U", 1f, 6f));
			textChunk2.add(new TextElement(20f, 20f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "N", 1f, 6f));
			textChunk2.add(new TextElement(20f, 30f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "D", 1f, 6f));
			textChunk2.add(new TextElement(20f, 40f, 10f, 20f, UtilsForTesting.HELVETICA_BOLD, 1f, "O", 1f, 6f));
			expectedWords.Add(textChunk2);

			Assert.Equal(2, words.Count);
			Assert.Equal(expectedWords, words);

		}
	}
}
