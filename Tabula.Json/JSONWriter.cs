using System;
using System.Collections.Generic;
using System.IO;
using Tabula.Writers;

namespace Tabula.Json
{
    public class JSONWriter : Writer
    {
        public void write(StreamWriter sb, Table table)
        {
            throw new NotImplementedException();
        }

        public void write(StreamWriter sb, IReadOnlyList<Table> tables)
        {
            throw new NotImplementedException();
        }
    }
}
