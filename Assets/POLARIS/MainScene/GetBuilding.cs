using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Esri.HPFramework;
using POLARIS.Managers;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using static System.Char;

namespace POLARIS.MainScene
{
    public class GetBuilding : MonoBehaviour
    {
        public Color BuildingSelect;
        
        private Camera _mainCamera;
        private Label _uiDocLabel;

        private float _lastTapTime = 0;
        private const float DoubleTapThreshold = 0.3f;

        private GameObject _lastSelected;
        private Color _lastColor;
        
        private ArcGISMapComponent _arcGisMapComponent;
        private LocationManager locationManager;
        private MenUI_Panels menUI;

        private void Start()
        {
            locationManager = LocationManager.getInstance();
            _mainCamera = Camera.main;
            _uiDocLabel = gameObject.GetComponent<UIDocument>().rootVisualElement.Q<Label>("BuildingTopLabel");
            _arcGisMapComponent = FindObjectOfType<ArcGISMapComponent>();
            menUI = GameObject.Find("MenuUI").GetComponent<MenUI_Panels>();
        }
        
        private void Update()
        {
            // Double tap to view building name
            if (Input.touchCount != 1) return;
            
            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began) return;

            var hit = MapRaycast(touch);
            if (hit.transform != null)
            {
                var locationName = hit.transform.name;

                print("My object is clicked by mouse " + locationName);

                if (string.IsNullOrWhiteSpace(locationName) ||
                    locationName.StartsWith("ArcGISGameObject_") ||
                    locationName.Length <= 4)
                {
                    return;
                }

                if (Time.time - _lastTapTime <= DoubleTapThreshold)
                {
                    _lastTapTime = 0;
                    // UnsetLastBuildingColor();

                    var buildingName = ToTitleCase(locationName[4..]);
                    _uiDocLabel.text = $"{buildingName}";
                    var closestBuilding = GetClosestBuilding(buildingName);
                    if (closestBuilding != null) Debug.Log(
                        $"Name: {closestBuilding.BuildingName ?? ""}\n" +
                        $"Aliases: {((closestBuilding.BuildingAllias != null) ? string.Join(", ", closestBuilding.BuildingAllias) : "")}\n" +
                        $"Abbreviations: {((closestBuilding.BuildingAbbreviation != null) ? string.Join(", ", closestBuilding.BuildingAbbreviation) : "")}\n" +
                        $"Description: {closestBuilding.BuildingDesc ?? ""}\n" +
                        $"Longitude: {closestBuilding.BuildingLong}\n" +
                        $"Latitude: {closestBuilding.BuildingLat}\n" +
                        $"Address: {closestBuilding.BuildingAddress ?? ""}\n" +
                        $"Events: {((closestBuilding.BuildingEvents != null) ? string.Join(", ", closestBuilding.BuildingEvents) : "")}\n");

                    UpdateBuildingColor(hit.transform.gameObject, BuildingSelect);
                }
                else
                {
                    _lastTapTime = Time.time;
                }
            }
        }

        private RaycastHit MapRaycast(Touch touch)
        {
            var ray = _mainCamera.ScreenPointToRay(touch.position);
            
            if (!Physics.Raycast(ray, out var hit)) return new RaycastHit();
                        
            var worldPosition = math.inverse(_arcGisMapComponent.WorldMatrix).HomogeneousTransformPoint(hit.point.ToDouble3());
            var geoPosition = _arcGisMapComponent.View.WorldToGeographic(worldPosition);
            var offsetPosition = new ArcGISPoint(geoPosition.X, geoPosition.Y, geoPosition.Z, geoPosition.SpatialReference);
            var coords = GeoUtils.ProjectToSpatialReference(offsetPosition, new ArcGISSpatialReference(4326));
            Debug.Log($"Hit position: {coords.X}, {coords.Y}, {coords.Z}");

            UnsetLastBuildingColor();

            return hit;
        }

        private void UpdateBuildingColor(GameObject buildingPart, Color color)
        {
            var building = buildingPart.transform.parent.gameObject;
            _lastColor = building.GetComponentInChildren<MeshRenderer>().material.color;
            _lastSelected = building;
            
            foreach (var mesh in building.GetComponentsInChildren<MeshRenderer>())
            {
                mesh.material.color = color;
            }
        }

        private void UnsetLastBuildingColor()
        {
            if (!_lastSelected) return;
            
            foreach (var mesh in _lastSelected.GetComponentsInChildren<MeshRenderer>())
            {
                mesh.material.color = _lastColor;
            }
        }

        public static string ToTitleCase(string stringToConvert)
        {
            return new string(ToTitleCaseEnumerable(stringToConvert).ToArray());
        }
        
        private static IEnumerable<char> ToTitleCaseEnumerable(string stringToConvert)
        {
            var newWord = true;
            var prevLetterI = false;
            foreach (var c in stringToConvert)
            {
                if (newWord)
                {
                    yield return ToUpper(c);
                    newWord = false;
                    prevLetterI = c == 'I';
                }
                else
                {
                    if (prevLetterI)
                    {
                        if (c == 'I') yield return ToUpper(c);
                        else yield return ToLower(c);
                    }
                    else
                    {
                        yield return ToLower(c);
                    }
                }

                if (c == ' ')
                {
                    newWord = true;
                    prevLetterI = false;
                }
            }
        }

        private LocationData GetClosestBuilding(string raycastHitName)
        {
            foreach (LocationData building in locationManager.dataList)
            {
                if (string.Equals(raycastHitName, building.BuildingName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return building;
                }

                if (building.BuildingAllias == null) continue;
                foreach (string alias in building.BuildingAllias)
                {
                    if (String.Equals(raycastHitName, alias,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return building;
                    }
                }
            }

            return null;
        }
    }
}