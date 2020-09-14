using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Tabula.Writers;

namespace Tabula.Csv
{
    public class CSVWriter : Writer
    {
        //private CSVFormat format;
        public readonly string delimiter;

        public CSVWriter(string delimiter = ",")
        {
            this.delimiter = delimiter;
        }

        public void write(StreamWriter sw, Table table)
        {
            write(sw, new Table[] { table });
        }

        public void write(StreamWriter sw, IReadOnlyList<Table> tables)
        {
            var csv = new CsvWriter(sw, CultureInfo.InvariantCulture);

            csv.Configuration.Delimiter = delimiter;
            csv.Configuration.NewLine = CsvHelper.Configuration.NewLine.CRLF;

            foreach (Table table in tables)
            {
                foreach (var row in table.getRows())
                {
                    List<string> cells = new List<string>(row.Count);
                    foreach (RectangularTextContainer tc in row)
                    {
                        cells.Add(tc.getText());
                    }
                    csv.WriteField(cells);
                    csv.NextRecord();
                }
            }
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
                new CSVWriter().write(sw, tables);

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
