using System.Collections.Generic;
using Google.XR.ARCoreExtensions.Samples.Geospatial;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace POLARIS.GeospatialScene
{
    public class LoadPanels : MonoBehaviour
    {
        public static GeospatialAnchorContent[] LoadNearby(List<GameObject> anchorObjects, ARAnchorManager anchorManager)
        {
            // mock fetch for now
            var results = new[]
            {
                new GeospatialAnchorContent("FIRST panel", new GeospatialAnchorHistory(28.614481, -81.195693, -5.6, new Quaternion(0, 0, 0, 0))),
                new GeospatialAnchorContent("second panel", new GeospatialAnchorHistory(28.614469, -81.195702, -5.4, new Quaternion(0, 0, 0, 0)))
            };

            foreach (var anchor in results)
            {
                var panel = anchorManager.AddComponent<TextPanel>();
                panel.Instantiate(anchor);
                panel.PlacePanelGeospatialAnchor(anchorObjects, anchorManager);
            }

            return results;
        }
    }
}