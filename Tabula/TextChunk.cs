using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

namespace Tabula
{
    // https://github.com/tabulapdf/tabula-java/blob/ebc83ac2bb1a1cbe54ab8081d70f3c9fe81886ea/src/main/java/technology/tabula/TextChunk.java#L11
    public class TextChunk : RectangularTextContainer<TextElement>, HasText
    {
        public Word word;

        public static TextChunk EMPTY = new TextChunk();

        public List<TextElement> textElements = new List<TextElement>();

        private TextChunk() : base(new PdfRectangle())
        { }

        public TextChunk(double top, double left, double width, double height) : base(top, left, width, height)
        {
            throw new ArgumentOutOfRangeException();
        }

        public TextChunk(TextElement textElement) : base(textElement.BoundingBox)
        {
            this.add(textElement);
        }

        public TextChunk(List<TextElement> textElements) : this(textElements[0])
        {
            for (int i = 1; i < textElements.Count; i++)
            {
                this.add(textElements[i]);
            }
        }

        private enum DirectionalityOptions
        {
            LTR, NONE, RTL
        }

        private static Dictionary<string, DirectionalityOptions> directionalities = new Dictionary<string, DirectionalityOptions>()
        {
            { "AN", DirectionalityOptions.LTR },    // DIRECTIONALITY_ARABIC_NUMBER, Weak BCT
            { "BN", DirectionalityOptions.NONE },   // DIRECTIONALITY_BOUNDARY_NEUTRAL, Weak BCT
            { "CS", DirectionalityOptions.LTR },    // DIRECTIONALITY_COMMON_NUMBER_SEPARATOR, Weak BCT

            { "EN", DirectionalityOptions.LTR },    // DIRECTIONALITY_EUROPEAN_NUMBER, Weak BCT
            { "ES", DirectionalityOptions.LTR },    // DIRECTIONALITY_EUROPEAN_NUMBER_SEPARATOR, Weak BCT
            { "ET", DirectionalityOptions.LTR },    // DIRECTIONALITY_EUROPEAN_NUMBER_TERMINATOR, Weak BCT

            { "L", DirectionalityOptions.LTR },     // DIRECTIONALITY_LEFT_TO_RIGHT, Strong BCT
            { "LRE", DirectionalityOptions.LTR },   // DIRECTIONALITY_LEFT_TO_RIGHT_EMBEDDING, Strong BCT
            { "LRO", DirectionalityOptions.LTR },   // DIRECTIONALITY_LEFT_TO_RIGHT_OVERRIDE, Strong BCT

            { "NSM", DirectionalityOptions.NONE },  // DIRECTIONALITY_NONSPACING_MARK, Weak BCT
            { "ON", DirectionalityOptions.NONE },   // DIRECTIONALITY_OTHER_NEUTRALS, Neutral BCT
            { "B", DirectionalityOptions.NONE },    // DIRECTIONALITY_PARAGRAPH_SEPARATOR, Neutral BCT

            { "PDF", DirectionalityOptions.NONE },  // DIRECTIONALITY_POP_DIRECTIONAL_FORMAT, Weak BCT
            { "R", DirectionalityOptions.RTL },     // DIRECTIONALITY_RIGHT_TO_LEFT, Strong BCT
            { "AL", DirectionalityOptions.RTL },    // DIRECTIONALITY_RIGHT_TO_LEFT_ARABIC, Strong BCT

            { "RLE", DirectionalityOptions.RTL },   // DIRECTIONALITY_RIGHT_TO_LEFT_EMBEDDING, Strong BCT
            { "RLO", DirectionalityOptions.RTL },   // DIRECTIONALITY_RIGHT_TO_LEFT_OVERRIDE, Strong BCT
            { "S", DirectionalityOptions.RTL },     // DIRECTIONALITY_SEGMENT_SEPARATOR, Neutral BCT // Bobld: should be none??

            { "WS", DirectionalityOptions.NONE },   // DIRECTIONALITY_WHITESPACE, Neutral BCT

            { "LRI", DirectionalityOptions.LTR },
            { "RLI", DirectionalityOptions.RTL },
            { "FSI", DirectionalityOptions.NONE },
            { "PDI", DirectionalityOptions.NONE },
        };

        /// <summary>
        /// Splits a TextChunk into N TextChunks, where each chunk is of a single directionality, and
        /// then reverse the RTL ones.
        /// what we're doing here is *reversing* the Unicode bidi algorithm
        /// in the language of that algorithm, each chunk is a(maximal) directional run.
        /// We attach whitespace to the beginning of non-RTL
        /// </summary>
        /// <param name="isLtrDominant"></param>
        /// <returns></returns>
        public TextChunk groupByDirectionality(bool isLtrDominant)
        {
            if (this.getTextElements().Count <= 0)
            {
                throw new ArgumentException();
            }

            List<List<TextElement>> chunks = new List<List<TextElement>>();
            List<TextElement> buff = new List<TextElement>();
            DirectionalityOptions buffDirectionality = DirectionalityOptions.NONE; // the directionality of the characters in buff;

            foreach (TextElement te in this.getTextElements())
            {
                //TODO: we need to loop over the textelement characters
                //      because it is possible for a textelement to contain multiple characters?


                // System.out.println(te.getText() + " is " + Character.getDirectionality(te.getText().charAt(0) ) + " " + directionalities.get(Character.getDirectionality(te.getText().charAt(0) )));
                if (buff.Count == 0)
                {
                    buff.Add(te);
                    buffDirectionality = directionalities[te.getText()[0].getDirectionality()];
                }
                else
                {
                    if (buffDirectionality == DirectionalityOptions.NONE)
                    {
                        buffDirectionality = directionalities[te.getText()[0].getDirectionality()];
                    }
                    DirectionalityOptions teDirectionality = directionalities[te.getText()[0].getDirectionality()];

                    if (teDirectionality == buffDirectionality || teDirectionality == DirectionalityOptions.NONE)
                    {
                        if (te.getText()[0].getDirectionality() == "WS" && (buffDirectionality == (isLtrDominant ? DirectionalityOptions.RTL : DirectionalityOptions.LTR)))
                        {
                            buff.Insert(0, te);
                        }
                        else
                        {
                            buff.Add(te);
                        }
                    }
                    else
                    {
                        // finish this chunk
                        if (buffDirectionality == DirectionalityOptions.RTL)
                        {
                            buff.Reverse(); // Collections.reverse(buff);
                        }
                        chunks.Add(buff);

                        // and start a new one
                        buffDirectionality = directionalities[te.getText()[0].getDirectionality()];
                        buff = new List<TextElement>();
                        buff.Add(te);
                    }
                }
            }
            if (buffDirectionality == DirectionalityOptions.RTL)
            {
                buff.Reverse();
            }
            chunks.Add(buff);

            List<TextElement> everything = new List<TextElement>();
            if (!isLtrDominant)
            {
                chunks.Reverse();
            }
            foreach (List<TextElement> group in chunks)
            {
                everything.AddRange(group);
            }
            return new TextChunk(everything);
        }

        /// <summary>
        /// 1 is LTR, 0 is neutral, -1 is RTL
        /// </summary>
        /// <returns></returns>
        public override int isLtrDominant()
        {
            int ltrCnt = 0;
            int rtlCnt = 0;
            for (int i = 0; i < this.getTextElements().Count; i++)
            {
                String elementText = this.getTextElements()[i].getText();
                for (int j = 0; j < elementText.Length; j++)
                {
                    var dir = elementText[j].getDirectionality();
                    if ((dir == "L") || (dir == "LRE") || (dir == "LRO"))
                    {
                        ltrCnt++;
                    }
                    else if ((dir == "R") || (dir == "AL") || (dir == "RLE") || (dir == "RLO"))
                    {
                        rtlCnt++;
                    }
                }
            }
            return ltrCnt.CompareTo(rtlCnt);
        }

        public TextChunk merge(TextChunk other)
        {
            base.merge(other); // super.merge(other);
            return this;
        }

        public void add(TextElement textElement)
        {
            this.textElements.Add(textElement);
            this.BoundingBox = Utils.bounds(new[] { this.BoundingBox, textElement.BoundingBox });
            //this.merge(textElement);
        }

        public void add(List<TextElement> elements)
        {
            foreach (TextElement te in elements)
            {
                this.add(te);
            }
        }

        public override List<TextElement> getTextElements()
        {
            return textElements;
        }

        public override string getText()
        {
            if (this.textElements.Count == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            foreach (TextElement te in this.textElements)
            {
                sb.Append(te.getText());
            }

            return sb.ToString().Normalize(NormalizationForm.FormKC).Trim();
        }

        public override string getText(bool useLineReturns)
        {
            // TODO Auto-generated method stub
            return null;
        }

        /// <summary>
        /// Returns true if text contained in this TextChunk is the same repeated character
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool isSameChar(char c)
        {
            return isSameChar(new char[] { c });
        }

        public bool isSameChar(char[] c)
        {
            String s = this.getText();
            List<char> chars = c.ToList();
            for (int i = 0; i < s.Length; i++)
            {
                if (!chars.Contains(s[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Splits a TextChunk in two, at the position of the i-th TextElement
        /// </summary>
        /// <param name="i"></param>
        public TextChunk[] splitAt(int i)
        {
            if (i < 1 || i >= this.getTextElements().Count)
            {
                throw new ArgumentException();
            }

            TextChunk[] rv = new TextChunk[]
            {
                new TextChunk(this.getTextElements().subList(0, i)), //.subList(0, i)),
                new TextChunk(this.getTextElements().subList(i, this.getTextElements().Count)) //.subList(i, this.getTextElements().size()))
            };

            // TODO: remove below
            if (this.getTextElements().Count != (rv[0].getTextElements().Count + rv[1].getTextElements().Count))
            {
                throw new NotImplementedException();
            }

            return rv;
        }

        /// <summary>
        /// Removes runs of identical TextElements in this TextChunk
        /// <para>For example, if the TextChunk contains this string of characters: "1234xxxxx56xx"
        /// and c == 'x' and minRunLength == 4, this method will return a list of TextChunk
        /// such that: ["1234", "56xx"]
        /// </para>
        /// </summary>
        /// <param name="c"></param>
        /// <param name="minRunLength"></param>
        public List<TextChunk> squeeze(char c, int minRunLength)
        {
            char? currentChar, lastChar = null;
            int subSequenceLength = 0, subSequenceStart = 0;
            TextChunk[] t;
            List<TextChunk> rv = new List<TextChunk>();

            for (int i = 0; i < this.getTextElements().Count; i++)
            {
                TextElement textElement = this.getTextElements()[i];
                String text = textElement.getText();
                if (text.Length > 1)
                {
                    currentChar = text.Trim()[0];
                }
                else
                {
                    currentChar = text[0];
                }


                if (lastChar != null && currentChar.Equals(c) && lastChar.Equals(currentChar))
                {
                    subSequenceLength++;
                }
                else
                {
                    if (((lastChar != null && !lastChar.Equals(currentChar)) || i + 1 == this.getTextElements().Count) && subSequenceLength >= minRunLength)
                    {

                        if (subSequenceStart == 0 && subSequenceLength <= this.getTextElements().Count - 1)
                        {
                            t = this.splitAt(subSequenceLength);
                        }
                        else
                        {
                            t = this.splitAt(subSequenceStart);
                            rv.Add(t[0]);
                        }
                        rv.AddRange(t[1].squeeze(c, minRunLength)); // Lo and behold, recursion.
                        break;

                    }
                    subSequenceLength = 1;
                    subSequenceStart = i;
                }
                lastChar = currentChar;
            }

            if (rv.Count == 0)
            {
                // no splits occurred, hence this.squeeze() == [this]
                if (subSequenceLength >= minRunLength && subSequenceLength < this.textElements.Count)
                {
                    TextChunk[] chunks = this.splitAt(subSequenceStart);
                    rv.Add(chunks[0]);
                }
                else
                {
                    rv.Add(this);
                }
            }

            return rv;
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = base.GetHashCode();
            result = prime * result
                    + ((textElements == null) ? 0 : textElements.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (!base.Equals(obj))
                return false;
            if (GetType() != obj.GetType())
                return false;
            TextChunk other = (TextChunk)obj;
            if (textElements == null)
            {
                if (other.textElements != null)
                    return false;
            }
            else if (!textElements.Equals(other.textElements))
                return false;
            return true;
        }

        public static bool allSameChar(List<TextChunk> textChunks)
        {
            /* the previous, far more elegant version of this method failed when there was an empty TextChunk in textChunks.
             * so I rewrote it in an ugly way. but it works!
             * it would be good for this to get rewritten eventually
             * the purpose is basically just to return true iff there are 2+ TextChunks and they're identical.
             * -Jeremy 5/13/2016
             */

            if (textChunks.Count == 1) return false;
            bool hasHadAtLeastOneNonEmptyTextChunk = false;
            char first = '\u0000';
            foreach (TextChunk tc in textChunks)
            {
                if (tc.getText().Length == 0)
                {
                    continue;
                }
                if (first == '\u0000')
                {
                    first = tc.getText()[0];
                }
                else
                {
                    hasHadAtLeastOneNonEmptyTextChunk = true;
                    if (!tc.isSameChar(first)) return false;
                }
            }
            return hasHadAtLeastOneNonEmptyTextChunk;
        }

        public static List<TableLine> groupByLines(List<TextChunk> textChunks)
        {
            Utils.sort(textChunks); // added by bld: force re-sorting

            List<TableLine> lines = new List<TableLine>();

            if (textChunks.Count == 0)
            {
                return lines;
            }

            double bbwidth = TableRectangle.boundingBoxOf(textChunks).width;

            TableLine l = new TableLine();
            l.addTextChunk(textChunks[0]);
            textChunks.RemoveAt(0);
            lines.Add(l);

            TableLine last = lines[lines.Count - 1];
            foreach (TextChunk te in textChunks)
            {
                if (last.verticalOverlapRatio(te) < 0.1)
                {
                    if (last.width / bbwidth > 0.9 && TextChunk.allSameChar(last.getTextElements()))
                    {
                        lines.RemoveAt(lines.Count - 1);
                    }
                    lines.Add(new TableLine());
                    last = lines[lines.Count - 1];
                }
                last.addTextChunk(te);
            }

            if (last.width / bbwidth > 0.9 && TextChunk.allSameChar(last.getTextElements()))
            {
                lines.RemoveAt(lines.Count - 1);
            }

            List<TableLine> rv = new List<TableLine>(lines.Count);

            foreach (TableLine line in lines)
            {
                rv.Add(TableLine.removeRepeatedCharacters(line, ' ', 3));
            }

            return rv;
        }
    }
}
