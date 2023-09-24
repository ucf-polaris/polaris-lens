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
        public ARAnchorManager AnchorManager;
        
        private List<GameObject> _panelAnchors;
        
        private double2 _loadLocation;
        private float _loadTime;

        public PanelManager()
        {
            _loadLocation = new double2(double.PositiveInfinity, double.NegativeInfinity);
            _loadTime = 0;
        }

        public GeospatialAnchorContent[] LoadNearbyIfNeeded(double2 currentLocation, List<GameObject> anchorObjects)
        {
            // Wait at least 5 seconds
            // Check for distance
            if (!(Time.time - _loadTime > 5 &&
                  DistanceInKmBetweenEarthCoordinates(currentLocation, _loadLocation) > 0.2))
            {
                return new GeospatialAnchorContent[] { };
            }
            
            _loadLocation = currentLocation;
            _loadTime = Time.time;

            return LoadNearby(anchorObjects);
        }
        
        
        private GeospatialAnchorContent[] LoadNearby(List<GameObject> anchorObjects)
        {
            // mock fetch for now
            var results = new[]
            {
                new GeospatialAnchorContent("FIRST panel", new GeospatialAnchorHistory(28.614481, -81.195693, -5.6, AnchorType.Geospatial, new Quaternion(0, 0, 0, 0))),
                new GeospatialAnchorContent("second panel", new GeospatialAnchorHistory(28.614469, -81.195702, -5.4, AnchorType.Geospatial, new Quaternion(0, 0, 0, 0)))
            };

            foreach (var anchor in results)
            {
                var panel = AnchorManager.AddComponent<TextPanel>();
                panel.Instantiate(anchor);
                panel.PlacePanelGeospatialAnchor(anchorObjects, AnchorManager);
            }

            return results;
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