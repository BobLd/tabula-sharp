using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Tabula.Csv
{
    public class CSVWriter : Writers.Writer
    {
        //private CSVFormat format;

        public void write(StreamWriter sb, Table table)
        {
            write(sb, new Table[] { table });
        }

        public void write(StreamWriter sb, IReadOnlyList<Table> tables)
        {
            var csv = new CsvWriter(sb, CultureInfo.InvariantCulture);

            csv.Configuration.Delimiter = ",";
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
    }
}
