using System.Collections.Generic;

namespace Tabula.Extractors
{
    public interface ExtractionAlgorithm
    {
        List<Table> extract(PageArea page);
    }
}
