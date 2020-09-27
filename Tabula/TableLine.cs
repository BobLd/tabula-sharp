using System;
using System.Collections.Generic;
using System.Text;

namespace Tabula
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/Line.java

    // TODO this class seems superfluous - get rid of it

    /// <summary>
    /// A Tabula Line.
    /// </summary>
    public class TableLine : TableRectangle
    {
        /// <summary>
        /// List of white space characters.
        /// </summary>
        public static readonly char[] WHITE_SPACE_CHARS = { ' ', '\t', '\r', '\n', '\f' };

        private List<TextChunk> textChunks = new List<TextChunk>();

        /// <summary>
        /// Gets the list of text elements.
        /// <para>This is a read-only list. Use <see cref="AddTextChunk(TextChunk)"/> to add a <see cref="TextChunk"/>.</para>
        /// </summary>
        public IReadOnlyList<TextChunk> TextElements => textChunks;

        /// <summary>
        /// Sets the TextElements.
        /// </summary>
        /// <param name="textChunks"></param>
        public void SetTextElements(List<TextChunk> textChunks)
        {
            this.textChunks = textChunks;
        }

        /// <summary>
        /// Add a text chunk to the text elements at a given index.
        /// </summary>
        /// <param name="i">The index at which to add the chunk.</param>
        /// <param name="textChunk">The chunk to be added.</param>
        public void AddTextChunk(int i, TextChunk textChunk)
        {
            if (i < 0)
            {
                throw new ArgumentException("Index can't be less than 0.", nameof(i));
            }

            int s = this.textChunks.Count;
            if (s < i + 1)
            {
                for (; s <= i; s++)
                {
                    this.textChunks.Add(null);
                }
                this.textChunks[i] = textChunk;
            }
            else
            {
                this.textChunks[i] = this.textChunks[i].Merge(textChunk);
            }
            this.Merge(textChunk);
        }

        /// <summary>
        /// Add a text chunk to the text elements.
        /// </summary>
        /// <param name="textChunk">The chunk to be added.</param>
        public void AddTextChunk(TextChunk textChunk)
        {
            if (this.textChunks.Count == 0)
            {
                this.SetRect(textChunk.BoundingBox);
            }
            else
            {
                this.Merge(textChunk);
            }
            this.textChunks.Add(textChunk);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string s = base.ToString();
            sb.Append(s, 0, s.Length - 1);
            sb.Append(",chunks=");
            foreach (TextChunk te in this.textChunks)
            {
                sb.Append("'" + te.GetText() + "', ");
            }
            sb.Append(']');
            return sb.ToString();
        }

        internal static TableLine RemoveRepeatedCharacters(TableLine line, char c, int minRunLength)
        {
            TableLine rv = new TableLine();

            foreach (TextChunk t in line.TextElements)
            {
                foreach (TextChunk r in t.Squeeze(c, minRunLength))
                {
                    rv.AddTextChunk(r);
                }
            }

            return rv;
        }
    }
}
