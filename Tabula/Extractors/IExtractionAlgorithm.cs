using System.Collections.Generic;

namespace Tabula.Extractors
{
    /// <summary>
    /// Table extraction algorithm.
    /// </summary>
    public interface IExtractionAlgorithm
    {
        /// <summary>
        /// Extracts the tables in the page.
        /// </summary>
        /// <param name="page">The page where to extract the tables.</param>
        List<Table> Extract(PageArea page);
    }
}
