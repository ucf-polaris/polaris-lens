// Copyright 2022 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Esri.HPFramework;
using Newtonsoft.Json.Linq;
using POLARIS.MainScene;
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

        private Label _routingDestLabel;
        private Label _routingInfoLabel;

        private HPRoot _hpRoot;
        private ArcGISMapComponent _arcGisMapComponent;

        private const float ElevationOffset = 2.0f;

        private const int StopCount = 2;
        private readonly Queue<GameObject> _stops = new Queue<GameObject>();
        private bool _routing = false;
        private const string RoutingURL = "https://route-api.arcgis.com/arcgis/rest/services/World/Route/NAServer/Route_World/solve";
        private readonly List<GameObject> _breadcrumbs = new List<GameObject>();

        private LineRenderer _lineRenderer;

        private readonly HttpClient _client = new HttpClient();

        private double3 _lastRootPosition;

        private Camera _mainCamera;

        private string _startName;
        private string _destName;
        private readonly Queue<string> _stopNames = new Queue<string>();

        private float pressTime = 0;

        private void Start()
        {
            // We need HPRoot for the HitToGeoPosition Method
            _hpRoot = FindObjectOfType<HPRoot>();

            // We need this ArcGISMapComponent for the FromCartesianPosition Method
            // defined on the ArcGISMapComponent.View
            _arcGisMapComponent = FindObjectOfType<ArcGISMapComponent>();

            _lineRenderer = Route.GetComponent<LineRenderer>();

            _lastRootPosition = _arcGisMapComponent.GetComponent<HPRoot>().RootUniversePosition;

            _mainCamera = Camera.main;

            var rootVisual = RouteInfo.GetComponent<UIDocument>().rootVisualElement;
            _routingDestLabel = rootVisual.Q<Label>("RoutingDest");
            _routingInfoLabel = rootVisual.Q<Label>("RoutingInfos");
        }
        
        private async void Update()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                switch (touch.phase)
                {
                    // Uncomment below if you want to reset when the touch was moved
                    //case TouchPhase.Moved:
                    case TouchPhase.Began:
                        pressTime = 0;
                        break;

                    case TouchPhase.Stationary:
                        pressTime += Time.deltaTime;
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (pressTime > 0.5f)
                        {
                            if (_routing)
                            {
                                Debug.Log("Please Wait for Results or Cancel");
                                return;
                            }

                            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

                            if (Physics.Raycast(ray, out var hit))
                            {
                                var routeMarker = Instantiate(RouteMarker, hit.point, Quaternion.identity, _arcGisMapComponent.transform);

                                var geoPosition = HitToGeoPosition(hit);

                                var locationComponent = routeMarker.GetComponent<ArcGISLocationComponent>();
                                locationComponent.enabled = true;
                                locationComponent.Position = geoPosition;
                                locationComponent.Rotation = new ArcGISRotation(0, 90, 0);

                                _stops.Enqueue(routeMarker);
                                var locationName = hit.transform.name;
                                _stopNames.Enqueue(locationName.StartsWith("ArcGISGameObject_") ? "Point" : GetBuilding.ToTitleCase(locationName[4..]));

                                if (_stops.Count > StopCount)
                                {
                                    Destroy(_stops.Dequeue());
                                    _stopNames.Dequeue();
                                }
                    
                                if (_stops.Count == StopCount)
                                {
                                    var stopNamesArray = _stopNames.ToArray();
                                    _startName = stopNamesArray[0];
                                    _destName = stopNamesArray[1];
                                    _routing = true;

                                    var results = await FetchRoute(_stops.ToArray());

                                    if (results.Contains("error"))
                                    {
                                        DisplayError(results);
                                    }
                                    else
                                    {
                                        StartCoroutine(DrawRoute(results));
                                    }

                                    _routing = false;
                                }
                            }
                        }
                        pressTime = 0;
                        break;
                }
            }

            RebaseRoute();
        }

        /// <summary>
        /// Return GeoPosition Based on RaycastHit; I.E. Where the user clicked in the Scene.
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="yOffset"></param>
        /// <returns></returns>
        private ArcGISPoint HitToGeoPosition(RaycastHit hit, float yOffset = 0)
        {
            var worldPosition = math.inverse(_arcGisMapComponent.WorldMatrix).HomogeneousTransformPoint(hit.point.ToDouble3());

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
            var jsonTravelModeText = JObject.Parse(jsonTravelModeAsset.text).ToString();

            IEnumerable<KeyValuePair<string, string>> payload = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("stops", GetRouteString(stops)),
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

        private IEnumerator DrawRoute(string routeInfo)
        {
            ClearRoute();

            var info = JObject.Parse(routeInfo);
            var routes = info.SelectToken("routes");
            var features = routes.SelectToken("features");

            UpdateRouteInfo(info);

            foreach (var feature in features)
            {
                var geometry = feature.SelectToken("geometry");
                var paths = geometry.SelectToken("paths")[0];

                var pathList = new List<double[]>{};

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
                SetElevation(t);
            }
        }

        // Does a raycast to find the ground
        private void SetElevation(GameObject breadcrumb)
        {
            // start the raycast in the air at an arbitrary to ensure it is above the ground
            const int raycastHeight = 1000;
            var position = breadcrumb.transform.position;
            var raycastStart = new Vector3(position.x, position.y + raycastHeight, position.z);
            
            if (!Physics.Raycast(raycastStart, Vector3.down, out var hitInfo)) return;
            
            var location = breadcrumb.GetComponent<ArcGISLocationComponent>();
            location.Position = HitToGeoPosition(hitInfo, ElevationOffset);
        }

        private void UpdateRouteInfo(JToken info)
        {
            var features = info.SelectToken("routes").SelectToken("features");
            var attributes = features[0].SelectToken("attributes");

            var travelMiles = (float)attributes.SelectToken("Total_Miles");

            var summary = info.SelectToken("directions")[0].SelectToken("summary");
            var travelMinutes = (float)summary.SelectToken("totalTime");

            print($"Time: {travelMinutes:0.00} Minutes, Distance: {travelMiles:0.00} Miles");

            _routingDestLabel.text = $"Routing from {_startName} to {_destName}";
            _routingInfoLabel.text = $"{travelMinutes:0} min, {travelMiles:0.0} miles";
        }

        private void ClearRoute()
        {
            foreach (var breadcrumb in _breadcrumbs)
                Destroy(breadcrumb);

            _breadcrumbs.Clear();

            if (_lineRenderer)
                _lineRenderer.positionCount = 0;
        }

        private void RenderLine() 
        {
            if (_breadcrumbs.Count < 1) return;

            _lineRenderer.widthMultiplier = 5;

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
        }

        // The ArcGIS Rebase component
        private void RebaseRoute()
        {
            var rootPosition = _arcGisMapComponent.GetComponent<HPRoot>().RootUniversePosition;
            var delta = (_lastRootPosition - rootPosition).ToVector3();
            if (!(delta.magnitude > 1)) return; // 1km
            
            if (_lineRenderer != null)
            {
                var points = new Vector3[_lineRenderer.positionCount];
                _lineRenderer.GetPositions(points);
                for (var i = 0; i < points.Length; i++)
                {
                    points[i] += delta;
                }
                _lineRenderer.SetPositions(points);
            }
            _lastRootPosition = rootPosition;
        }
    }
}
