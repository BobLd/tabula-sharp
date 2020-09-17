using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tabula.Json;

namespace Tabula.Writers
{
    public class JSONWriter : JsonSerializer, IWriter
    {
        public JSONWriter(bool indented = false)
        {
            this.Converters.Add(TableSerializer.INSTANCE);
            this.Converters.Add(RectangularTextContainerSerializer.INSTANCE);
            if (indented) this.Formatting = Formatting.Indented;
        }

        public void Write(StreamWriter sb, Table table)
        {
            this.Serialize(sb, table);
        }

        public void Write(StreamWriter sb, IReadOnlyList<Table> tables)
        {
            this.Serialize(sb, tables);
        }

        public void Write(StringBuilder sb, Table table)
        {
            Write(sb, new Table[] { table });
        }

        public void Write(StringBuilder sb, IReadOnlyList<Table> tables)
        {
            using (var stream = new MemoryStream())
            using (var sw = new StreamWriter(stream) { AutoFlush = true })
            {
                new JSONWriter().Write(sw, tables);

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
