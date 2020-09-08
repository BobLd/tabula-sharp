using System;
using System.Collections.Generic;
using System.Text;

namespace Tabula
{
    // https://github.com/tabulapdf/tabula-java/blob/ebc83ac2bb1a1cbe54ab8081d70f3c9fe81886ea/src/main/java/technology/tabula/Line.java#L9

    // TODO this class seems superfluous - get rid of it

    public class TableLine : TableRectangle
    {
        List<TextChunk> textChunks = new List<TextChunk>();
        public static readonly char[] WHITE_SPACE_CHARS = { ' ', '\t', '\r', '\n', '\f' };

        public List<TextChunk> getTextElements()
        {
            return textChunks;
        }

        public void setTextElements(List<TextChunk> textChunks)
        {
            this.textChunks = textChunks;
        }

        public void addTextChunk(int i, TextChunk textChunk)
        {
            if (i < 0)
            {
                throw new ArgumentException("i can't be less than 0");
            }

            int s = this.textChunks.Count;
            if (s < i + 1)
            {
                for (; s <= i; s++)
                {
                    this.textChunks.Add(null);
                }
                //this.textChunks.set(i, textChunk);
                this.textChunks[i] = textChunk;
            }
            else
            {
                this.textChunks[i] = this.textChunks[i].merge(textChunk);
            }
            this.merge(textChunk);
        }

        public void addTextChunk(TextChunk textChunk)
        {
            if (this.textChunks.Count == 0) //if (this.textChunks.isEmpty())
            {
                this.setRect(textChunk.BoundingBox);
            }
            else
            {
                this.merge(textChunk);
            }
            this.textChunks.Add(textChunk);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            String s = base.ToString();
            sb.Append(s, 0, s.Length - 1);
            sb.Append(",chunks=");
            foreach (TextChunk te in this.textChunks)
            {
                sb.Append("'" + te.getText() + "', ");
            }
            sb.Append(']');
            return sb.ToString();
        }

        internal static TableLine removeRepeatedCharacters(TableLine line, char c, int minRunLength)
        {

            TableLine rv = new TableLine();

            foreach (TextChunk t in line.getTextElements())
            {
                foreach (TextChunk r in t.squeeze(c, minRunLength))
                {
                    rv.addTextChunk(r);
                }
            }

            return rv;
        }
    }
}
