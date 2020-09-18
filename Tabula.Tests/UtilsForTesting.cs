using System.IO;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.PdfFonts;
using Xunit;

namespace Tabula.Tests
{
    public static class UtilsForTesting
    {
        public static readonly FontDetails HELVETICA_BOLD = new FontDetails("HELVETICA_BOLD", true, 0, false);

        public static PageArea GetAreaFromFirstPage(string path, PdfRectangle pdfRectangle)
        {
            return GetAreaFromPage(path, 1, pdfRectangle);
        }

        public static PageArea GetAreaFromPage(string path, int page, PdfRectangle pdfRectangle)
        {
            return GetPage(path, page).GetArea(pdfRectangle);
        }

        public static PageArea GetPage(string path, int pageNumber)
        {
            ObjectExtractor oe = null;
            try
            {
                PageArea page;
                using (PdfDocument document = PdfDocument.Open(path, new ParsingOptions() { ClipPaths = true }))
                {
                    oe = new ObjectExtractor(document);
                    page = oe.Extract(pageNumber);
                }
                return page;
            }
            finally
            {
                oe?.Close();
            }
        }

        public static string[][] TableToArrayOfRows(Table table)
        {
            var tableRows = table.Rows;

            int maxColCount = 0;

            for (int i = 0; i < tableRows.Count; i++)
            {
                var row = tableRows[i];
                if (maxColCount < row.Count)
                {
                    maxColCount = row.Count;
                }
            }

            Assert.Equal(maxColCount, table.ColumnCount);

            string[][] rv = new string[tableRows.Count][];

            for (int i = 0; i < tableRows.Count; i++)
            {
                var row = tableRows[i];
                rv[i] = new string[maxColCount];
                for (int j = 0; j < row.Count; j++)
                {
                    rv[i][j] = table[i, j].GetText();
                }
            }

            return rv;
        }

        public static string LoadJson(string path)
        {
            return File.ReadAllText(path, Encoding.UTF8);

            /*
            StringBuilder stringBuilder = new StringBuilder();

            using (BufferedReader reader = new BufferedReader(new InputStreamReader(new FileInputStream(path), "UTF-8")))
            {
                String line = null;
                while ((line = reader.readLine()) != null)
                {
                    stringBuilder.Append(line);
                }
            }

            return stringBuilder.ToString();
            */
        }

        public static string LoadCsv(string path)
        {
            return File.ReadAllText(path, Encoding.UTF8).Replace("\r\n", "\n"); // "(?<!\r)\n"

            /*
            StringBuilder outp = new StringBuilder();
            CSVParser parse = org.apache.commons.csv.CSVParser.parse(new File(path), Charset.forName("utf-8"), CSVFormat.EXCEL);

            CSVPrinter printer = new CSVPrinter(out, CSVFormat.EXCEL);
            printer.printRecords(parse);
            printer.close();

            String csv = outp.ToString().replaceAll("(?<!\r)\n", "\r");
            return csv;
            */
        }

        public static string[][] LoadCsvLines(string path)
        {
            return File.ReadAllLines(path, Encoding.UTF8).Select(x => x.Split(',')).ToArray();
        }
    }
}
