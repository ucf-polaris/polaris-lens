/* Copyright 2022 Esri
 *
 * Licensed under the Apache License Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Esri.HPFramework;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using POLARIS.Managers;

namespace POLARIS.MainScene
{
    public class Geocoder : MonoBehaviour
    {
        [FormerlySerializedAs("AddressMarkerTemplate")] public GameObject addressMarkerTemplate;
        [FormerlySerializedAs("AddressMarkerScale")] public float addressMarkerScale = 1;
        [FormerlySerializedAs("LocationMarkerTemplate")] public GameObject locationMarkerTemplate;
        [FormerlySerializedAs("LocationMarkerScale")] public float locationMarkerScale = 1;
        [FormerlySerializedAs("AddressCardTemplate")] public GameObject addressCardTemplate;
        [FormerlySerializedAs("SearchBar")] public GameObject searchBar;
        
        private TextField _searchField;
        private Button _searchButton;
        private Button _clearButton;

        private Camera _mainCamera;
        private ArcGISLocationComponent _cameraLocation;
        private double3 _oldCameraPosition;
        private double3 _newCameraPosition;
        private double3 _oldCameraRotation;
        private double3 _newCameraRotation;
        
        private GameObject _queryLocationGo;
        private ArcGISLocationComponent _queryLocationLocation;
        private ArcGISMapComponent _arcGisMapComponent;
        private string _responseAddress = "";
        private bool _shouldPlaceMarker = false;
        private bool _waitingForResponse = false;
        private float _timer = 0;
        private const float SlowLoadFactor = 1;
        
        private List<Building> _buildingSearchList = new List<Building>();
        private List<EventData> _eventSearchList = new List<EventData>();
        private ListView _buildingOrEventListView;
        private Action<VisualElement, int> bindLocationItem;
        private Action<VisualElement, int> bindEventItem;
        
        private string currentTab;
        private EventManager eventManager;
        private void Start()
        {
            eventManager = EventManager.getInstance();
            _arcGisMapComponent = FindObjectOfType<ArcGISMapComponent>();
            _mainCamera = Camera.main;
            _cameraLocation = _mainCamera.GetComponent<ArcGISLocationComponent>();
            
            SetupQueryLocationGameObject(addressMarkerTemplate, scale: new Vector3(addressMarkerScale, addressMarkerScale, addressMarkerScale));
            _queryLocationLocation = _queryLocationGo.GetComponent<ArcGISLocationComponent>();
            
            currentTab = ChangeTabImage._lastPressed;

            var rootVisual = searchBar.GetComponent<UIDocument>().rootVisualElement;

            _searchField = rootVisual.Q<TextField>("SearchBar");
            _searchField.RegisterValueChangedCallback(OnSearchValueChanged);
            _searchField.selectAllOnFocus = true;
            SetPlaceholderText(_searchField, currentTab == "location" ? "Search for locations" : "Search for events");
            
            Func<VisualElement> makeItem = () => new Label();
            bindLocationItem = (VisualElement element, int index) => {
                (element as Label).text = _buildingSearchList[index].BuildingName;
                (element as Label).RegisterCallback<ClickEvent>(_ => OnBuildingSearchClick(_buildingSearchList[index]));
                element.style.flexGrow = 0;
                element.style.color = Color.black;
                element.style.backgroundColor = Color.white;
                element.style.fontSize = 100f;
                element.style.marginBottom = 50f;
            };
            bindEventItem = (VisualElement element, int index) => {
                (element as Label).text = _eventSearchList[index].Name;
                (element as Label).RegisterCallback<ClickEvent>(_ => OnEventSearchClick(_eventSearchList[index]));
                element.style.flexGrow = 1;
                element.style.color = Color.black;
                element.style.backgroundColor = Color.white;
                element.style.fontSize = 40f;
            };
            _buildingOrEventListView = rootVisual.Q<ListView>("SearchResultBigLabel");
            _buildingOrEventListView.fixedItemHeight = 150f;
            _buildingOrEventListView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            _buildingOrEventListView.makeItem = makeItem;

            ScrollView SV = _buildingOrEventListView.Q<ScrollView>();

            SV.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _buildingOrEventListView.selectionType = SelectionType.Single;
            _buildingOrEventListView.style.flexGrow = 1;
            SV.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
            SV.scrollDecelerationRate = 0.25f;
            SV.elasticity = 0.01f;
        }

        private void Update()
        {
            if (ChangeTabImage._lastPressed != currentTab)
            {
                currentTab = ChangeTabImage._lastPressed;
                _searchField.value = "";
                ClearSearchResults();
                SetPlaceholderText(_searchField, ChangeTabImage._lastPressed == "location" ? "Search for locations" : "Search for events");
            }
            
            // Create a marker and address card after an address lookup
            if (!_shouldPlaceMarker) return;
            
            // Wait for a fixed time for the map to load
            if (_timer < 1)
            {
                _timer += Time.deltaTime * SlowLoadFactor;
                var t = _timer * _timer * (3.0 - 2.0 * _timer);
                var curPos = _oldCameraPosition + (_newCameraPosition - _oldCameraPosition) * t;
                var curRot = _oldCameraRotation + (_newCameraRotation - _oldCameraRotation) * _timer;
                _cameraLocation.Position = new ArcGISPoint(curPos.x, curPos.y, curPos.z, new ArcGISSpatialReference(4326));
                _cameraLocation.Rotation = new ArcGISRotation(curRot.x, curRot.y, curRot.z);
            }
            else
            {
                PlaceOnGround(_queryLocationLocation);
                CreateAddressCard(true);

                // Place the camera above the marker and start rendering again
                var markerPosition = _queryLocationLocation.Position;
                _cameraLocation.Position = new ArcGISPoint(
                    markerPosition.X,
                    markerPosition.Y,
                    markerPosition.Z + _cameraLocation.GetComponent<HPTransform>().UniversePosition.y,
                    markerPosition.SpatialReference);
                _mainCamera.GetComponent<Camera>().cullingMask = -1;
            }
        }

        private void OnSearchValueChanged(ChangeEvent<string> evt)
        {
            Debug.Log("p");
            if (currentTab == "location")
            {
                _buildingOrEventListView.itemsSource = _buildingSearchList;
                _buildingOrEventListView.bindItem = bindLocationItem;
            }
            else
            {
                _buildingOrEventListView.itemsSource = _eventSearchList;
                _buildingOrEventListView.bindItem = bindEventItem;
            }
            string newText = evt.newValue;

            if (newText.EndsWith("\n"))
            {
                Deselect();
                _searchField.value = newText.TrimEnd('\n');
                return;
            }

            if (!string.IsNullOrWhiteSpace(newText))
            {
                if (currentTab == "location")
                {
                    List<Building> buildings = GetBuildingsFromSearch(newText, newText[0] == '~');
                    UpdateBuildingSearchUI(buildings);
                }
                else
                {
                    Debug.Log("ALLs");
                    List<EventData> events = eventManager.GetEventsFromSearch(newText, newText[0] == '~');
                    UpdateEventSearchUI(events);
                    Debug.Log("ALL");
                }
            }
            else
            {
                ClearSearchResults();
            }
        }


        private bool FuzzyMatch(string source, string target, int tolerance)
        {
            return LongestCommonSubsequence(source, target).Length >= target.Length - tolerance;
        }

        private List<Building> GetBuildingsFromSearch(string query, bool fuzzySearch)
        {
            const int TOLERANCE = 1;
            
            List<Building> buildings = new List<Building>();
            foreach (Building building in Locations.LocationList)
            {
                if (building.BuildingName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || 
                    (fuzzySearch && FuzzyMatch(building.BuildingName, query, TOLERANCE)))
                {
                    buildings.Add(building);
                    continue;
                }

                if (building.BuildingAllias == null) continue;
                foreach (string alias in building.BuildingAllias)
                {
                    if (alias.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || 
                        (fuzzySearch && FuzzyMatch(alias, query, TOLERANCE)))
                    {
                        buildings.Add(building);
                        break;
                    }
                }
            }

            return buildings;
        }

        

        private void UpdateBuildingSearchUI(List<Building> buildings)
        {
            // Clear previous suggestions
            ClearSearchResults();
            
            // Display the new suggestions in the AutoSuggestionText
            foreach (var building in buildings)
            {
                _buildingOrEventListView.itemsSource.Add(building);
                _buildingOrEventListView.Rebuild();
            }
        }
        
        private void UpdateEventSearchUI(List<EventData> events)
        {
            // Clear previous suggestions
            ClearSearchResults();

            // Display the new suggestions in the AutoSuggestionText
            foreach (var UCFEvent in events)
            {
                _buildingOrEventListView.itemsSource.Add(UCFEvent);
                _buildingOrEventListView.Rebuild();
            }
        }

        private void OnBuildingSearchClick(Building selectedBuilding)
        {
            if (_waitingForResponse) return;
            _waitingForResponse = true;
            Deselect();
            Debug.Log(
                $"Name: {selectedBuilding.BuildingName ?? ""}\n" +
                $"Aliases: {((selectedBuilding.BuildingAllias != null) ? string.Join(", ", selectedBuilding.BuildingAllias) : "")}\n" +
                $"Abbreviations: {((selectedBuilding.BuildingAbbreviation != null) ? string.Join(", ", selectedBuilding.BuildingAbbreviation) : "")}\n" +
                $"Description: {selectedBuilding.BuildingDesc ?? ""}\n" +
                $"Longitude: {selectedBuilding.BuildingLong}\n" +
                $"Latitude: {selectedBuilding.BuildingLat}\n" +
                $"Address: {selectedBuilding.BuildingAddress ?? ""}\n" +
                $"Events: {((selectedBuilding.BuildingEvents != null) ? string.Join(", ", selectedBuilding.BuildingEvents) : "")}\n");
            SetStuff(selectedBuilding.BuildingLong, selectedBuilding.BuildingLat, selectedBuilding.BuildingAddress);
            // ClearSearchResults();
            _waitingForResponse = false;
        }

        private void OnEventSearchClick(EventData selectedEvent)
        {
            if (_waitingForResponse) return;
            _waitingForResponse = true;
            Deselect();
            Debug.Log(
                $"Name: {selectedEvent.Name ?? ""}\n" +
                $"Description: {selectedEvent.Description ?? ""}\n" +
                $"Longitude: {selectedEvent.Location.BuildingLong}\n" +
                $"Latitude: {selectedEvent.Location.BuildingLat}\n" +
                $"Host: {selectedEvent.Host ?? ""}\n" +
                $"Start Time: {selectedEvent.DateTime}\n" +
                $"End Time: {selectedEvent.EndsOn}\n" +
                $"Image Path: {selectedEvent.Image ?? ""}\n" +
                $"Location: {selectedEvent.ListedLocation ?? ""}\n");
            // ClearSearchResults();
            _waitingForResponse = false;
        }
        
        private void Deselect()
        {
            // Deselect the text input field that was used to call this function. 
            // It is required so that the camera controller can be enabled/disabled when the input field is deselected/selected 
            var eventSystem = EventSystem.current;
            if (!eventSystem.alreadySelecting)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }
        
        private void FillInputField(string searchText)
        {
            _searchField.value = searchText;
        }

        private void ClearSearchResults()
        {
            _buildingOrEventListView.itemsSource.Clear();
            _buildingOrEventListView.Rebuild();
        }

        private void SetStuff(double lon, double lat, string address)
        {
            _responseAddress = address;
            
            _oldCameraPosition = new double3(_cameraLocation.Position.X, _cameraLocation.Position.Y, _cameraLocation.Position.Z);
            _oldCameraRotation = new double3(_cameraLocation.Rotation.Heading, _cameraLocation.Rotation.Pitch, _cameraLocation.Rotation.Roll);

            _newCameraRotation = new double3(0, 0, 0);
            _newCameraPosition = new double3(lon, lat, _cameraLocation.Position.Z);

            _shouldPlaceMarker = true;
            _timer = 0;
        }
        
        public static void SetPlaceholderText(TextField textField, string placeholder)
        {
            string placeholderClass = TextField.ussClassName + "__placeholder";
 
            onFocusOut();
            textField.RegisterCallback<FocusInEvent>(_ => onFocusIn());
            textField.RegisterCallback<FocusOutEvent>(_ => onFocusOut());
 
            void onFocusIn()
            {
                if (textField.ClassListContains(placeholderClass))
                {
                    textField.value = string.Empty;
                    textField.RemoveFromClassList(placeholderClass);
                }
            }
 
            void onFocusOut()
            {
                if (string.IsNullOrEmpty(textField.text))
                {
                    textField.SetValueWithoutNotify(placeholder);
                    textField.AddToClassList(placeholderClass);
                }
            }
        }

        /// <summary>
        /// Perform a raycast from current camera location towards the map to determine the height of earth's surface at that point.
        /// The game object received as input argument is placed on the ground and rotated upright
        /// </summary>
        /// <param name="markerLocation"></param>
        private void PlaceOnGround(ArcGISLocationComponent markerLocation)
        {
            var position = _mainCamera.transform.position;
            var raycastStart = new Vector3(position.x, position.y, position.z);

            if (Physics.Raycast(raycastStart, Vector3.down, out var hitInfo))
            {
                // Determine the geographic location of the point hit by the raycast and place the game object there
                markerLocation.Position = HitToGeoPosition(hitInfo, 0);
            }
            else // Raycast didn't hit an object. Print a warning
            {
                markerLocation.Position = _cameraLocation.Position;
                Debug.LogWarning("The elevation at the queried location could not be determined.");
            }
            // markerLocation.Position = new ArcGISPoint(markerLocation.Position.X, markerLocation.Position.Y
                                                      // ,20, new ArcGISSpatialReference(4326));

            markerLocation.Rotation = new ArcGISRotation(0, 90, 0);

            _shouldPlaceMarker = false;
        }

        /// <summary>
        /// Return GeoPosition for an engine location hit by a raycast.
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="yOffset"></param>
        /// <returns></returns>
        private ArcGISPoint HitToGeoPosition(RaycastHit hit, float yOffset = 0)
        {
            var worldPosition = math.inverse(_arcGisMapComponent.WorldMatrix).HomogeneousTransformPoint(hit.point.ToDouble3());
            var geoPosition = _arcGisMapComponent.View.WorldToGeographic(worldPosition);
            var offsetPosition = new ArcGISPoint(geoPosition.X, geoPosition.Y, geoPosition.Z + yOffset, geoPosition.SpatialReference);

            return GeoUtils.ProjectToSpatialReference(offsetPosition, new ArcGISSpatialReference(4326));
        }

        /// <summary>
        /// Create an instance of the template game object and sets the transform based on input arguments. Ensures the game object has an ArcGISLocation component attached.
        /// </summary>
        /// <param name="templateGo"></param>
        /// <param name="location"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        private void SetupQueryLocationGameObject(GameObject templateGo,
            Vector3 location = new(), Quaternion rotation = new(), Vector3 scale = new())
        {
            if (_queryLocationGo != null)
            {
                Destroy(_queryLocationGo);
            }

            _queryLocationGo = Instantiate(templateGo, location, rotation, _arcGisMapComponent.transform);
            _queryLocationGo.transform.localScale = scale;

            if (!_queryLocationGo.TryGetComponent<ArcGISLocationComponent>(out var markerLocComp))
            {
                markerLocComp = _queryLocationGo.AddComponent<ArcGISLocationComponent>();
            }
            markerLocComp.enabled = true;
        }

        /// <summary>
        /// Create a visual cue for showing the address/description returned for the query. 
        /// </summary>
        /// <param name="isAddressQuery"></param>
        private void CreateAddressCard(bool isAddressQuery)
        {
            var card = Instantiate(addressCardTemplate, _queryLocationGo.transform);
            var t = card.GetComponentInChildren<TextMeshProUGUI>();
            // Based on the type of the query set the location, rotation and scale of the text relative to the query location game object  
            if (isAddressQuery)
            {
                var localScale = 1.5f / addressMarkerScale;
                card.transform.localPosition = new Vector3(0, 150f / addressMarkerScale, 100f / addressMarkerScale);
                card.transform.localRotation = Quaternion.Euler(270, 0, 180);
                card.transform.localScale = new Vector3(localScale, localScale, localScale);
            }
            else
            {
                var localScale = 3.5f / locationMarkerScale;
                card.transform.localPosition = new Vector3(0, 300f / locationMarkerScale, -300f / locationMarkerScale);
                card.transform.localScale = new Vector3(localScale, localScale, localScale);
            }

            if (t != null)
            {
                t.text = _responseAddress;
            }
        }  
        
        private string LongestCommonSubsequence(string source, string target)
        {
            int[,] C = LongestCommonSubsequenceLengthTable(source, target);

            return Backtrack(C, source, target, source.Length, target.Length);
        }

        private int[,] LongestCommonSubsequenceLengthTable(string source, string target)
        {
            int[,] C = new int[source.Length + 1, target.Length + 1];

            for (int i = 0; i < source.Length + 1; i++) { C[i, 0] = 0; }
            for (int j = 0; j < target.Length + 1; j++) { C[0, j] = 0; }

            for (int i = 1; i < source.Length + 1; i++)
            {
                for (int j = 1; j < target.Length + 1; j++)
                {
                    if (source[i - 1].Equals(target[j - 1]))
                    {
                        C[i, j] = C[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        C[i, j] = Math.Max(C[i, j - 1], C[i - 1, j]);
                    }
                }
            }

            return C;
        }

        private string Backtrack(int[,] C, string source, string target, int i, int j)
        {
            if (i == 0 || j == 0)
            {
                return "";
            }
            else if (source[i - 1].Equals(target[j - 1]))
            {
                return Backtrack(C, source, target, i - 1, j - 1) + source[i - 1];
            }
            else
            {
                if (C[i, j - 1] > C[i - 1, j])
                {
                    return Backtrack(C, source, target, i, j - 1);
                }
                else
                {
                    return Backtrack(C, source, target, i - 1, j);
                }
            }
        }
    }
}