NuGet packages available in the [releases](https://github.com/BobLd/tabula-sharp/releases) page.

# tabula-sharp
Port of [tabula-java](https://github.com/tabulapdf/tabula-java)

![Windows](https://github.com/BobLd/tabula-sharp/workflows/Windows/badge.svg)
![Linux](https://github.com/BobLd/tabula-sharp/workflows/Linux/badge.svg)
![Mac OS](https://github.com/BobLd/tabula-sharp/workflows/Mac%20OS/badge.svg)

- Supports .NET Core 3.1; .NET Standard 2.0; .NET Framework 4.5, 4.51, 4.52, 4.6, 4.61, 4.62, 4.7
- No java bindings

## Difference to tabula-java
- Uses [PdfPig](https://github.com/UglyToad/PdfPig), and not PdfBox.
- Coordinate system starts from the bottom left point of the page, and not from the top left point.
- The `NurminenDetectionAlgorithm` is not yet implemented, because it requieres an image management library.
- Table results might be different because of the way PdfPig builds Letter bounding box.

# Usage
## Stream mode - BasicExtractionAlgorithm
```csharp
using (PdfDocument document = PdfDocument.Open("doc.pdf", new ParsingOptions() { ClipPaths = true }))
{
	ObjectExtractor oe = new ObjectExtractor(document);
	PageArea page = oe.Extract(1);

	IExtractionAlgorithm ea = new BasicExtractionAlgorithm();
	List<Table> tables = ea.Extract(page);
	var table = tables[0];
	var rows = table.Rows;
}
```

## Lattice mode - SpreadsheetExtractionAlgorithm
```csharp
using (PdfDocument document = PdfDocument.Open("doc.pdf", new ParsingOptions() { ClipPaths = true }))
{
	ObjectExtractor oe = new ObjectExtractor(document);
	PageArea page = oe.Extract(1);

	IExtractionAlgorithm ea = new SpreadsheetExtractionAlgorithm();
	List<Table> tables = ea.Extract(page);
	var table = tables[0];
	var rows = table.Rows;
}
```
