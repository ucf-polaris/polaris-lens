using System;
using System.Collections.Generic;
using System.Linq;
using Google.XR.ARCoreExtensions;
using Google.XR.ARCoreExtensions.Samples.Geospatial;
using POLARIS.Managers;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace POLARIS.GeospatialScene
{
    public class PanelManager : MonoBehaviour
    {
        private LocationManager locationManager;
        public Camera Camera;
        public ARAnchorManager AnchorManager;
        
        private readonly List<TextPanel> _panels;
        
        private double2 _loadLocation;
        private float _loadTime;

        private const float LoadDistance = 1.5f; // km
        private const float RenderDistance = 200f; // m

        //public IDictionary<string, float> panel_Test = new Dictionary<string, float>();
        //public List<LocationData> location_test = new List<LocationData>();
        void Start()
        {
            locationManager = LocationManager.getInstance();
        }
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
                  DistanceInKmBetweenEarthCoordinates(currentLocation, _loadLocation) > LoadDistance/2))
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
            panel.Instantiate(new GeospatialAnchorContent(new LocationData(), "WHY HELLO THERE <style=Description>third panel <color=green>hello</color></style>", history));
            var anchor = panel.PlacePanelGeospatialAnchor(anchorObjects, AnchorManager);
            _panels.Add(panel);

            return anchor;
        }

        private static double TestMakeSmallDist(double num, double origin)
        {
            return ((num - origin) / 200) + origin;
        }

        private List<GeospatialAnchorContent> FetchNearby(double2 currentLocation, List<GameObject> anchorObjects)
        {
            // Fetch from internal DB
            var locations = GetLocationsWithinRadius(currentLocation, LoadDistance);
            //location_test = locations.ToList<LocationData>();

            var contentList = locations.Select(location => 
                                                   new GeospatialAnchorContent(
                                                       location,
                                                       TextPanel.GenerateLocationText(location), 
                                                       new GeospatialAnchorHistory(
                                                           location.BuildingLat, 
                                                           // TestMakeSmallDist(location.BuildingLat, 28.61442),
                                                           location.BuildingLong, 
                                                           // TestMakeSmallDist(location.BuildingLong, -81.19579),
                                                           location.BuildingAltitude,
                                                           location.BuildingAltitude == 0 ? AnchorType.Terrain : AnchorType.Geospatial, 
                                                           new Quaternion(0, 0, 0, 0)))).ToList();

            Debug.Log("Location selected length " + contentList.Count);

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
            
            Debug.Log("panels list length " + contentList.Count);

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
            //panel_Test.Clear();
            foreach (var panel in _panels)
            {
                if (panel.CurrentPrefab == null) continue;
                
                var withinThresh =
                    Vector3.Distance(panel.CurrentPrefab.transform.position, Camera.transform.position) <
                    RenderDistance;
                //panel_Test.Add(panel.Content.Location.BuildingName, Vector3.Distance(panel.CurrentPrefab.transform.position, Camera.transform.position));

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
            foreach (var panel in _panels)
            {
                Destroy(panel);
            }
            _panels.Clear();
        }

        private IEnumerable<LocationData> GetLocationsWithinRadius(double2 loc, double radius)
        {
            return locationManager.dataList.Where(
                location => DistanceInKmBetweenEarthCoordinates(
                    loc, new double2(location.BuildingLat, location.BuildingLong)) < radius);
            // .Where(data => data.BuildingEvents?.Length > 0).ToList();
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
        
        public static double DegreesToRadians(double degrees) {
            return degrees * Math.PI / 180;
        }
    }
}