using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.PdfFonts;

namespace Tabula
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/TextElement.java
    /// <summary>
    /// A tabula, text element. Equivalent to a letter.
    /// </summary>
    public class TextElement : TableRectangle, IHasText
    {
        internal Letter letter;

        private string text;
        private static double AVERAGE_CHAR_TOLERANCE = 0.3;

        /// <summary>
        /// Create a text element.
        /// </summary>
        /// <param name="pdfRectangle"></param>
        /// <param name="font"></param>
        /// <param name="fontSize"></param>
        /// <param name="c"></param>
        /// <param name="widthOfSpace"></param>
        /// <param name="dir">The direction of the text (0, 90, 180, or 270). Can be any number with PdfPig.</param>
        public TextElement(PdfRectangle pdfRectangle, FontDetails font, double fontSize, string c, double widthOfSpace, double dir)
            : base(pdfRectangle)
        {
            this.text = c;
            this.WidthOfSpace = widthOfSpace;
            this.FontSize = fontSize;
            this.Font = font;
            this.Direction = dir;
        }
        public string GetText() => text;

        /// <summary>
        /// The direction of the text (0, 90, 180, or 270). Can be any number with PdfPig.
        /// </summary>
        public double Direction { get; }

        public double WidthOfSpace { get; }

        public FontDetails Font { get; }

        public double FontSize { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string s = base.ToString();
            sb.Append(s, 0, s.Length - 1);
            sb.Append(",text=").Append(this.GetText()).Append(']');
            return sb.ToString();
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();

            result = prime * result + BitConverter.ToInt32(BitConverter.GetBytes(Direction), 0);
            result = prime * result + ((Font?.GetHashCode()) ?? 0);
            result = prime * result + BitConverter.ToInt32(BitConverter.GetBytes(FontSize), 0);
            result = prime * result + ((text?.GetHashCode()) ?? 0);
            result = prime * result + BitConverter.ToInt32(BitConverter.GetBytes(WidthOfSpace), 0);
            return result;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is TextElement other)
            {
                if (BitConverter.ToInt32(BitConverter.GetBytes(Direction), 0) != BitConverter.ToInt32(BitConverter.GetBytes(other.Direction), 0))
                {
                    return false;
                }

                if (Font == null)
                {
                    if (other.Font != null) return false;
                }
                else if (!Font.Equals(other.Font))
                {
                    return false;
                }

                if (BitConverter.ToInt32(BitConverter.GetBytes(FontSize), 0) != BitConverter.ToInt32(BitConverter.GetBytes(other.FontSize), 0))
                {
                    return false;
                }

                if (text == null)
                {
                    if (other.text != null) return false;
                }
                else if (!text.Equals(other.text))
                {
                    return false;
                }

                return BitConverter.ToInt32(BitConverter.GetBytes(WidthOfSpace), 0) != BitConverter.ToInt32(BitConverter.GetBytes(other.WidthOfSpace), 0);
            }
            return false;
        }

        public static List<TextChunk> MergeWords(List<TextElement> textElements)
        {
            return MergeWords(textElements, new List<Ruling>());
        }

        /// <summary>
        /// heuristically merge a list of TextElement into a list of TextChunk
        /// ported from PDFBox's PDFTextStripper.writePage, with modifications.
        /// Here be dragons
        /// </summary>
        /// <param name="textElements"></param>
        /// <param name="verticalRulings"></param>
        public static List<TextChunk> MergeWords(IReadOnlyList<TextElement> textElements, IReadOnlyList<Ruling> verticalRulings)
        {
            List<TextChunk> textChunks = new List<TextChunk>();

            if (textElements.Count == 0)
            {
                return textChunks;
            }

            textChunks.Add(new TextChunk(textElements[0]));

            TextChunk firstTC = textChunks[0];

            double previousAveCharWidth = firstTC.Width;
            double endOfLastTextX = firstTC.Right;
            double maxYForLine = firstTC.Top; //.getBottom();
            double maxHeightForLine = firstTC.Height;
            double minYTopForLine = firstTC.Bottom;//.getTop();
            double lastWordSpacing = -1;
            double wordSpacing, deltaSpace, averageCharWidth, deltaCharWidth;
            double expectedStartOfNextWordX, dist;
            TextElement sp, prevChar;
            TextChunk currentChunk;
            bool sameLine, acrossVerticalRuling;

            foreach (TextElement chr in textElements.Skip(1))
            {
                currentChunk = textChunks[textChunks.Count - 1];
                prevChar = currentChunk.TextElements[currentChunk.TextElements.Count - 1];

                // if same char AND overlapped, skip
                if (chr.GetText().Equals(prevChar.GetText()) && (prevChar.OverlapRatio(chr) > 0.5))
                {
                    continue;
                }

                // if chr is a space that overlaps with prevChar, skip
                if (chr.GetText().Equals(" ") && Utils.Feq(prevChar.Left, chr.Left) && Utils.Feq(prevChar.Bottom, chr.Bottom)) // getTop() getTop()
                {
                    continue;
                }

                // Resets the average character width when we see a change in font
                // or a change in the font size
                if ((chr.Font != prevChar.Font) || !Utils.Feq(chr.FontSize, prevChar.FontSize))
                {
                    previousAveCharWidth = -1;
                }

                // is there any vertical ruling that goes across chr and prevChar?
                acrossVerticalRuling = false;
                foreach (Ruling r in verticalRulings)
                {
                    if (VerticallyOverlapsRuling(prevChar, r) && VerticallyOverlapsRuling(chr, r) && prevChar.X < r.Position && chr.X > r.Position ||
                        (prevChar.X > r.Position && chr.X < r.Position))
                    {
                        acrossVerticalRuling = true;
                        break;
                    }
                }

                // Estimate the expected width of the space based on the
                // space character with some margin.
                wordSpacing = chr.WidthOfSpace;
                deltaSpace = 0;
                if (double.IsNaN(wordSpacing) || wordSpacing == 0)
                {
                    deltaSpace = double.MaxValue;
                }
                else if (lastWordSpacing < 0)
                {
                    deltaSpace = wordSpacing * 0.5f; // 0.5 == spacing tolerance
                }
                else
                {
                    deltaSpace = ((wordSpacing + lastWordSpacing) / 2.0f) * 0.5f;
                }

                // Estimate the expected width of the space based on the
                // average character width with some margin. This calculation does not
                // make a true average (average of averages) but we found that it gave the
                // best results after numerous experiments. Based on experiments we also found that
                // .3 worked well.
                if (previousAveCharWidth < 0)
                {
                    averageCharWidth = chr.Width / chr.GetText().Length;
                }
                else
                {
                    averageCharWidth = (previousAveCharWidth + (chr.Width / chr.GetText().Length)) / 2.0f;
                }
                deltaCharWidth = averageCharWidth * AVERAGE_CHAR_TOLERANCE;

                // Compares the values obtained by the average method and the wordSpacing method and picks
                // the smaller number.
                expectedStartOfNextWordX = -double.MaxValue;

                if (endOfLastTextX != -1)
                {
                    expectedStartOfNextWordX = endOfLastTextX + Math.Min(deltaCharWidth, deltaSpace);
                }

                // new line?
                sameLine = true;
                if (!Utils.Overlap(chr.Top, chr.Height, maxYForLine, maxHeightForLine)) // getBottom()
                {
                    endOfLastTextX = -1;
                    expectedStartOfNextWordX = -double.MaxValue;
                    maxYForLine = -double.MaxValue;
                    maxHeightForLine = -1;
                    minYTopForLine = double.MaxValue;
                    sameLine = false;
                }

                endOfLastTextX = chr.Right;

                // should we add a space?
                if (!acrossVerticalRuling && sameLine && expectedStartOfNextWordX < chr.Left && !prevChar.GetText().EndsWith(" "))
                {
                    sp = new TextElement(
                        new PdfRectangle(prevChar.BoundingBox.BottomLeft, new PdfPoint(expectedStartOfNextWordX, prevChar.BoundingBox.TopRight.Y)),
                            prevChar.Font,
                            prevChar.FontSize,
                            " ",
                            prevChar.WidthOfSpace, 0);

                    currentChunk.Add(sp);
                }
                else
                {
                    sp = null;
                }

                maxYForLine = Math.Max(chr.Top, maxYForLine); // getBottom()
                maxHeightForLine = Math.Max(maxHeightForLine, chr.Height);
                minYTopForLine = Math.Min(minYTopForLine, chr.Bottom); // .getTop()

                dist = chr.Left - (sp != null ? sp.Right : prevChar.Right);

                // added by BobLd
                // handle cases where order of character is not good, implement quicksort???
                if (dist < -wordSpacing)
                {
                    dist = double.MaxValue; // force create new word because testColumnRecognition() fails
                }
                // end added

                if (!acrossVerticalRuling && sameLine && (dist < 0 ? currentChunk.VerticallyOverlaps(chr) : dist < wordSpacing))
                {
                    currentChunk.Add(chr);
                }
                else
                {
                    // create a new chunk
                    textChunks.Add(new TextChunk(chr));
                }

                lastWordSpacing = wordSpacing;
                previousAveCharWidth = sp != null ? (averageCharWidth + sp.Width) / 2.0f : averageCharWidth;
            }

            List<TextChunk> textChunksSeparatedByDirectionality = new List<TextChunk>();
            // count up characters by directionality
            foreach (TextChunk chunk in textChunks)
            {
                // choose the dominant direction
                bool isLtrDominant = chunk.IsLtrDominant() != -1; // treat neutral as LTR
                TextChunk dirChunk = chunk.GroupByDirectionality(isLtrDominant);
                textChunksSeparatedByDirectionality.Add(dirChunk);
            }

            return textChunksSeparatedByDirectionality;
        }

        private static bool VerticallyOverlapsRuling(TextElement te, Ruling r)
        {
            return Math.Max(0, Math.Min(te.Top, r.Y2) - Math.Max(te.Bottom, r.Y1)) > 0; // .getBottom() .getTop()
        }
    }
}
