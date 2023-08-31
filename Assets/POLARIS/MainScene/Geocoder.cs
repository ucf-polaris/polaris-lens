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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Esri.HPFramework;
using Newtonsoft.Json.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

using Newtonsoft.Json;

namespace POLARIS
{
    public class AutoSuggestion
    {
        public string Text { get; set; }
    }
    public class Geocoder : MonoBehaviour
    {
        public GameObject AddressMarkerTemplate;
        public float AddressMarkerScale = 1;
        public GameObject LocationMarkerTemplate;
        public float LocationMarkerScale = 1;
        public GameObject AddressCardTemplate;
        public TextMeshProUGUI InfoField;
        public GameObject SearchBar;
        
        private TextField _searchField;
        private Button _searchButton;

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
        private const string AddressQueryURL = "https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer/findAddressCandidates";
        private const string LocationQueryURL = "https://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer/reverseGeocode";

        // private Dropdown _dropdown;
        private Label AutoSuggestionText;

        private void Start()
        {
            _arcGisMapComponent = FindObjectOfType<ArcGISMapComponent>();
            _mainCamera = Camera.main;
            _cameraLocation = _mainCamera.GetComponent<ArcGISLocationComponent>();
            
            SetupQueryLocationGameObject(AddressMarkerTemplate, scale: new Vector3(AddressMarkerScale, AddressMarkerScale, AddressMarkerScale));
            _queryLocationLocation = _queryLocationGo.GetComponent<ArcGISLocationComponent>();
            
            var rootVisual = SearchBar.GetComponent<UIDocument>().rootVisualElement;
            _searchField = rootVisual.Q<TextField>("SearchBox");
            _searchField.RegisterValueChangedCallback(OnSearchValueChanged);
            _searchButton = rootVisual.Q<UnityEngine.UIElements.Button>("SearchButton");
            _searchButton.RegisterCallback<ClickEvent>(OnButtonClick);
            AutoSuggestionText = rootVisual.Q<Label>("AutoSuggestionText");
            // _dropdown = rootVisual.Q<Dropdown>("AutocompleteDropdown");
            // _dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        private void Update()
        {
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

        private async void OnSearchValueChanged(ChangeEvent<string> evt)
        {
            string newText = evt.newValue;
            if (!string.IsNullOrWhiteSpace(newText))
            {
                List<string> suggestions = await FetchAutoSuggestions(newText);
                UpdateAutoSuggestionsUI(suggestions);
            }
            else
            {
                ClearAutoSuggestionsUI();
            }
        }

        private async Task<List<string>> FetchAutoSuggestions(string query)
        {
            // // Perform a request to get autocomplete suggestions based on the query
            // // Parse the response and return a list of suggestion strings
            // var locatorTask = new LocatorTask(new Uri("https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer"));
            List<string> suggestions = new List<string>();
            suggestions.Add("bunger");
            suggestions.Add("bunger2");
            // var suggestions2 = await locatorTask.SuggestAsync(query);
            // if (suggestions2?.Any() ?? false)
            // {
            //     foreach (var suggestion in suggestions2)
            //     {
            //         suggestions.Add(suggestion.Label);
            //     }
            // }
            return suggestions;
        }

        private void UpdateAutoSuggestionsUI(List<string> suggestions)
        {
            // Clear previous suggestions
            AutoSuggestionText.text = "";

            // Display the new suggestions in the AutoSuggestionText
            foreach (string suggestion in suggestions)
            {
                AutoSuggestionText.text += suggestion + "\n";
            }
        }

        private void ClearAutoSuggestionsUI()
        {
            AutoSuggestionText.text = "";
        }

        private void OnButtonClick(ClickEvent clickEvent)
        {
            HandleTextInput(_searchField.value);
        }

        /// <summary>
        /// Verify the input text and call the geocoder. This function is called when an address is entered in the text input field.
        /// </summary>
        /// <param name="textInput"></param>
        private void HandleTextInput(string textInput)
        {
            if (!string.IsNullOrWhiteSpace(textInput))
            {
                Geocode(textInput);
            }

            // Deselect the text input field that was used to call this function. 
            // It is required so that the camera controller can be enabled/disabled when the input field is deselected/selected 
            var eventSystem = EventSystem.current;
            if (!eventSystem.alreadySelecting)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }


        /// <summary>
        /// Perform a geocoding query (address lookup) and parse the response. If the server returned an error, the message is shown to the user.
        /// </summary>
        /// <param name="address"></param>
        private async void Geocode(string address)
        {
            if (_waitingForResponse) return;

            _waitingForResponse = true;
            var results = await SendAddressQuery("University of Central Florida " + address);
            print(results);

            if (results.Contains("error")) // Server returned an error
            {
                var response = JObject.Parse(results);
                var error = response.SelectToken("error");
                Debug.Log(error.SelectToken("message"));
            }
            else
            {
                // Parse the query response
                var response = JObject.Parse(results);
                var candidates = response.SelectToken("candidates");
                if (candidates is JArray array)
                {
                    if (array.Count > 0) // Check if the response included any result  
                    {
                        // print("multiple results: ");
                        // for (int i = 0; i < array.count; i++) { 
                            // print((string)array[i].selecttoken("address")); 
                        // }

                        var location = array[0].SelectToken("location");
                        var lon = location.SelectToken("x");
                        var lat = location.SelectToken("y");
                        _responseAddress = (string)array[0].SelectToken("address");

                        // Move the camera to the queried address
                        _oldCameraPosition = new double3(_cameraLocation.Position.X, _cameraLocation.Position.Y, _cameraLocation.Position.Z);
                        _oldCameraRotation = new double3(_cameraLocation.Rotation.Heading, _cameraLocation.Rotation.Pitch, _cameraLocation.Rotation.Roll);

                        _newCameraRotation = new double3(0, 0, 0);
                        _newCameraPosition = new double3((double)lon, (double)lat, _cameraLocation.Position.Z);

                        _shouldPlaceMarker = true;
                        _timer = 0;
                    }
                    else
                    {
                        Destroy(_queryLocationGo);
                    }

                    // Update the info field in the UI
                    var errorText = array.Count switch
                    {
                        0 => "Query did not return a valid response.",
                        1 => "Enter an address above to move there or shift+click on a location to see the address / description.",
                        _ => "Query returned multiple results. If the shown location is not the intended one, make your input more specific."
                    };
                    print(errorText);
                }
            }
            _waitingForResponse = false;
        }

        /// <summary>
        /// Create and send an HTTP request for a geocoding query and return the received response.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private async Task<string> SendAddressQuery(string address)
        {

            IEnumerable<KeyValuePair<string, string>> payload = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("address", address),
                new KeyValuePair<string, string>("token", _arcGisMapComponent.APIKey),
                new KeyValuePair<string, string>("searchExtent", "-81.209995,28.580255,-81.181589,28.613986"),
                new KeyValuePair<string, string>("f", "json"),
            };

            var client = new HttpClient();
            HttpContent content = new FormUrlEncodedContent(payload);
            var response = await client.PostAsync(AddressQueryURL, content);

            response.EnsureSuccessStatusCode();
            var results = await response.Content.ReadAsStringAsync();
            return results;
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
            var card = Instantiate(AddressCardTemplate, _queryLocationGo.transform);
            var t = card.GetComponentInChildren<TextMeshProUGUI>();
            // Based on the type of the query set the location, rotation and scale of the text relative to the query location game object  
            if (isAddressQuery)
            {
                var localScale = 1.5f / AddressMarkerScale;
                card.transform.localPosition = new Vector3(0, 150f / AddressMarkerScale, 100f / AddressMarkerScale);
                card.transform.localRotation = Quaternion.Euler(270, 0, 180);
                card.transform.localScale = new Vector3(localScale, localScale, localScale);
            }
            else
            {
                var localScale = 3.5f / LocationMarkerScale;
                card.transform.localPosition = new Vector3(0, 300f / LocationMarkerScale, -300f / LocationMarkerScale);
                card.transform.localScale = new Vector3(localScale, localScale, localScale);
            }

            if (t != null)
            {
                t.text = _responseAddress;
            }
        }
    }
}
