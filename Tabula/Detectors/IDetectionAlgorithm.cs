using System.Collections.Generic;

namespace Tabula.Detectors
{
    // https://github.com/tabulapdf/tabula-java/blob/master/src/main/java/technology/
    /**
     * ** tabula/detectors/DetectionAlgorithm.java **
     * Created by matt on 2015-12-14.
     */
    public interface IDetectionAlgorithm
    {
        List<TableRectangle> Detect(PageArea page);
    }
}
