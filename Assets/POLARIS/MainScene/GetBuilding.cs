using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Esri.HPFramework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using static System.Char;

namespace POLARIS.MainScene
{
    public class GetBuilding : MonoBehaviour
    {
        private Camera _mainCamera;
        private Label _uiDocLabel;

        private float lastTapTime = 0;
        private float doubleTapThreshold = 0.3f;
        
        private ArcGISMapComponent _arcGisMapComponent;


        private void Start()
        {
            _mainCamera = Camera.main;
            _uiDocLabel = gameObject.GetComponent<UIDocument>().rootVisualElement.Q<Label>("BuildingTopLabel");
            _arcGisMapComponent = FindObjectOfType<ArcGISMapComponent>();
        }
        
        private void Update()
        {
            // Double tap to view building name
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    if (Time.time - lastTapTime <= doubleTapThreshold)
                    {
                        lastTapTime = 0;
                        
                        var ray = _mainCamera.ScreenPointToRay(touch.position);

                        if (!Physics.Raycast(ray, out var hit)) return;
                        
                        var worldPosition = math.inverse(_arcGisMapComponent.WorldMatrix).HomogeneousTransformPoint(hit.point.ToDouble3());
                        var geoPosition = _arcGisMapComponent.View.WorldToGeographic(worldPosition);
                        var offsetPosition = new ArcGISPoint(geoPosition.X, geoPosition.Y, geoPosition.Z, geoPosition.SpatialReference);
                        var coords = GeoUtils.ProjectToSpatialReference(offsetPosition, new ArcGISSpatialReference(4326));
                        
                        var locationName = hit.transform.name;
                        Debug.Log($"Hit position: {coords.X}, {coords.Y}, {coords.Z}");
                        print("My object is clicked by mouse " + locationName);
                        if (!string.IsNullOrWhiteSpace(locationName) &&
                            !locationName.StartsWith("ArcGISGameObject_") && locationName.Length > 4)
                        {
                            var buildingName = ToTitleCase(locationName[4..]);
                            _uiDocLabel.text = $"{buildingName}";
                            var closestBuilding = GetClosestBuilding(buildingName);
                            Debug.Log(
                                $"Name: {closestBuilding.BuildingName ?? ""}\n" +
                                $"Aliases: {((closestBuilding.BuildingAllias != null) ? string.Join(", ", closestBuilding.BuildingAllias) : "")}\n" +
                                $"Abbreviations: {((closestBuilding.BuildingAbbreviation != null) ? string.Join(", ", closestBuilding.BuildingAbbreviation) : "")}\n" +
                                $"Description: {closestBuilding.BuildingDesc ?? ""}\n" +
                                $"Longitude: {closestBuilding.BuildingLong}\n" +
                                $"Latitude: {closestBuilding.BuildingLat}\n" +
                                $"Address: {closestBuilding.BuildingAddress ?? ""}\n" +
                                $"Events: {((closestBuilding.BuildingEvents != null) ? string.Join(", ", closestBuilding.BuildingEvents) : "")}\n");

                            StartCoroutine(ToggleLabelHeight());
                        }
                    }
                    else
                    {
                        lastTapTime = Time.time;
                    }
                }
            }
        }

        private IEnumerator ToggleLabelHeight()
        {
            _uiDocLabel.ToggleInClassList("RaisedLabel");
            _uiDocLabel.ToggleInClassList("BuildingTopLabel");
            yield return new WaitForSeconds(2.0f);
            _uiDocLabel.ToggleInClassList("RaisedLabel");
            _uiDocLabel.ToggleInClassList("BuildingTopLabel");
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

        public static Building GetClosestBuilding(String raycastHitName)
        {
            foreach (Building building in Locations.LocationList)
            {
                if (String.Equals(raycastHitName, building.BuildingName,
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

            return Locations.LocationList[0];
        }
    }
}