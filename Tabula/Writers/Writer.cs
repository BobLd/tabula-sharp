using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tabula.Writers
{
    public interface Writer
    {
        void write(StreamWriter sb, Table table);

        void write(StreamWriter sb, IReadOnlyList<Table> tables);

        void write(StringBuilder sb, Table table);

        void write(StringBuilder sb, IReadOnlyList<Table> tables);
    }
}
