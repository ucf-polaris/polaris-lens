using System.Collections.Generic;
using Esri.GameEngine.Geometry;

namespace POLARIS.MainScene
{
    public static class PersistData
    {
        public static ArcGISPoint DestinationPoint = 
            new ArcGISPoint(-81.19543, 28.60991, 0, new ArcGISSpatialReference(4326));

        public static List<double[]> PathPoints = null;
    }
}
