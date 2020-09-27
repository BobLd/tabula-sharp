using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tabula.Writers
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/writers/Writer.java
    /// <summary>
    /// Base interface for tabula writer.
    /// </summary>
    public interface IWriter
    {
        /// <summary>
        /// Write the table to the stream.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="table"></param>
        void Write(StreamWriter sb, Table table);

        /// <summary>
        /// Write the tables to the stream.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="tables"></param>
        void Write(StreamWriter sb, IReadOnlyList<Table> tables);

        /// <summary>
        /// Write the table to the stream.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="table"></param>
        void Write(StringBuilder sb, Table table);

        /// <summary>
        /// Write the tables to the stream.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="tables"></param>
        void Write(StringBuilder sb, IReadOnlyList<Table> tables);
    }
}
