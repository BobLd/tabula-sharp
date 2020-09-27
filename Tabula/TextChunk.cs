using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UglyToad.PdfPig.Core;

namespace Tabula
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/TextChunk.java
    public class TextChunk : RectangularTextContainer<TextElement>, IHasText
    {
        /// <summary>
        /// An empty text chunk.
        /// </summary>
        public static TextChunk EMPTY => new TextChunk();

        /// <summary>
        /// Create an empty text chunk.
        /// </summary>
        private TextChunk()
            : base(new PdfRectangle())
        {
            SetTextElements(new List<TextElement>());
        }

        /// <summary>
        /// Create a text chunk from the text element.
        /// </summary>
        /// <param name="textElement"></param>
        public TextChunk(TextElement textElement)
            : base(textElement.BoundingBox)
        {
            SetTextElements(new List<TextElement>());
            this.Add(textElement);
        }

        /// <summary>
        /// Create a text chunk from the text elements.
        /// </summary>
        /// <param name="textElements"></param>
        public TextChunk(List<TextElement> textElements)
            : this(textElements[0])
        {
            for (int i = 1; i < textElements.Count; i++)
            {
                this.Add(textElements[i]);
            }
        }

        private enum DirectionalityOptions
        {
            LTR, NONE, RTL
        }

        private static readonly Dictionary<string, DirectionalityOptions> directionalities = new Dictionary<string, DirectionalityOptions>()
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
            { "S", DirectionalityOptions.RTL },     // DIRECTIONALITY_SEGMENT_SEPARATOR, Neutral BCT // Bobld: should be NONE??

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
        /// in the language of that algorithm, each chunk is a (maximal) directional run.
        /// We attach whitespace to the beginning of non-RTL.
        /// </summary>
        /// <param name="isLtrDominant"></param>
        public TextChunk GroupByDirectionality(bool isLtrDominant)
        {
            if (this.TextElements.Count == 0)
            {
                throw new ArgumentException("TextElements is empty.");
            }

            List<List<TextElement>> chunks = new List<List<TextElement>>();
            List<TextElement> buff = new List<TextElement>();
            DirectionalityOptions buffDirectionality = DirectionalityOptions.NONE; // the directionality of the characters in buff;

            foreach (TextElement te in this.TextElements)
            {
                //TODO: we need to loop over the textelement characters
                //      because it is possible for a textelement to contain multiple characters?

                if (buff.Count == 0)
                {
                    buff.Add(te);
                    buffDirectionality = directionalities[te.GetText()[0].GetDirectionality()];
                }
                else
                {
                    if (buffDirectionality == DirectionalityOptions.NONE)
                    {
                        buffDirectionality = directionalities[te.GetText()[0].GetDirectionality()];
                    }
                    DirectionalityOptions teDirectionality = directionalities[te.GetText()[0].GetDirectionality()];

                    if (teDirectionality == buffDirectionality || teDirectionality == DirectionalityOptions.NONE)
                    {
                        if (te.GetText()[0].GetDirectionality() == "WS" && (buffDirectionality == (isLtrDominant ? DirectionalityOptions.RTL : DirectionalityOptions.LTR)))
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
                            buff.Reverse();
                        }
                        chunks.Add(buff);

                        // and start a new one
                        buffDirectionality = directionalities[te.GetText()[0].GetDirectionality()];
                        buff = new List<TextElement> { te };
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
        /// 1 is Left-to-Right, 0 is neutral, -1 is Right-to-Left.
        /// </summary>
        public override int IsLtrDominant()
        {
            int ltrCnt = 0;
            int rtlCnt = 0;
            for (int i = 0; i < this.TextElements.Count; i++)
            {
                string elementText = this.TextElements[i].GetText();
                for (int j = 0; j < elementText.Length; j++)
                {
                    var dir = elementText[j].GetDirectionality();
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

        /// <summary>
        /// Merges this TextChunk with the other.
        /// <para>Also does it in place.</para>
        /// </summary>
        /// <param name="other"></param>
        public TextChunk Merge(TextChunk other)
        {
            base.Merge(other);
            return this;
        }

        /// <summary>
        /// Adds the text element to the TextChunk.
        /// </summary>
        /// <param name="textElement"></param>
        public void Add(TextElement textElement)
        {
            this.textElements.Add(textElement);
            this.Merge(textElement);
        }

        /// <summary>
        /// Adds the text elements to the TextChunk.
        /// </summary>
        /// <param name="elements"></param>
        public void Add(List<TextElement> elements)
        {
            foreach (TextElement te in elements)
            {
                this.Add(te);
            }
        }

        /// <summary>
        /// Gets the TextChunk's text.
        /// </summary>
        public override string GetText()
        {
            if (this.textElements.Count == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            foreach (TextElement te in this.textElements)
            {
                sb.Append(te.GetText());
            }

            return sb.ToString().Normalize(NormalizationForm.FormKC).Trim();
        }

        /// <summary>
        /// Returns null.
        /// <para>TODO Auto-generated method stub</para>
        /// </summary>
        /// <param name="useLineReturns"></param>
        /// <returns></returns>
        public override string GetText(bool useLineReturns)
        {
            // TODO Auto-generated method stub
            return null;
        }

        /// <summary>
        /// Returns true if text contained in this TextChunk is the same repeated character
        /// </summary>
        /// <param name="c"></param>
        public bool IsSameChar(char c)
        {
            return IsSameChar(new char[] { c });
        }

        public bool IsSameChar(char[] c)
        {
            string s = this.GetText();
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
        public TextChunk[] SplitAt(int i)
        {
            if (i < 1 || i >= this.TextElements.Count)
            {
                throw new ArgumentException();
            }

            TextChunk[] rv = new TextChunk[]
            {
                new TextChunk(this.TextElements.SubList(0, i)),
                new TextChunk(this.TextElements.SubList(i, this.TextElements.Count))
            };

            System.Diagnostics.Debug.Assert(this.TextElements.Count == (rv[0].TextElements.Count + rv[1].TextElements.Count));

            return rv;
        }

        /// <summary>
        /// Removes runs of identical TextElements in this TextChunk.
        /// <para>For example, if the TextChunk contains this string of characters: "1234xxxxx56xx"
        /// and c == 'x' and minRunLength == 4, this method will return a list of TextChunk
        /// such that: ["1234", "56xx"]
        /// </para>
        /// </summary>
        /// <param name="c"></param>
        /// <param name="minRunLength"></param>
        public List<TextChunk> Squeeze(char c, int minRunLength)
        {
            char? currentChar, lastChar = null;
            int subSequenceLength = 0, subSequenceStart = 0;
            TextChunk[] t;
            List<TextChunk> rv = new List<TextChunk>();

            for (int i = 0; i < this.TextElements.Count; i++)
            {
                TextElement textElement = this.TextElements[i];
                string text = textElement.GetText();
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
                    if (((lastChar != null && !lastChar.Equals(currentChar)) || i + 1 == this.TextElements.Count) && subSequenceLength >= minRunLength)
                    {
                        if (subSequenceStart == 0 && subSequenceLength <= this.TextElements.Count - 1)
                        {
                            t = this.SplitAt(subSequenceLength);
                        }
                        else
                        {
                            t = this.SplitAt(subSequenceStart);
                            rv.Add(t[0]);
                        }
                        rv.AddRange(t[1].Squeeze(c, minRunLength)); // Lo and behold, recursion.
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
                    TextChunk[] chunks = this.SplitAt(subSequenceStart);
                    rv.Add(chunks[0]);
                }
                else
                {
                    rv.Add(this);
                }
            }

            return rv;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            return prime * result + ((textElements?.GetHashCode()) ?? 0);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is TextChunk other)
            {
                if (textElements == null)
                {
                    if (other.textElements != null)
                        return false;
                }
                else if (!textElements.Equals(other.textElements))
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// The purpose is basically just to return true iff there are 2+ TextChunks and they're identical.
        /// </summary>
        /// <param name="textChunks"></param>
        public static bool AllSameChar(IReadOnlyList<TextChunk> textChunks)
        {
            /* 
             * the previous, far more elegant version of this method failed when there was an empty TextChunk in textChunks.
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
                if (tc.GetText().Length == 0)
                {
                    continue;
                }

                if (first == '\u0000')
                {
                    first = tc.GetText()[0];
                }
                else
                {
                    hasHadAtLeastOneNonEmptyTextChunk = true;
                    if (!tc.IsSameChar(first)) return false;
                }
            }
            return hasHadAtLeastOneNonEmptyTextChunk;
        }

        public static List<TableLine> GroupByLines(List<TextChunk> textChunks)
        {
            Utils.Sort(textChunks); // added by bobLd: force re-sorting

            List<TableLine> lines = new List<TableLine>();

            if (textChunks.Count == 0)
            {
                return lines;
            }

            double bbwidth = BoundingBoxOf(textChunks).Width;

            TableLine l = new TableLine();
            l.AddTextChunk(textChunks[0]);
            textChunks.RemoveAt(0);
            lines.Add(l);

            TableLine last = lines[lines.Count - 1];
            foreach (TextChunk te in textChunks)
            {
                if (last.VerticalOverlapRatio(te) < 0.1)
                {
                    if (last.Width / bbwidth > 0.9 && AllSameChar(last.TextElements))
                    {
                        lines.RemoveAt(lines.Count - 1);
                    }
                    lines.Add(new TableLine());
                    last = lines[lines.Count - 1];
                }
                last.AddTextChunk(te);
            }

            if (last.Width / bbwidth > 0.9 && AllSameChar(last.TextElements))
            {
                lines.RemoveAt(lines.Count - 1);
            }

            List<TableLine> rv = new List<TableLine>(lines.Count);

            foreach (TableLine line in lines)
            {
                rv.Add(TableLine.RemoveRepeatedCharacters(line, ' ', 3));
            }

            return rv;
        }
    }
}
