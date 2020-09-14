using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        public void write(StringBuilder sb, Table table)
        {
            write(sb, new Table[] { table });
        }

        public void write(StringBuilder sb, IReadOnlyList<Table> tables)
        {
            using (var stream = new MemoryStream())
            using (var sw = new StreamWriter(stream) { AutoFlush = true })
            {
                new JSONWriter().write(sw, tables);

                var reader = new StreamReader(stream);
                stream.Position = 0;
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    sb.AppendLine(line);
                }
            }
        }
    }
}
