using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using Tabula.Writers;

namespace Tabula.Json
{
    public class JSONWriter : JsonSerializer, Writer
    {
        public JSONWriter(bool indented = false)
        {
            this.Converters.Add(TableSerializer.INSTANCE);
            this.Converters.Add(RectangularTextContainerSerializer.INSTANCE);
            if (indented) this.Formatting = Formatting.Indented;
        }

        public void write(StreamWriter sb, Table table)
        {
            this.Serialize(sb, table);
        }

        public void write(StreamWriter sb, IReadOnlyList<Table> tables)
        {
            this.Serialize(sb, tables);
        }
    }
}
