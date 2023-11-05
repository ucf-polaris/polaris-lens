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

        private Label _routingSrcLabel;
        private Label _routingDestLabel;
        private Label _routingInfoLabel;
        private GroupBox _routingSrcBox;
        private VisualElement _routingBox;
        private Button _slideButton;
        private Button _stopButton;
        
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

        private string _srcName;
        private string _destName;
        private readonly Stack<string> _stopNames = new();

        private bool _destSelected = false;
        private bool _lastDestSelected = true;
        private bool _routing = false;
        private float _pressTime = 0;
        private bool _closed = false;

        private float _travelMinutes = 0f;
        private float _travelMiles = 0f;

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
            
            _slideButton.clickable.clicked += ToggleSlide;
            _stopButton.clickable.clicked += StopClicked;

            userManager = UserManager.getInstance();
            _locationManager = LocationManager.getInstance();
            suggestedLocations = new Queue<string>(numSuggestions);
        }

        private async void Update()
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
                            if (_routing)
                            {
                                Debug.Log("Please Wait for Results or Cancel");
                                return;
                            }

                            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

                            if (Physics.Raycast(ray, out var hit))
                            {
                                var locationName = hit.transform.name;

                                PlaceMarker(hit.point, GetBuilding.ToTitleCase(locationName[4..]), false);
                            }
                        }
                        _pressTime = 0;
                        break;
                    case TouchPhase.Moved:
                    default:
                        break;
                }
            }

            if (_destSelected != _lastDestSelected)
            {
                _lastDestSelected = _destSelected;
                StartCoroutine(ToggleRouting(_destSelected));
            }

            RebaseRoute();
        }

        private async void PlaceMarker(Vector3 position, string locationName, bool adjustHeight)
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
            _stopNames.Push(locationName.StartsWith("Isgameobject_") ? $"{locationComponent.Position.Y:00.00000}, {locationComponent.Position.X:00.00000}" : locationName);

            if (_stops.Count > StopCount)
            {
                Destroy(_stops.Pop());
                _stopNames.Pop();
            }

            if (_stops.Count == 1)
            {
                _destName = _stopNames.Peek();
                UpdateRouteInfoIncomplete();
                _destSelected = true;
            }

            if (_stops.Count == StopCount)
            {
                var stopNamesArray = _stopNames.ToArray();
                _srcName = stopNamesArray[0];
                _destName = stopNamesArray[1];
                // No filthy coordinates in my suggestions
                if (!_destName.Contains(',')) HandleSuggestedLocations(_destName);

                var results = await FetchRoute(_stops.ToArray());

                if (results.Contains("error"))
                {
                    DisplayError(results);
                }
                else
                {
                    StartCoroutine(DrawRoute(results, !adjustHeight));
                }
            }
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

            var spatialRef = GeoUtils.ProjectToSpatialReference(offsetPosition, new ArcGISSpatialReference(4326));
            PersistData.DestinationPoint = spatialRef;
                
            return GeoUtils.ProjectToSpatialReference(offsetPosition, new ArcGISSpatialReference(4326));
        }

        private static void DisplayError(string errorText)
        {
            var error = JObject.Parse(errorText).SelectToken("error");
            var message = error.SelectToken("message");

            print($"Error: {message}");
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

                var pathList = new List<double[]>();

                foreach(var path in paths)
                {
                    _breadcrumbs.Add(CreateBreadCrumb((float)path[0], (float)path[1]));
                    pathList.Add(new[]{(double)path[0], (double)path[1]});

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

            _travelMiles = (float)attributes?.SelectToken("Total_Miles");

            var summary = info.SelectToken("directions")?[0]?.SelectToken("summary");
            _travelMinutes = (float)summary?.SelectToken("totalTime");

            print($"Time: {_travelMinutes:0.00} Minutes, Distance: {_travelMiles:0.00} Miles");

            _routingInfoLabel.text = $"Routing - {_travelMinutes:0} min. ({_travelMiles:0.0} mi.)";
            _routingSrcLabel.text = _srcName;
            _routingDestLabel.text = _destName;
            
            _routingSrcBox.ToggleDisplayStyle(includeSrc);

            PersistData.SrcName = _srcName;
            PersistData.DestName = _destName;
            PersistData.TravelMiles = _travelMiles;
            PersistData.TravelMinutes = _travelMinutes;
        }

        private void UpdateRouteInfoIncomplete()
        {
            _routingInfoLabel.text = "Choose starting point...";
            _routingSrcLabel.text = "Waiting";
            _routingDestLabel.text = _destName;
            
            _routingSrcBox.ToggleDisplayStyle(false);
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
                _stopNames.Clear();
                
                _routing = _destSelected = false;
                PersistData.Routing = false;
            }

            foreach (var breadcrumb in _breadcrumbs)
                Destroy(breadcrumb);
            _breadcrumbs.Clear();

            if (_lineRenderer)
                _lineRenderer.positionCount = 0;
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
            _routing = true;
            PersistData.Routing = true;
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
                var closestPoint = GetClosestPathPoint(points);
                var pointPercent = PointPercentage(points, closestPoint);
                _routingInfoLabel.text = $"Routing - {(_travelMinutes*pointPercent):0} min. ({(_travelMiles*pointPercent):0.0} mi.)";

                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] {new GradientColorKey(PathStart, 0.0f), new GradientColorKey(PathStart, pointPercent - 0.01f), new GradientColorKey(PathEnd, pointPercent + 0.01f), new GradientColorKey(PathEnd, 1f)},
                    new[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
                    );
                _lineRenderer.colorGradient = gradient;
            }
            _lastRootPosition = rootPosition;
        }

        public void RouteToEvent(EventData eventData)
        {
            LocationData location = null;
            foreach (var building in _locationManager.dataList)
            {
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
            PlaceMarker(worldPosition,  buildingName, true);

            var curPosition = new ArcGISPoint(GetUserCurrentLocation._longitude,
                                              GetUserCurrentLocation._latitude,
                                              0f,
                                              new ArcGISSpatialReference(4326));
            var curWorldPosition =
                _root.TransformPoint(_arcGisMapComponent.View.GeographicToWorld(curPosition)).ToVector3();
            PlaceMarker(curWorldPosition, "My Location", true);
        }

        private void ToggleSlide()
        {
            _closed = !_closed;
            _routingBox.style.left = Length.Percent(_closed ? -67f : -5f);
            _slideButton.style.rotate = new Rotate(_closed ? 180 : 0);
            _stopButton.style.width = _stopButton.style.height = _closed ? 0 : 100;
        }

        private void StopClicked()
        {
            ClearRoute(true);
        }

        private IEnumerator ToggleRouting(bool routing)
        {
            if (routing)
            {
                _routingBox.style.left = Length.Percent(-80f);
                _routingBox.ToggleDisplayStyle(true);
                _routingBox.style.left = Length.Percent(-5f);
            }
            else
            {
                _routingBox.style.left = Length.Percent(-80f);
                yield return new WaitForSeconds(0.3f);
                _routingBox.ToggleDisplayStyle(false);
            }
        }

        private int GetClosestPathPoint(Vector3[] points)
        {
            var smallestDist = float.MaxValue;
            var smallestIndex = 0;
            for (var i = 0; i < points.Length; i++)
            {
                var dist = Vector3.Distance(points[i], LocationMarker.transform.position);
                if (dist < smallestDist)
                {
                    smallestDist = dist;
                    smallestIndex = i;
                }
            }

            if (smallestDist > 50) // Dont know what dist this is
            {
                // TODO: Auto reroute
                // Debug.Log("Should recalculate route! - Smallest dist is " + smallestDist);
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
