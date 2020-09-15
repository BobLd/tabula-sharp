using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.PdfFonts;

namespace Tabula
{
    public class TextElement : TableRectangle, HasText
    {
        internal Letter letter;

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

        public string getText()
        {
            return text;
        }

        public double getDirection()
        {
            return dir;
        }

        public double getWidthOfSpace()
        {
            return widthOfSpace;
        }

        public FontDetails getFont()
        {
            return font;
        }

        public double getFontSize()
        {
            return fontSize;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string s = base.ToString();
            sb.Append(s.Substring(0, s.Length - 1));
            sb.Append($",text={this.getText()}]");
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

        public static List<TextChunk> mergeWords(List<TextElement> textElements)
        {
            return mergeWords(textElements, new List<Ruling>());
        }

        /// <summary>
        /// heuristically merge a list of TextElement into a list of TextChunk
        /// ported from PDFBox's PDFTextStripper.writePage, with modifications.
        /// Here be dragons
        /// </summary>
        /// <param name="textElements"></param>
        /// <param name="verticalRulings"></param>
        /// <returns></returns>
        public static List<TextChunk> mergeWords(List<TextElement> textElements, List<Ruling> verticalRulings)
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

            double previousAveCharWidth = firstTC.width;
            double endOfLastTextX = firstTC.getRight();
            double maxYForLine = firstTC.getTop(); //.getBottom();
            double maxHeightForLine = firstTC.height;
            double minYTopForLine = firstTC.getBottom();//.getTop();
            double lastWordSpacing = -1;
            double wordSpacing, deltaSpace, averageCharWidth, deltaCharWidth;
            double expectedStartOfNextWordX, dist;
            TextElement sp, prevChar;
            TextChunk currentChunk;
            bool sameLine, acrossVerticalRuling;

            foreach (TextElement chr in textElements.Skip(0)) //copyOfTextElements)
            {
                currentChunk = textChunks[textChunks.Count - 1];
                prevChar = currentChunk.textElements[currentChunk.textElements.Count - 1];

                // if same char AND overlapped, skip
                if (chr.getText().Equals(prevChar.getText()) && (prevChar.overlapRatio(chr) > 0.5))
                {
                    continue;
                }

                // if chr is a space that overlaps with prevChar, skip
                if (chr.getText().Equals(" ") && Utils.feq(prevChar.getLeft(), chr.getLeft()) && Utils.feq(prevChar.getBottom(), chr.getBottom())) // getTop() getTop()
                {
                    continue;
                }

                // Resets the average character width when we see a change in font
                // or a change in the font size
                if ((chr.getFont() != prevChar.getFont()) || !Utils.feq(chr.getFontSize(), prevChar.getFontSize()))
                {
                    previousAveCharWidth = -1;
                }

                // is there any vertical ruling that goes across chr and prevChar?
                acrossVerticalRuling = false;
                foreach (Ruling r in verticalRulings)
                {
                    if (verticallyOverlapsRuling(prevChar, r) && verticallyOverlapsRuling(chr, r) && prevChar.x < r.getPosition() && chr.x > r.getPosition() ||
                        (prevChar.x > r.getPosition() && chr.x < r.getPosition()))
                    {
                        acrossVerticalRuling = true;
                        break;
                    }
                }

                // Estimate the expected width of the space based on the
                // space character with some margin.
                wordSpacing = chr.getWidthOfSpace();
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
                    averageCharWidth = chr.width / chr.getText().Length;
                }
                else
                {
                    averageCharWidth = (previousAveCharWidth + (chr.width / chr.getText().Length)) / 2.0f;
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
                if (!Utils.overlap(chr.getTop(), chr.height, maxYForLine, maxHeightForLine)) // getBottom()
                {
                    endOfLastTextX = -1;
                    expectedStartOfNextWordX = -double.MaxValue;
                    maxYForLine = -double.MaxValue;
                    maxHeightForLine = -1;
                    minYTopForLine = double.MaxValue;
                    sameLine = false;
                }

                endOfLastTextX = chr.getRight();

                // should we add a space?
                if (!acrossVerticalRuling && sameLine && expectedStartOfNextWordX < chr.getLeft() && !prevChar.getText().EndsWith(" "))
                {
                    sp = new TextElement(
                        new PdfRectangle(prevChar.BoundingBox.BottomLeft, new PdfPoint(expectedStartOfNextWordX, prevChar.BoundingBox.TopRight.Y)),
                            prevChar.getFont(),
                            prevChar.getFontSize(),
                            " ",
                            prevChar.getWidthOfSpace(), 0);

                    currentChunk.add(sp);
                }
                else
                {
                    sp = null;
                }

                maxYForLine = Math.Max(chr.getTop(), maxYForLine); // getBottom()
                maxHeightForLine = Math.Max(maxHeightForLine, chr.height);
                minYTopForLine = Math.Min(minYTopForLine, chr.getBottom()); // .getTop()

                dist = chr.getLeft() - (sp != null ? sp.getRight() : prevChar.getRight());

                // added by BobLd
                // handle cases where order of character is not good, implement quicksort???
                if (dist < -wordSpacing)
                {
                    dist = double.MaxValue; // force create new word because testColumnRecognition() fails
                }
                // end added

                if (!acrossVerticalRuling && sameLine && (dist < 0 ? currentChunk.verticallyOverlaps(chr) : dist < wordSpacing))
                {
                    currentChunk.add(chr);
                }
                else
                {
                    // create a new chunk
                    textChunks.Add(new TextChunk(chr));
                }

                lastWordSpacing = wordSpacing;
                previousAveCharWidth = sp != null ? (averageCharWidth + sp.width) / 2.0f : averageCharWidth;
            }

            List<TextChunk> textChunksSeparatedByDirectionality = new List<TextChunk>();
            // count up characters by directionality
            foreach (TextChunk chunk in textChunks)
            {
                // choose the dominant direction
                bool isLtrDominant = chunk.isLtrDominant() != -1; // treat neutral as LTR
                TextChunk dirChunk = chunk.groupByDirectionality(isLtrDominant);
                textChunksSeparatedByDirectionality.Add(dirChunk);
            }

            return textChunksSeparatedByDirectionality;
        }

        private static bool verticallyOverlapsRuling(TextElement te, Ruling r)
        {
            return Math.Max(0, Math.Min(te.getTop(), r.getY2()) - Math.Max(te.getBottom(), r.getY1())) > 0; // .getBottom() .getTop()
        }
    }
}
