using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
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
        private static double AVERAGE_CHAR_TOLERANCE = 0.3f;

        public TextElement(Letter letter):this(letter.GlyphRectangle.TopLeft.Y,
            letter.GlyphRectangle.TopLeft.X,
            letter.GlyphRectangle.Width,
            letter.GlyphRectangle.Height,
            letter.Font,
            letter.FontSize,
            letter.Value,
            double.NaN)
        {
            this.letter = letter;
        }

        public TextElement(double y, double x, double width, double height,
                      FontDetails font, double fontSize, string c, double widthOfSpace) :
            this(y, x, width, height, font, fontSize, c, widthOfSpace, 0f)
        { }

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
            String s = base.ToString();
            sb.Append(s.Substring(0, s.Length - 1));
            sb.Append($",text={this.getText()}]");
            return sb.ToString();
        }

        public override int GetHashCode()
        {
            int prime = 31;
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
                return false;
            if (BitConverter.ToInt32(BitConverter.GetBytes(fontSize), 0) != BitConverter.ToInt32(BitConverter.GetBytes(other.fontSize), 0)) //if (java.lang.Float.floatToIntBits(fontSize) != java.lang.Float.floatToIntBits(other.fontSize))
                return false;
            if (text == null)
            {
                if (other.text != null)
                    return false;
            }
            else if (!text.Equals(other.text))
                return false;
            return (BitConverter.ToInt32(BitConverter.GetBytes(widthOfSpace), 0) != BitConverter.ToInt32(BitConverter.GetBytes(other.widthOfSpace), 0));  //return java.lang.Float.floatToIntBits(widthOfSpace) == java.lang.Float.floatToIntBits(other.widthOfSpace);
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
            if (textElements.All(te => te.letter != null))
            {
                NearestNeighbourWordExtractor nnwe = NearestNeighbourWordExtractor.Instance;
                var words = nnwe.GetWords(textElements.Select(te => te.letter).ToList());
                return words.Select(w => new TextChunk(w)).ToList();
            }

            List<TextChunk> textChunks = new List<TextChunk>();

            if (textElements.Count == 0)
            {
                return textChunks;
            }

            // it's a problem that this `remove` is side-effecty
            // other things depend on `textElements` and it can sometimes lead to the first textElement in textElement
            // not appearing in the final output because it's been removed here.
            // https://github.com/tabulapdf/tabula-java/issues/78
            List<TextElement> copyOfTextElements = new  List<TextElement>(textElements);
            textChunks.Add(new TextChunk(copyOfTextElements.Skip(1).ToList())); //remove(0)));
            TextChunk firstTC = textChunks[0];

            double previousAveCharWidth = firstTC.getWidth();
            double endOfLastTextX = firstTC.getRight();
            double maxYForLine = firstTC.getBottom();
            double maxHeightForLine = firstTC.getHeight();
            double minYTopForLine = firstTC.getTop();
            double lastWordSpacing = -1;
            double wordSpacing, deltaSpace, averageCharWidth, deltaCharWidth;
            double expectedStartOfNextWordX, dist;
            TextElement sp, prevChar;
            TextChunk currentChunk;
            bool sameLine, acrossVerticalRuling;

            foreach (TextElement chr in copyOfTextElements)
            {
                currentChunk = textChunks[textChunks.Count - 1];
                prevChar = currentChunk.textElements[currentChunk.textElements.Count - 1];

                // if same char AND overlapped, skip
                if ((chr.getText().Equals(prevChar.getText())) && (prevChar.overlapRatio(chr) > 0.5))
                {
                    continue;
                }

                // if chr is a space that overlaps with prevChar, skip
                if (chr.getText().Equals(" ") && Utils.feq(prevChar.getLeft(), chr.getLeft()) && Utils.feq(prevChar.getTop(), chr.getTop()))
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
                    if ((verticallyOverlapsRuling(prevChar, r) && verticallyOverlapsRuling(chr, r)) &&
                        (prevChar.x < r.getPosition() && chr.x > r.getPosition()) ||
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
                    averageCharWidth = (float)(chr.getWidth() / chr.getText().Length);
                }
                else
                {
                    averageCharWidth = (float)((previousAveCharWidth + (chr.getWidth() / chr.getText().Length)) / 2.0f);
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
                if (!Utils.overlap(chr.getBottom(), chr.height, maxYForLine, maxHeightForLine))
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
                if (!acrossVerticalRuling &&
                        sameLine &&
                        expectedStartOfNextWordX < chr.getLeft() &&
                        !prevChar.getText().EndsWith(" "))
                {

                    sp = new TextElement(prevChar.getTop(),               
                        prevChar.getLeft(),                   
                        expectedStartOfNextWordX - prevChar.getLeft(),
                            (float)prevChar.getHeight(),
                            prevChar.getFont(),
                            prevChar.getFontSize(),
                            " ",
                            prevChar.getWidthOfSpace());

                    currentChunk.add(sp);
                }
                else
                {
                    sp = null;
                }

                maxYForLine = Math.Max(chr.getBottom(), maxYForLine);
                maxHeightForLine = (float)Math.Max(maxHeightForLine, chr.getHeight());
                minYTopForLine = Math.Min(minYTopForLine, chr.getTop());

                dist = chr.getLeft() - (sp != null ? sp.getRight() : prevChar.getRight());

                if (!acrossVerticalRuling &&
                        sameLine &&
                        (dist < 0 ? currentChunk.verticallyOverlaps(chr) : dist < wordSpacing))
                {
                    currentChunk.add(chr);
                }
                else
                { // create a new chunk
                    textChunks.Add(new TextChunk(chr));
                }

                lastWordSpacing = wordSpacing;
                previousAveCharWidth = (float)(sp != null ? (averageCharWidth + sp.getWidth()) / 2.0f : averageCharWidth);
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
            return Math.Max(0, Math.Min(te.getBottom(), r.getY2()) - Math.Max(te.getTop(), r.getY1())) > 0;
        }
    }
}
