using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.PdfFonts;

namespace Tabula
{
    public class TextElement : TableRectangle, IHasText
    {
        internal Letter letter; // do we really use it

        private string text;
        private FontDetails font; // PDFont
        private double fontSize;
        private double widthOfSpace, dir;
        private static double AVERAGE_CHAR_TOLERANCE = 0.3;

        public TextElement(PdfRectangle pdfRectangle, FontDetails font, double fontSize, string c, double widthOfSpace, double dir)
            : base(pdfRectangle)
        {
            this.text = c;
            this.widthOfSpace = widthOfSpace;
            this.fontSize = fontSize;
            this.font = font;
            this.dir = dir;
        }

        [Obsolete("Use TextElement(PdfRectangle...) instead.")]
        public TextElement(double y, double x, double width, double height,
            FontDetails font, double fontSize, string c, double widthOfSpace) :
            this(y, x, width, height, font, fontSize, c, widthOfSpace, 0f)
        {
            throw new ArgumentOutOfRangeException();
        }

        [Obsolete("Use TextElement(PdfRectangle...) instead.")]
        public TextElement(double y, double x, double width,
            double height, FontDetails font, double fontSize, string c, double widthOfSpace, double dir)
            : base(x, y, width, height)
        {
            //base.setRect(x, y, width, height);
            this.text = c;
            this.widthOfSpace = widthOfSpace;
            this.fontSize = fontSize;
            this.font = font;
            this.dir = dir;
            throw new ArgumentOutOfRangeException();
        }

        public string GetText()
        {
            return text;
        }

        public double GetDirection()
        {
            return dir;
        }

        public double GetWidthOfSpace()
        {
            return widthOfSpace;
        }

        public FontDetails GetFont()
        {
            return font;
        }

        public double GetFontSize()
        {
            return fontSize;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string s = base.ToString();
            sb.Append(s.Substring(0, s.Length - 1));
            sb.Append($",text={this.GetText()}]");
            return sb.ToString();
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();

            result = prime * result + BitConverter.ToInt32(BitConverter.GetBytes(dir), 0); //java.lang.Float.floatToIntBits(dir);
            result = prime * result + ((font == null) ? 0 : font.GetHashCode());
            result = prime * result + BitConverter.ToInt32(BitConverter.GetBytes(fontSize), 0); // java.lang.Float.floatToIntBits(fontSize);
            result = prime * result + ((text == null) ? 0 : text.GetHashCode());
            result = prime * result + BitConverter.ToInt32(BitConverter.GetBytes(widthOfSpace), 0); //java.lang.Float.floatToIntBits(widthOfSpace);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (!base.Equals(obj))
                return false;
            if (!this.GetType().Equals(obj.GetType())) // getClass() != obj.getClass())
                return false;
            TextElement other = (TextElement)obj;
            if (BitConverter.ToInt32(BitConverter.GetBytes(dir), 0) != BitConverter.ToInt32(BitConverter.GetBytes(other.dir), 0)) //if (java.lang.Float.floatToIntBits(dir) != java.lang.Float.floatToIntBits(other.dir))
                return false;
            if (font == null)
            {
                if (other.font != null)
                    return false;
            }
            else if (!font.Equals(other.font))
            {
                return false;
            }

            if (BitConverter.ToInt32(BitConverter.GetBytes(fontSize), 0) != BitConverter.ToInt32(BitConverter.GetBytes(other.fontSize), 0)) //if (java.lang.Float.floatToIntBits(fontSize) != java.lang.Float.floatToIntBits(other.fontSize))
                return false;
            if (text == null)
            {
                if (other.text != null)
                    return false;
            }
            else if (!text.Equals(other.text))
            {
                return false;
            }

            return BitConverter.ToInt32(BitConverter.GetBytes(widthOfSpace), 0) != BitConverter.ToInt32(BitConverter.GetBytes(other.widthOfSpace), 0);  //return java.lang.Float.floatToIntBits(widthOfSpace) == java.lang.Float.floatToIntBits(other.widthOfSpace);
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
        /// <returns></returns>
        public static List<TextChunk> MergeWords(List<TextElement> textElements, List<Ruling> verticalRulings)
        {
            List<TextChunk> textChunks = new List<TextChunk>();

            if (textElements.Count == 0)
            {
                return textChunks;
            }

            /*
            // it's a problem that this `remove` is side-effecty
            // other things depend on `textElements` and it can sometimes lead to the first textElement in textElement
            // not appearing in the final output because it's been removed here.
            // https://github.com/tabulapdf/tabula-java/issues/78
            List<TextElement> copyOfTextElements = new List<TextElement>(textElements);

            //remove(0)));
            var removed = copyOfTextElements[0];
            copyOfTextElements.RemoveAt(0);

            textChunks.Add(new TextChunk(removed));
            */

            textChunks.Add(new TextChunk(textElements[0]));

            TextChunk firstTC = textChunks[0];

            double previousAveCharWidth = firstTC.Width;
            double endOfLastTextX = firstTC.GetRight();
            double maxYForLine = firstTC.GetTop(); //.getBottom();
            double maxHeightForLine = firstTC.Height;
            double minYTopForLine = firstTC.GetBottom();//.getTop();
            double lastWordSpacing = -1;
            double wordSpacing, deltaSpace, averageCharWidth, deltaCharWidth;
            double expectedStartOfNextWordX, dist;
            TextElement sp, prevChar;
            TextChunk currentChunk;
            bool sameLine, acrossVerticalRuling;

            foreach (TextElement chr in textElements.Skip(1)) //copyOfTextElements)
            {
                currentChunk = textChunks[textChunks.Count - 1];
                prevChar = currentChunk.textElements[currentChunk.textElements.Count - 1];

                // if same char AND overlapped, skip
                if (chr.GetText().Equals(prevChar.GetText()) && (prevChar.OverlapRatio(chr) > 0.5))
                {
                    continue;
                }

                // if chr is a space that overlaps with prevChar, skip
                if (chr.GetText().Equals(" ") && Utils.Feq(prevChar.GetLeft(), chr.GetLeft()) && Utils.Feq(prevChar.GetBottom(), chr.GetBottom())) // getTop() getTop()
                {
                    continue;
                }

                // Resets the average character width when we see a change in font
                // or a change in the font size
                if ((chr.GetFont() != prevChar.GetFont()) || !Utils.Feq(chr.GetFontSize(), prevChar.GetFontSize()))
                {
                    previousAveCharWidth = -1;
                }

                // is there any vertical ruling that goes across chr and prevChar?
                acrossVerticalRuling = false;
                foreach (Ruling r in verticalRulings)
                {
                    if (VerticallyOverlapsRuling(prevChar, r) && VerticallyOverlapsRuling(chr, r) && prevChar.X < r.GetPosition() && chr.X > r.GetPosition() ||
                        (prevChar.X > r.GetPosition() && chr.X < r.GetPosition()))
                    {
                        acrossVerticalRuling = true;
                        break;
                    }
                }

                // Estimate the expected width of the space based on the
                // space character with some margin.
                wordSpacing = chr.GetWidthOfSpace();
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
                if (!Utils.Overlap(chr.GetTop(), chr.Height, maxYForLine, maxHeightForLine)) // getBottom()
                {
                    endOfLastTextX = -1;
                    expectedStartOfNextWordX = -double.MaxValue;
                    maxYForLine = -double.MaxValue;
                    maxHeightForLine = -1;
                    minYTopForLine = double.MaxValue;
                    sameLine = false;
                }

                endOfLastTextX = chr.GetRight();

                // should we add a space?
                if (!acrossVerticalRuling && sameLine && expectedStartOfNextWordX < chr.GetLeft() && !prevChar.GetText().EndsWith(" "))
                {
                    sp = new TextElement(
                        new PdfRectangle(prevChar.BoundingBox.BottomLeft, new PdfPoint(expectedStartOfNextWordX, prevChar.BoundingBox.TopRight.Y)),
                            prevChar.GetFont(),
                            prevChar.GetFontSize(),
                            " ",
                            prevChar.GetWidthOfSpace(), 0);

                    currentChunk.Add(sp);
                }
                else
                {
                    sp = null;
                }

                maxYForLine = Math.Max(chr.GetTop(), maxYForLine); // getBottom()
                maxHeightForLine = Math.Max(maxHeightForLine, chr.Height);
                minYTopForLine = Math.Min(minYTopForLine, chr.GetBottom()); // .getTop()

                dist = chr.GetLeft() - (sp != null ? sp.GetRight() : prevChar.GetRight());

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
            return Math.Max(0, Math.Min(te.GetTop(), r.GetY2()) - Math.Max(te.GetBottom(), r.GetY1())) > 0; // .getBottom() .getTop()
        }
    }
}
