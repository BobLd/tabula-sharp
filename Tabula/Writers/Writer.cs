using System.Collections.Generic;
using System.IO;

namespace Tabula.Writers
{
    public interface Writer
    {
        void write(StreamWriter sb, Table table);
        void write(StreamWriter sb, IReadOnlyList<Table> tables);
    }
}
