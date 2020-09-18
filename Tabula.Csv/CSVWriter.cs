using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Tabula.Writers
{
    public class CSVWriter : IWriter
    {
        public readonly string delimiter;

        public CSVWriter(string delimiter = ",")
        {
            this.delimiter = delimiter;
        }

        public void Write(StreamWriter sw, Table table)
        {
            Write(sw, new Table[] { table });
        }

        public void Write(StreamWriter sw, IReadOnlyList<Table> tables)
        {
            var csv = new CsvWriter(sw, CultureInfo.InvariantCulture);

            csv.Configuration.Delimiter = delimiter;
            csv.Configuration.NewLine = CsvHelper.Configuration.NewLine.CRLF;

            foreach (Table table in tables)
            {
                foreach (var row in table.Rows)
                {
                    //List<string> cells = new List<string>(row.Count);
                    //bool isfirst = true;
                    foreach (RectangularTextContainer tc in row)
                    {
                        //cells.Add(tc.getText());
                        csv.WriteField(tc.GetText()); //, (string.IsNullOrEmpty(tc.getText()) && isfirst) || tc.getText().Contains(delimiter));
                        //isfirst = false;
                    }
                    //csv.WriteField(cells);
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
