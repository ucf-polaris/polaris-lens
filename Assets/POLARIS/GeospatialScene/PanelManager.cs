using System;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions.Samples.Geospatial;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace POLARIS.GeospatialScene
{
    public class PanelManager : MonoBehaviour
    {
        public Camera Camera;
        public ARAnchorManager AnchorManager;
        
        private List<TextPanel> _panels;
        
        private double2 _loadLocation;
        private float _loadTime;
        
        private const float LoadDistance = 20f; // m

        public PanelManager()
        {
            _loadLocation = new double2(0, 0);
            _loadTime = 0;
            _panels = new List<TextPanel>();
        }

        public GeospatialAnchorContent[] FetchNearbyIfNeeded(double2 currentLocation, List<GameObject> anchorObjects)
        {
            // Wait at least 5 seconds
            // Check for distance
            if (!(Time.time - _loadTime > 5 &&
                  DistanceInKmBetweenEarthCoordinates(currentLocation, _loadLocation) > 0.5))
            {
                // Return nothing
                return new GeospatialAnchorContent[] { };
            }
            
            _loadLocation = currentLocation;
            _loadTime = Time.time;
        
            print("FETCHING NEARBY");
            return FetchNearby(anchorObjects);
        }
        
        
        private GeospatialAnchorContent[] FetchNearby(List<GameObject> anchorObjects)
        {
            // mock fetch for now
            var results = new[]
            {
                new GeospatialAnchorContent("FIRST panel", new GeospatialAnchorHistory(28.614402, -81.195860, -5.6, AnchorType.Geospatial, new Quaternion(0, 0, 0, 0))),
                new GeospatialAnchorContent("second panel", new GeospatialAnchorHistory(28.614469, -81.195702, -5.4, AnchorType.Geospatial, new Quaternion(0, 0, 0, 0))),
                new GeospatialAnchorContent("<style=Description>third panel <color=green>hello</color></style>", new GeospatialAnchorHistory(28.614369, -81.195760, -5.4, AnchorType.Geospatial, new Quaternion(0, 0, 0, 0)))
            };

            _panels.Clear();
            foreach (var anchorContent in results)
            {
                var panel = AnchorManager.AddComponent<TextPanel>();
                panel.Instantiate(anchorContent);
                panel.PlacePanelGeospatialAnchor(anchorObjects, AnchorManager);
                _panels.Add(panel);
            }

            return results;
        }

        public void LoadNearby()
        {
            foreach (var panel in _panels)
            {
                var withinThresh =
                    Vector3.Distance(panel.CurrentPrefab.transform.position, Camera.transform.position) <
                    LoadDistance;

                switch (panel.Loaded)
                {
                    case false when withinThresh:
                        panel.LoadPanel();
                        break;
                    case true when !withinThresh:
                        panel.UnloadPanel();
                        break;
                }
            }
        }
        
        private static double DistanceInKmBetweenEarthCoordinates(double2 pointA, double2 pointB) {
            const int earthRadiusKm = 6371;

            var distLat = DegreesToRadians(pointB.x - pointA.x);
            var distLon = DegreesToRadians(pointB.y - pointA.y);

            var latA = DegreesToRadians(pointA.x);
            var latB = DegreesToRadians(pointB.x);

            var a = Math.Sin(distLat/2) * Math.Sin(distLat/2) +
                    Math.Sin(distLon/2) * Math.Sin(distLon/2) * Math.Cos(latA) * Math.Cos(latB); 
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a)); 
            return earthRadiusKm * c;
        }
        
        private static double DegreesToRadians(double degrees) {
            return degrees * Math.PI / 180;
        }
    }
}