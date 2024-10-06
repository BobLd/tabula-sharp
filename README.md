# tabula-sharp
`tabula-sharp` is a library for extracting tables from PDF files — it is a port of [tabula-java](https://github.com/tabulapdf/tabula-java)

![Windows](https://github.com/BobLd/tabula-sharp/workflows/Windows/badge.svg)
![Linux](https://github.com/BobLd/tabula-sharp/workflows/Linux/badge.svg)
![Mac OS](https://github.com/BobLd/tabula-sharp/workflows/Mac%20OS/badge.svg)

- Supports netstandard2.0, net462, net471, net6.0, net8.0
- No java bindings

NuGet packages available on the [releases](https://github.com/BobLd/tabula-sharp/releases) page and on www.nuget.org:
- [Tabula](https://www.nuget.org/packages/Tabula)
- [Tabula.Json](https://www.nuget.org/packages/Tabula.Json)
- [Tabula.Csv](https://www.nuget.org/packages/Tabula.Csv)

## Differences with tabula-java
- Uses [PdfPig](https://github.com/UglyToad/PdfPig), and not PdfBox.
- Coordinate system starts from the bottom left point (going up) of the page, and not from the top left point (going down).
- The `NurminenDetectionAlgorithm` is replaced by `SimpleNurminenDetectionAlgorithm`, because it requieres an image management library.
- Table results might be different because of the way PdfPig builds Letters bounding box.

# Usage
## Stream mode - BasicExtractionAlgorithm
```csharp
using (PdfDocument document = PdfDocument.Open("doc.pdf", new ParsingOptions() { ClipPaths = true }))
{
	ObjectExtractor oe = new ObjectExtractor(document);
	PageArea page = oe.Extract(1);
	
	// detect canditate table zones
	SimpleNurminenDetectionAlgorithm detector = new SimpleNurminenDetectionAlgorithm();
	var regions = detector.Detect(page);
	
	IExtractionAlgorithm ea = new BasicExtractionAlgorithm();
	List<Table> tables = ea.Extract(page.GetArea(regions[0].BoundingBox)); // take first candidate area
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

# Results
## Stream mode - BasicExtractionAlgorithm
![example](images/stream-us-018.png)
## Lattice mode - SpreadsheetExtractionAlgorithm
![example](images/lattice-eu-004.png)
