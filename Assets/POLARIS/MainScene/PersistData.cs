using System.Collections.Generic;
using Esri.GameEngine.Geometry;

namespace POLARIS.MainScene
{
    public static class PersistData
    {
        public static ArcGISPoint DestinationPoint = 
            new(-81.195760, 28.614369, 0, new ArcGISSpatialReference(4326));

        public static List<double[]> PathPoints = new()
        {
            new[]{28.614402, -81.195860},
            new[]{28.614469, -81.195702},
            new[]{28.614369, -81.195760}
        };
    }
}
