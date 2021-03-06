using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Tabula.Writers
{
    public class CSVWriter : IWriter, IDisposable
    {
        public readonly string delimiter;

        public CSVWriter(string delimiter = ",")
        {
            this.delimiter = delimiter;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Write(StreamWriter sw, Table table)
        {
            Write(sw, new Table[] { table });
        }

        public void Write(StreamWriter sw, IReadOnlyList<Table> tables)
        {
            // the CsvWriter is not disposed here as it would also dispose the StreamWriter
            var csv = new CsvWriter(sw, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = delimiter });

            foreach (Table table in tables)
            {
                foreach (var row in table.Rows)
                {
                    foreach (RectangularTextContainer tc in row)
                    {
                        csv.WriteField(tc.GetText());
                    }
                    csv.NextRecord();
                }
            }
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
                new CSVWriter().Write(sw, tables);

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
