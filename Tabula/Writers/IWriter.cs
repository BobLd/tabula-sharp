using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tabula.Writers
{
    public interface IWriter
    {
        void Write(StreamWriter sb, Table table);

        void Write(StreamWriter sb, IReadOnlyList<Table> tables);

        void Write(StringBuilder sb, Table table);

        void Write(StringBuilder sb, IReadOnlyList<Table> tables);
    }
}
