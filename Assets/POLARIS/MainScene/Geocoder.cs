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

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Esri.HPFramework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace POLARIS.MainScene
{
    public class AutoSuggestion
    {
        public string Text { get; set; }
        public string MagicKey { get; set; }
        public bool IsCollection { get; set; }
    }

    public class SearchResult
    {
        public double Longitude { get; set;  }
        public double Latitude { get; set;  }
        public string Address { get; set; }
    }
    
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
        private const string AddressQueryURL = "https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer/findAddressCandidates";
        private const string LocationQueryURL = "https://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer/reverseGeocode";
        private const string SuggestQueryURL = "https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer/suggest";

        // private Dropdown _dropdown;
        private Label _autoSuggestionBigLabel;
        private bool _justClicked = false;

        private Label _searchResultBigLabel;

        private void Start()
        {
            _arcGisMapComponent = FindObjectOfType<ArcGISMapComponent>();
            _mainCamera = Camera.main;
            _cameraLocation = _mainCamera.GetComponent<ArcGISLocationComponent>();
            
            SetupQueryLocationGameObject(addressMarkerTemplate, scale: new Vector3(addressMarkerScale, addressMarkerScale, addressMarkerScale));
            _queryLocationLocation = _queryLocationGo.GetComponent<ArcGISLocationComponent>();
            
            var rootVisual = searchBar.GetComponent<UIDocument>().rootVisualElement;
            _searchField = rootVisual.Q<TextField>("SearchBox");
            _searchField.RegisterValueChangedCallback(OnSearchValueChanged);
            _searchButton = rootVisual.Q<Button>("SearchButton");
            _searchButton.RegisterCallback<ClickEvent>(OnButtonClick);
            _autoSuggestionBigLabel = rootVisual.Q<Label>("AutoSuggestionBigLabel");
            _searchResultBigLabel = rootVisual.Q<Label>("SearchResultBigLabel");
            _clearButton = rootVisual.Q<Button>("ClearButton");
            _clearButton.RegisterCallback<ClickEvent>(_ => ClearSearchResultsUI());
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
            if (!string.IsNullOrWhiteSpace(newText) && !_justClicked)
            {
                List<AutoSuggestion> suggestions = await FetchAutoSuggestions(newText);
                UpdateAutoSuggestionsUI(suggestions);
            }
            else
            {
                ClearAutoSuggestionsUI();
                _justClicked = false;
            }
        }

        private async Task<List<AutoSuggestion>> FetchAutoSuggestions(string query)
        {
            IEnumerable<KeyValuePair<string, string>> payload = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("text", "University of Central Florida " + query),
                new KeyValuePair<string, string>("token", _arcGisMapComponent.APIKey),
                new KeyValuePair<string, string>("searchExtent", "-81.209995,28.580255,-81.181589,28.613986"),
                new KeyValuePair<string, string>("f", "json"),
            };
            
            var client = new HttpClient();
            HttpContent content = new FormUrlEncodedContent(payload);
            HttpResponseMessage response = await client.PostAsync(SuggestQueryURL, content);
            
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                List<AutoSuggestion> suggestions = ParseSuggestions(responseContent);
                return suggestions;
            }
            else
            {
                print($"Error fetching suggestions: {response.StatusCode}");
                return new List<AutoSuggestion>();
            }
        }
        
        private List<AutoSuggestion> ParseSuggestions(string jsonResponse)
        {
            List<AutoSuggestion> suggestions = new List<AutoSuggestion>();
            
            try
            {
                JObject response = JObject.Parse(jsonResponse);
                print(response);
                if (response.TryGetValue("suggestions", out JToken suggestionsToken) &&
                    suggestionsToken is JArray suggestionArray)
                {
                    foreach (JToken suggestionToken in suggestionArray)
                    {
                        AutoSuggestion suggestion = new AutoSuggestion
                        {
                            Text = suggestionToken["text"].ToString(),
                            MagicKey = suggestionToken["magicKey"].ToString(),
                            IsCollection = (bool)suggestionToken["isCollection"]
                        };
                        suggestions.Add(suggestion);
                    }
                }
                else
                {
                    print("No suggestions found in the JSON response.");
                }
            }
            catch (JsonException ex)
            {
                print($"Error parsing JSON response: {ex.Message}");
            }

            return suggestions;
        }

        private void UpdateAutoSuggestionsUI(List<AutoSuggestion> suggestions)
        {
            // Clear previous suggestions
            ClearAutoSuggestionsUI();

            // Display the new suggestions in the AutoSuggestionText
            foreach (var suggestion in suggestions)
            {
                // Just get place name (no city / state), and no UCF since that is added in find button anyways
                var splitSuggestion = suggestion.Text.Split(",");
                var suggestionLocationName = splitSuggestion[0];
                suggestionLocationName = suggestionLocationName.Replace("University of Central Florida Orlando Campus", "");
                var autoSuggestionSubLabel = new Label
                {
                    text = suggestionLocationName
                };
                autoSuggestionSubLabel.AddToClassList("sublabel"); 
                autoSuggestionSubLabel.RegisterCallback<ClickEvent>(_ => OnSuggestionClick(autoSuggestionSubLabel.text, suggestion.MagicKey));
                _autoSuggestionBigLabel.Add(autoSuggestionSubLabel);
            }
        }

        private async void OnSuggestionClick(string suggestionText, string magicKey)
        {
            FillInputField(suggestionText);
            
            // --------- Basically copied from OnButtonClick(), lol
            if (_waitingForResponse) return;

            _waitingForResponse = true;
            List<SearchResult> searchResults = await FetchSearchResults(suggestionText, magicKey);
            UpdateSearchResultsUI(searchResults);

            // Deselect the text input field that was used to call this function. 
            // It is required so that the camera controller can be enabled/disabled when the input field is deselected/selected 
            var eventSystem = EventSystem.current;
            if (!eventSystem.alreadySelecting)
            {
                eventSystem.SetSelectedGameObject(null);
            }
            _waitingForResponse = false;
            // -----------
            
            ClearAutoSuggestionsUI();
            _justClicked = true;
        }
        
        private void FillInputField(string suggestionText)
        {
            _searchField.value = suggestionText;
        }

        private void ClearAutoSuggestionsUI()
        {
            _autoSuggestionBigLabel.Clear();
        }

        private async void OnButtonClick(ClickEvent clickEvent)
        {
            if (_waitingForResponse) return;

            _waitingForResponse = true;
            var textInput = _searchField.value;
            if (!string.IsNullOrWhiteSpace(textInput))
            {
                List<SearchResult> searchResults = await FetchSearchResults(_searchField.value);
                UpdateSearchResultsUI(searchResults);
            }

            // Deselect the text input field that was used to call this function. 
            // It is required so that the camera controller can be enabled/disabled when the input field is deselected/selected 
            var eventSystem = EventSystem.current;
            if (!eventSystem.alreadySelecting)
            {
                eventSystem.SetSelectedGameObject(null);
            }
            _waitingForResponse = false;
        }

        private async Task<List<SearchResult>> FetchSearchResults(string address, string magicKey = "")
        {
            // Add school name in front of address query for more relevant results
            string results = await SendAddressQuery("University of Central Florida " + address, magicKey);
            List<SearchResult> searchResults = ParseSearchResults(results);
            return searchResults;
        }

        private List<SearchResult> ParseSearchResults(string jsonResponse)
        {
            List<SearchResult> searchResults = new List<SearchResult>();
            if (jsonResponse.Contains("error")) // Server returned an error
            {
                var response = JObject.Parse(jsonResponse);
                var error = response.SelectToken("error");
                Debug.Log(error.SelectToken("message"));
            }
            else
            {
                // Parse the query response
                var response = JObject.Parse(jsonResponse);
                var candidates = response.SelectToken("candidates");
                if (candidates is JArray array)
                {
                    if (array.Count > 0)
                    {
                        foreach (JToken searchToken in array)
                        {
                            var location = searchToken.SelectToken("location");
                            SearchResult result = new SearchResult
                            {
                                Longitude = (double)location.SelectToken("x"),
                                Latitude = (double)location.SelectToken("y"),
                                Address = (string)searchToken.SelectToken("address")
                            };
                            searchResults.Add(result);
                        }
                    }
                    else
                    {
                        Destroy(_queryLocationGo);
                    }
                }

                return searchResults;
            }

            return new List<SearchResult>();
        }

        private void UpdateSearchResultsUI(List<SearchResult> searchResults)
        {
            ClearSearchResultsUI();
            
            foreach (var searchResult in searchResults)
            {
                var searchResultSubLabel = new Label
                {
                    text = searchResult.Address
                };
                // Add to class list
                searchResultSubLabel.RegisterCallback<ClickEvent>(_ => SetStuff(searchResult.Longitude, searchResult.Latitude, searchResult.Address));
                _searchResultBigLabel.Add(searchResultSubLabel);
            }
        }

        // TODO: SHOULD SHOW BUILDING INFORMATION
        private void OnSearchClick()
        {
            
        }
        
        private void ClearSearchResultsUI()
        {
            _searchResultBigLabel.Clear();
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

        /// <summary>
        /// Create and send an HTTP request for a geocoding query and return the received response.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="magicKey"></param>
        /// <returns></returns>
        private async Task<string> SendAddressQuery(string address, string magicKey = "")
        {
            IEnumerable<KeyValuePair<string, string>> payload = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("SingleLine", address),
                new KeyValuePair<string, string>("token", _arcGisMapComponent.APIKey),
                new KeyValuePair<string, string>("searchExtent", "-81.209995,28.580255,-81.181589,28.613986"),
                new KeyValuePair<string, string>("f", "json"),
                new KeyValuePair<string, string>("magicKey", magicKey),
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
    }
}