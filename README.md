work in progress

# tabula-sharp
Port of [tabula-java](https://github.com/tabulapdf/tabula-java)

![Windows](https://github.com/BobLd/tabula-sharp/workflows/Windows/badge.svg)
![Linux](https://github.com/BobLd/tabula-sharp/workflows/Linux/badge.svg)
![Mac OS](https://github.com/BobLd/tabula-sharp/workflows/Mac%20OS/badge.svg)

About Linux and MacOS tests failing:
- Know n problem of missing fonts ([PdfPig](https://github.com/UglyToad/PdfPig) related), installing `mscorefonts`on Linux fixes the problem - these tests will only run on Windows
- Problem with `new line` character 

**TO DO**
- some tests are failing
- switch from java to c# standards naming conventions
- image library not available in dotnet core - `NurminenDetectionAlgorithm`
