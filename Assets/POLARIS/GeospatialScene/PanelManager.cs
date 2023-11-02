using System;
using System.Collections.Generic;
using System.Linq;
using Google.XR.ARCoreExtensions;
using Google.XR.ARCoreExtensions.Samples.Geospatial;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace POLARIS.GeospatialScene
{
    public class PanelManager : MonoBehaviour
    {
        public Camera Camera;
        public ARAnchorManager AnchorManager;
        
        private readonly List<TextPanel> _panels;
        
        private double2 _loadLocation;
        private float _loadTime;

        private const float LoadDistance = 400f; // m
        private const float RenderDistance = 100f; // m

        public PanelManager()
        {
            _loadLocation = new double2(0, 0);
            _loadTime = 0;
            _panels = new List<TextPanel>();
        }

        public List<GeospatialAnchorContent> FetchNearbyIfNeeded(double2 currentLocation, List<GameObject> anchorObjects)
        {
            // Wait at least 5 seconds
            // Check for distance
            if (!(Time.time - _loadTime > 5 &&
                  DistanceInKmBetweenEarthCoordinates(currentLocation, _loadLocation) > LoadDistance/2000))
            {
                // Return nothing
                return new List<GeospatialAnchorContent>();
            }
            
            _loadLocation = currentLocation;
            _loadTime = Time.time;
        
            print("FETCHING NEARBY");
            return FetchNearby(currentLocation, anchorObjects);
        }


        // Temp function for show
        public ARGeospatialAnchor PlacePanel(List<GameObject> anchorObjects, GeospatialAnchorHistory history)
        {
            if (Camera.gameObject.GetNamedChild("Panel")) return null;
            
            var panel = AnchorManager.AddComponent<TextPanel>();
            panel.Instantiate(new GeospatialAnchorContent("WHY HELLO THERE <style=Description>third panel <color=green>hello</color></style>", history));
            var anchor = panel.PlacePanelGeospatialAnchor(anchorObjects, AnchorManager);
            _panels.Add(panel);

            return anchor;
        }

        private List<GeospatialAnchorContent> FetchNearby(double2 currentLocation, List<GameObject> anchorObjects)
        {
            // Fetch from internal DB
            var locations = GetLocationsWithinRadius(currentLocation, LoadDistance);

            var contentList = locations.Select(location => 
                                                   new GeospatialAnchorContent(
                                                       TextPanel.GenerateLocationText(location), 
                                                       new GeospatialAnchorHistory(
                                                           location.BuildingLat, 
                                                           location.BuildingLong, 
                                                           location.BuildingAltitude,
                                                           location.BuildingAltitude == 0 ? AnchorType.Terrain : AnchorType.Geospatial, 
                                                           new Quaternion(0, 0, 0, 0)))).ToList();
            
            // var results = new[]
            // {
            //     new GeospatialAnchorContent("FIRST panel", new GeospatialAnchorHistory(28.614402, -81.195860, -5.6, AnchorType.Geospatial, new Quaternion(0, 0, 0, 0))),
            //     new GeospatialAnchorContent("second panel", new GeospatialAnchorHistory(28.614469, -81.195702, -5.4, AnchorType.Geospatial, new Quaternion(0, 0, 0, 0))),
            //     new GeospatialAnchorContent("<style=Description>third panel <color=green>hello</color></style>", new GeospatialAnchorHistory(28.614369, -81.195760, -5.4, AnchorType.Geospatial, new Quaternion(0, 0, 0, 0)))
            // };

            // Find which panels should be added and removed
            var addPanels = new List<int>();
            var keepPanels = new int[_panels.Count];
            for (var i = 0; i < contentList.Count; i++)
            {
                var resultLat = contentList[i].History.Latitude;
                var resultLong = contentList[i].History.Longitude;

                var found = false;
                for (var j = 0; j < _panels.Count; j++)
                {
                    if (Math.Abs(resultLat - _panels[j].Content.History.Latitude) < 0.000001 &&
                        Math.Abs(resultLong - _panels[j].Content.History.Longitude) < 0.000001)
                    {
                        keepPanels[j] = 1;
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    addPanels.Add(i);
                }
            }

            // Remove out-of-range panels
            for (var i = _panels.Count - 1; i >= 0; i--)
            {
                if (keepPanels[i] == 0)
                {
                    _panels.RemoveAt(i);
                }
            }

            // Add new panels
            foreach (var newPanel in addPanels)
            {
                var panel = AnchorManager.AddComponent<TextPanel>();
                panel.Instantiate(contentList[newPanel]);
                panel.PlacePanelGeospatialAnchor(anchorObjects, AnchorManager);
                _panels.Add(panel);
            }

            return contentList;
        }

        public void LoadNearby()
        {
            foreach (var panel in _panels)
            {
                var withinThresh =
                    Vector3.Distance(panel.CurrentPrefab.transform.position, Camera.transform.position) <
                    RenderDistance;

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

        public void ClearPanels()
        {
            _panels.Clear();
        }

        private IEnumerable<Building> GetLocationsWithinRadius(double2 loc, double radius)
        {
            return Locations.LocationList.Where(
                location => DistanceInKmBetweenEarthCoordinates(
                    loc, new double2(location.BuildingLat, location.BuildingLong)) < radius).ToList();
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