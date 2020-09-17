using System.Collections.Generic;

namespace Tabula.Extractors
{
    public interface IExtractionAlgorithm
    {
        List<Table> Extract(PageArea page);
    }
}
