using System.Collections.Generic;

namespace Tabula.Detectors
{
    // ported from tabula-java/blob/master/src/main/java/technology/tabula/detectors/DetectionAlgorithm.java
    /*
     * ** tabula/detectors/DetectionAlgorithm.java **
     * Created by matt on 2015-12-14.
     */

    /// <summary>
    /// Table detection algorithm.
    /// </summary>
    public interface IDetectionAlgorithm
    {
        /// <summary>
        /// Detects the tables in the page.
        /// </summary>
        /// <param name="page">The page where to detect the tables.</param>
        List<TableRectangle> Detect(PageArea page);
    }
}
