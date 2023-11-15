// Copyright 2022 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Esri.HPFramework;
using Newtonsoft.Json.Linq;
using POLARIS.MainScene;
using POLARIS.Managers;
using QuickEye.UIToolkit;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace POLARIS
{
    public class UcfRouteManager : MonoBehaviour
    {
        public GameObject RouteMarker;
        public GameObject RouteBreadcrumb;
        public GameObject Route;
        public GameObject RouteInfo;
        public GameObject LocationMarker;

        [Header("Path Colors")] 
        public Color PathStart;
        public Color PathEnd;

        public float RerouteDist;

        private Label _routingSrcLabel;
        private Label _routingDestLabel;
        private Label _routingInfoLabel;
        private GroupBox _routingSrcBox;
        private VisualElement _routingBox;
        private Button _slideButton;
        private Button _stopButton;

        private Texture2D _checkMark;
        private Texture2D _leftArrow;
        private bool _slideIsCheck;
        
        private ArcGISMapComponent _arcGisMapComponent;

        private const float ElevationOffset = 2.0f;

        private const int StopCount = 2;
        private readonly Stack<GameObject> _stops = new();
        private const string RoutingURL = "https://route-api.arcgis.com/arcgis/rest/services/World/Route/NAServer/Route_World/solve";
        private readonly List<GameObject> _breadcrumbs = new();

        private LineRenderer _lineRenderer;

        private readonly HttpClient _client = new();

        private HPRoot _root;
        private double3 _lastRootPosition;

        private Camera _mainCamera;

        private bool _shouldClose = false;
        private bool _lastShouldClose = true;
        private float _pressTime = 0;
        private float _lastRerouteTime = 0;
        private float _closeTime;
        private bool _closed = false;

        private UserManager userManager;
        private LocationManager _locationManager;
        private Queue<string> suggestedLocations;
        [SerializeField] private int numSuggestions = 3;

        private void Start()
        {
            // We need this ArcGISMapComponent for the FromCartesianPosition Method
            // defined on the ArcGISMapComponent.View
            _arcGisMapComponent = FindObjectOfType<ArcGISMapComponent>();

            _lineRenderer = Route.GetComponent<LineRenderer>();
            _lineRenderer.widthMultiplier = 5;
            _lineRenderer.numCapVertices = 6;
            _lineRenderer.numCornerVertices = 4;
            _lineRenderer.positionCount = 0;

            _root = _arcGisMapComponent.GetComponent<HPRoot>();
            _lastRootPosition = _root.RootUniversePosition;

            _mainCamera = Camera.main;

            // UI Elements
            var rootVisual = RouteInfo.GetComponent<UIDocument>().rootVisualElement;
            _routingSrcLabel = rootVisual.Q<Label>("RoutingSrc");
            _routingDestLabel = rootVisual.Q<Label>("RoutingDest");
            _routingInfoLabel = rootVisual.Q<Label>("RoutingInfos");
            _routingSrcBox = rootVisual.Q<GroupBox>("RoutingSrcBox");
            _routingBox = rootVisual.Q<VisualElement>("RoutingInfo");
            _slideButton = rootVisual.Q<Button>("SlideButton");
            _stopButton = rootVisual.Q<Button>("StopButton");
            
            _checkMark = Resources.Load<Texture2D>("Polaris/icon _Check_");
            _leftArrow = Resources.Load<Texture2D>("Polaris/leftarrow");
            
            _slideButton.clickable.clicked += ToggleSlide;
            _stopButton.clickable.clicked += StopClicked;

            userManager = UserManager.getInstance();
            _locationManager = LocationManager.getInstance();
            suggestedLocations = new Queue<string>(numSuggestions);
            
            // PersistData.StopLocations.Push(new Vector3(16, -930, 239));
            // PersistData.StopLocations.Push(new Vector3(115, -970, 59));
            // PersistData.StopNames.Push("Classroom 2");
            // PersistData.StopNames.Push("Another building");
            
            var stopLocations = PersistData.StopLocations.ToArray();
            var stopNames = PersistData.StopNames.ToArray();
            for (var i = 0; i < stopLocations.Length; i++)
            {
                PlaceMarker(stopLocations[i], stopNames[i], false, true);
            }
        }

        private void Update()
        {
            if (MenUI_Panels.userOnListView) return;
            
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                switch (touch.phase)
                {
                    // Uncomment below if you want to reset when the touch was moved
                    //case TouchPhase.Moved:
                    case TouchPhase.Began:
                        _pressTime = 0;
                        break;

                    case TouchPhase.Stationary:
                        _pressTime += Time.deltaTime;
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (_pressTime > 0.5f)
                        {
                            if (PersistData.Routing)
                            {
                                Debug.Log("Please Wait for Results or Cancel");
                                return;
                            }

                            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

                            if (Physics.Raycast(ray, out var hit))
                            {
                                var locationName = hit.transform.name;

                                PlaceMarker(hit.point, GetBuilding.ToTitleCase(locationName[4..]), false, false);
                            }
                        }
                        _pressTime = 0;
                        break;
                    case TouchPhase.Moved:
                    default:
                        break;
                }
            }

            if (_shouldClose != _lastShouldClose)
            {
                _shouldClose = _lastShouldClose = false;
                StartCoroutine(ToggleRoutingBox(false, _routingBox, _closeTime));
            }

            RebaseRoute();
        }

        private async void PlaceMarker(Vector3 position, string locationName, bool adjustHeight, bool recreated)
        {
            var routeMarker = Instantiate(RouteMarker, position, Quaternion.identity, _arcGisMapComponent.transform);
            
            var locationComponent = routeMarker.GetComponent<ArcGISLocationComponent>();
            locationComponent.enabled = true;
            locationComponent.Rotation = new ArcGISRotation(0, 180, 0);
            
            if (adjustHeight)
            {
                SetElevation(routeMarker, 30f);
            }
            else
            {
                locationComponent.Position = Vector3ToGeoPosition(position, 30);
            }

            _stops.Push(routeMarker);

            if (!recreated)
            {
                PersistData.StopLocations.Push(routeMarker.transform.position);
                PersistData.StopNames.Push(locationName.StartsWith("Isgameobject_") 
                                               ? $"{locationComponent.Position.Y:00.00000}, {locationComponent.Position.X:00.00000}" 
                                               : locationName);
            }

            if (_stops.Count > StopCount)
            {
                RemoveLastMarker();
            }

            if (_stops.Count == 1)
            {
                var len = PersistData.StopLocations.Count;
                PersistData.DestPoint = PersistData.StopLocations.ToArray()[len - 1];
                PersistData.DestName = PersistData.StopNames.ToArray()[len - 1];
                PersistData.UsingCurrent = false;

                UpdateRouteInfoIncomplete();
                StartCoroutine(ToggleRoutingBox(true, _routingBox, _closeTime));
            }

            if (_stops.Count == StopCount)
            {
                StartCoroutine(ToggleRoutingBox(false, _routingBox, _closeTime));
                _closeTime = Time.time;
                
                PersistData.SrcName = PersistData.StopNames.Peek();
                // No filthy coordinates or weird locations in my suggestions
                if (!PersistData.DestName.Contains(',') 
                    && !PersistData.DestName.Equals("Other") 
                    && !PersistData.DestName.Equals("Virtual")) HandleSuggestedLocations(PersistData.DestName);

                var results = PersistData.RoutingString;
                if (!recreated)
                {
                    results = await FetchRoute(_stops.ToArray());
                    PersistData.RoutingString = results;
                }

                if (results.Contains("error"))
                {
                    DisplayError(results);
                }
                else
                {
                    StartCoroutine(DrawRoute(results, !PersistData.UsingCurrent));
                }
            }
        }

        private void RemoveLastMarker()
        {
            Destroy(_stops.Pop());
            PersistData.StopLocations.Pop();
            PersistData.StopNames.Pop();
        }

        /// <summary>
        /// Return GeoPosition Based on RaycastHit; I.E. Where the user clicked in the Scene.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="yOffset"></param>
        /// <returns></returns>
        private ArcGISPoint Vector3ToGeoPosition(Vector3 point, float yOffset = 0)
        {
            var worldPosition = math.inverse(_arcGisMapComponent.WorldMatrix).HomogeneousTransformPoint(point.ToDouble3());

            var geoPosition = _arcGisMapComponent.View.WorldToGeographic(worldPosition);
            var offsetPosition = new ArcGISPoint(geoPosition.X, geoPosition.Y, geoPosition.Z + yOffset, geoPosition.SpatialReference);
            
            return GeoUtils.ProjectToSpatialReference(offsetPosition, new ArcGISSpatialReference(4326));
        }

        private static void DisplayError(string errorText)
        {
            var error = JObject.Parse(errorText).SelectToken("error");
            var message = error.SelectToken("message");

            Debug.LogError($"Routing Error: {message}");
        }

        private async Task<string> FetchRoute(GameObject[] stops)
        {
            if (stops.Length != StopCount)
                return "";

            var jsonTravelModeAsset = Resources.Load("travelMode") as TextAsset;
            if (jsonTravelModeAsset == null)
            {
                Debug.LogError("ERROR - Could not find travelMode asset");
            }
            
            var jsonTravelModeText = JObject.Parse(jsonTravelModeAsset.text).ToString();

            IEnumerable<KeyValuePair<string, string>> payload = new List<KeyValuePair<string, string>>
            {
                new ("stops", GetRouteString(stops)),
                new ("returnRoutes", "true"),
                new ("token", _arcGisMapComponent.APIKey),
                new ("f", "json"),
                new ("travelMode", jsonTravelModeText)
            };

            HttpContent content = new FormUrlEncodedContent(payload);

            var response = await _client.PostAsync(RoutingURL, content);
            response.EnsureSuccessStatusCode();

            var results = await response.Content.ReadAsStringAsync();
            print(results);
            return results;
        }

        private static string GetRouteString(GameObject[] stops)
        {
            var startGp = stops[0].GetComponent<ArcGISLocationComponent>().Position;
            var endGp = stops[1].GetComponent<ArcGISLocationComponent>().Position;

            var startString = $"{startGp.X}, {startGp.Y}";
            var endString = $"{endGp.X}, {endGp.Y}";
        
            return $"{startString};{endString}";
        }

        private GameObject CreateBreadCrumb(float lat, float lon)
        {
            var breadcrumb = Instantiate(RouteBreadcrumb, _arcGisMapComponent.transform);

            breadcrumb.name = "Breadcrumb";

            var location = breadcrumb.AddComponent<ArcGISLocationComponent>();
            location.Position = new ArcGISPoint(lat, lon, ElevationOffset, new ArcGISSpatialReference(4326));

            return breadcrumb;
        }

        private IEnumerator DrawRoute(string routeInfo, bool includeSrc)
        {
            ClearRoute(false);

            var info = JObject.Parse(routeInfo);
            var routes = info.SelectToken("routes");
            var features = routes?.SelectToken("features");

            UpdateRouteInfo(info, includeSrc);

            foreach (var feature in features)
            {
                var geometry = feature.SelectToken("geometry");
                var paths = geometry?.SelectToken("paths")?[0];

                var pathList = new List<double2>();

                foreach(var path in paths)
                {
                    _breadcrumbs.Add(CreateBreadCrumb((float)path[0], (float)path[1]));
                    pathList.Add(new double2((double)path[1], (double)path[0]));

                    yield return null;
                    yield return null;
                }

                PersistData.PathPoints = pathList;
            }

            SetBreadcrumbHeight();

            // need a frame for location component updates to occur
            yield return null;
            yield return null;

            RenderLine();
        }

        // Does a raycast to get the elevation for each point.  For routes covering long distances the raycast will only hit elevation that is actively loaded. If you are doing 
        // something like this the raycast needs to happen dynamically when the data is loaded. This can be accomplished by only raycasting for breadcrums within a distance of the camera.
        private void SetBreadcrumbHeight()
        {
            foreach (var t in _breadcrumbs)
            {
                SetElevation(t, ElevationOffset);
            }
        }

        // Does a raycast to find the ground
        private void SetElevation(GameObject breadcrumb, float offset)
        {
            // start the raycast in the air at an arbitrary to ensure it is above the ground
            const int raycastHeight = 500;
            var position = breadcrumb.transform.position;
            var raycastStart = new Vector3(position.x, raycastHeight, position.z);
            
            if (!Physics.Raycast(raycastStart, Vector3.down, out var hitInfo)) return;
            
            var location = breadcrumb.GetComponent<ArcGISLocationComponent>();
            location.Position = Vector3ToGeoPosition(hitInfo.point, offset);
        }

        private void UpdateRouteInfo(JToken info, bool includeSrc)
        {
            var features = info.SelectToken("routes")?.SelectToken("features");
            var attributes = features?[0]?.SelectToken("attributes");

            PersistData.TravelMiles = (float)attributes?.SelectToken("Total_Miles");

            var summary = info.SelectToken("directions")?[0]?.SelectToken("summary");
            PersistData.TravelMinutes = (float)summary?.SelectToken("totalTime");

            print($"Time: {PersistData.TravelMinutes:0.00} Minutes, Distance: {PersistData.TravelMiles:0.00} Miles");

            _routingInfoLabel.text = GetUpdatedRouteText(0);
            _routingSrcLabel.text = PersistData.SrcName;
            _routingDestLabel.text = PersistData.DestName;
            
            _routingSrcBox.ToggleDisplayStyle(includeSrc);
            StartCoroutine(ToggleRoutingBox(true, _routingBox, _closeTime));
        }

        private void UpdateRouteInfoIncomplete()
        {
            _routingInfoLabel.text = "Hold to choose start";
            _routingSrcLabel.text = "Current Location";
            _routingDestLabel.text = PersistData.DestName;
            
            _slideButton.style.backgroundImage = _checkMark;
            _slideIsCheck = true;
            _routingSrcBox.ToggleDisplayStyle(true);
        }

        private void ClearRoute(bool markers)
        {
            if (markers)
            {
                foreach (var stop in _stops)
                {
                    Destroy(stop);
                }
                _stops.Clear();
                PersistData.ClearStops();
                _shouldClose = true;
            }

            foreach (var breadcrumb in _breadcrumbs)
                Destroy(breadcrumb);
            _breadcrumbs.Clear();

            if (_lineRenderer)
                _lineRenderer.positionCount = 0;

            _slideIsCheck = false;
            _slideButton.style.backgroundImage = _leftArrow;
        }

        private void RenderLine() 
        {
            if (_breadcrumbs.Count < 1) return;
            
            var allPoints = new List<Vector3>();

            foreach (var breadcrumb in _breadcrumbs)
            {
                if (breadcrumb.transform.position.Equals(Vector3.zero))
                {
                    Destroy(breadcrumb);
                    continue;
                }

                allPoints.Add(breadcrumb.transform.position);
            }

            _lineRenderer.positionCount = allPoints.Count;
            _lineRenderer.SetPositions(allPoints.ToArray());
            PersistData.Routing = true;
            
            // Default color
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(PathEnd, 0.0f), new GradientColorKey(PathEnd, 1f) },
                new[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) });
            _lineRenderer.colorGradient = gradient;
        }

        // The ArcGIS Rebase component
        private void RebaseRoute()
        {
            var rootPosition = _arcGisMapComponent.GetComponent<HPRoot>().RootUniversePosition;
            var delta = (_lastRootPosition - rootPosition).ToVector3();
            // if (!(delta.magnitude > 1)) return; // 1km

            if (LocationMarker == null)
            {
                Debug.LogError("No location marker!!");
            }

            if (_lineRenderer != null && _lineRenderer.positionCount > 1)
            {
                var points = new Vector3[_lineRenderer.positionCount];
                _lineRenderer.GetPositions(points);
                for (var i = 0; i < points.Length; i++)
                {
                    points[i] += delta;
                }
                _lineRenderer.SetPositions(points);

                if (PersistData.UsingCurrent)
                {
                    var closestPoint = GetClosestPathPoint(points);
                    var pointPercent = PointPercentage(points, closestPoint);
                    _routingInfoLabel.text = GetUpdatedRouteText(pointPercent);

                    var gradient = new Gradient();
                    gradient.SetKeys(
                        new[]
                        {
                            new GradientColorKey(PathStart, 0.0f),
                            new GradientColorKey(PathStart, pointPercent - 0.01f),
                            new GradientColorKey(PathEnd, pointPercent + 0.01f),
                            new GradientColorKey(PathEnd, 1f)
                        },
                        new[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
                    );
                    _lineRenderer.colorGradient = gradient;
                }
            }
            _lastRootPosition = rootPosition;
        }

        public static string GetUpdatedRouteText(float completionPercentage)
        {
            var minutes = PersistData.TravelMinutes * (1 - completionPercentage);
            var miles = PersistData.TravelMiles * (1 - completionPercentage);
                
            if (miles < 0.1)
            {
                var feet = (int)(miles * 5280);
                var feetRounded = (feet / 100) * 100;
                return $"Routing - {minutes:0} min. ({feetRounded} ft.)";
            }
            
            return $"Routing - {minutes:0} min. ({miles:0.0} mi.)";
        }

        public void RouteToEvent(EventData eventData)
        {
            LocationData location = null;
            foreach (var building in _locationManager.dataList)
            {
                // TODO: BUT WHAT IF EVENT DATA DOESNT HAVE A BUILDING LAT OR LONG?
                if (Math.Abs(building.BuildingLat - eventData.Location.BuildingLat) < 0.00001 &&
                    Math.Abs(building.BuildingLong - eventData.Location.BuildingLong) < 0.00001)
                {
                    location = building;
                    break;
                }
            }

            ClearRoute(true);

            var buildingName = location?.BuildingName ??
                               $"{eventData.Location.BuildingLat:00.00000}, {eventData.Location.BuildingLong:00.00000}";
            var geoPosition = new ArcGISPoint(eventData.Location.BuildingLong,
                                              eventData.Location.BuildingLat,
                                              0f,
                                              new ArcGISSpatialReference(4326));
            var worldPosition =
                _root.TransformPoint(_arcGisMapComponent.View.GeographicToWorld(geoPosition)).ToVector3();
            PlaceMarker(worldPosition,  buildingName, true, false);
            
            // TODO: MAKE IT SO CLICKING THE "CHOOSE STARTING POINT MENU" SELECTS USER CURRENT LOCATION
            if (GetUserCurrentLocation.displayLocation)
            {
                PlaceMarkerAtUserLocation();
            }
        }

        public void RouteToLocation(LocationData locationData)
        {
            LocationData location = locationData;

            ClearRoute(true);

            var buildingName = location?.BuildingName ??
                               $"{location.BuildingLat:00.00000}, {location.BuildingLong:00.00000}";
            var geoPosition = new ArcGISPoint(location.BuildingLong,
                                              location.BuildingLat,
                                              0f,
                                              new ArcGISSpatialReference(4326));
            var worldPosition =
                _root.TransformPoint(_arcGisMapComponent.View.GeographicToWorld(geoPosition)).ToVector3();
            PlaceMarker(worldPosition, buildingName, true, false);

            // TODO: MAKE IT SO CLICKING THE "CHOOSE STARTING POINT MENU" SELECTS USER CURRENT LOCATION
            if (GetUserCurrentLocation.displayLocation)
            {
                PlaceMarkerAtUserLocation();
            }
        }

        private void PlaceMarkerAtUserLocation()
        {
            PersistData.UsingCurrent = true;
            
            if (LocationMarker)
            {
                PlaceMarker(LocationMarker.transform.position, "Current Location", true, false);
            }
            else
            {
                var curPosition = new ArcGISPoint(GetUserCurrentLocation._longitude,
                                                  GetUserCurrentLocation._latitude,
                                                  0f,
                                                  new ArcGISSpatialReference(4326));
                var curWorldPosition =
                    _root.TransformPoint(_arcGisMapComponent.View.GeographicToWorld(curPosition)).ToVector3();
                PlaceMarker(curWorldPosition, "Current Location", true, false);
            }
        }
        

        private void ToggleSlide()
        {
            if (_slideIsCheck)
            {
                PlaceMarkerAtUserLocation();
                _slideButton.style.backgroundImage = _leftArrow;
                return;
            }
            
            _closed = !_closed;
            _routingBox.style.left = Length.Percent(_closed ? -67f : -5f);
            _slideButton.style.rotate = new Rotate(_closed ? 180 : 0);
            _stopButton.style.width = _stopButton.style.height = _closed ? 0 : 100;
        }

        private void StopClicked()
        {
            ClearRoute(true);
        }

        public static IEnumerator ToggleRoutingBox(bool routing, VisualElement routingBox, float closeTime)
        {
            if (routing)
            {
                var timeDiff = Time.time - closeTime;
                var waitTime = timeDiff < 0.5f ? 0.5f - timeDiff : 0;
                yield return new WaitForSeconds(waitTime);

                routingBox.style.left = Length.Percent(-80f);
                routingBox.ToggleDisplayStyle(true);
                routingBox.style.left = Length.Percent(-5f);
            }
            else
            {
                routingBox.style.left = Length.Percent(-80f);
                yield return new WaitForSeconds(0.3f);
                routingBox.ToggleDisplayStyle(false);
            }
        }

        private int GetClosestPathPoint(Vector3[] points)
        {
            var locPos = LocationMarker.transform.position;

            var smallestDist = float.MaxValue;
            var smallestIndex = 0;
            for (var i = 0; i < points.Length; i++)
            {
                var dist = Vector2.Distance(
                    new Vector2(points[i].x, points[i].z), 
                    new Vector2(locPos.x, locPos.z));
                if (dist < smallestDist)
                {
                    smallestDist = dist;
                    smallestIndex = i;
                }
            }

            // End route when less than 25m from destination
            var endDist = Vector2.Distance(
                new Vector2(points[^1].x, points[^1].z),
                new Vector2(locPos.x, locPos.z));
            if (endDist < 25)
            {
                ClearRoute(true);
                // TODO: Play animation
            }

            // Auto reroute
            if (PersistData.UsingCurrent && smallestDist > RerouteDist && Time.time - _lastRerouteTime > 5)
            {
                Debug.Log("Should recalculate route! - Smallest dist is " + smallestDist);
                _lastRerouteTime = Time.time;
                
                RemoveLastMarker();
                PlaceMarkerAtUserLocation();
            }

            return smallestIndex;
        }
        
        public static float PointPercentage(IReadOnlyList<Vector3> linePositions, int point)
        {
            var totalDist = 0f;
            var pointDist = 0f;
            var lastPosition = linePositions[0];
            for (var i = 1; i < linePositions.Count; i++)
            {
                var segDist = Vector3.Distance(lastPosition, linePositions[i]);
                
                if (i <= point)
                {
                    pointDist += segDist;
                }
                totalDist += segDist;
                
                lastPosition = linePositions[i];
            }
            
            return pointDist / totalDist;
        }

        private string QueueToString(Queue<string> queue)
        {
            return string.Join(", ", queue);
        }

        private void HandleSuggestedLocations(string suggestion)
        {
            if (userManager.data.Suggested != null) suggestedLocations = new Queue<string>(userManager.data.Suggested.Split("~").Reverse());
            print("Suggested locations: " + QueueToString(suggestedLocations));
            
            // Remove duplicates
            suggestedLocations = new Queue<string>(suggestedLocations.Where(x => x != suggestion));
            // Keep capacity at 3
            if (suggestedLocations.Count >= numSuggestions) suggestedLocations.Dequeue();
            // Add suggestion
            suggestedLocations.Enqueue(suggestion);
            
            // Use ~ as separator because some strings have commas, spaces, etc.
            userManager.data.Suggested = string.Join("~", suggestedLocations.Reverse());
            print("Suggested string: " + userManager.data.Suggested);
        }
    }
}
