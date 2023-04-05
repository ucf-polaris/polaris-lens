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
using Newtonsoft.Json.Linq;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace POLARIS
{
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
        private GameObject _queryLocationGo;
        private ArcGISLocationComponent _queryLocationLocation;
        private ArcGISMapComponent _arcGisMapComponent;
        private string _responseAddress = "";
        private bool _shouldPlaceMarker = false;
        private bool _waitingForResponse = false;
        private float _timer = 0;
        private const float MapLoadWaitTime = 1;
        private const string AddressQueryURL = "https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer/findAddressCandidates";
        private const string LocationQueryURL = "https://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer/reverseGeocode";


        private void Start()
        {
            _arcGisMapComponent = FindObjectOfType<ArcGISMapComponent>();
            _mainCamera = Camera.main;
            _cameraLocation = _mainCamera.GetComponent<ArcGISLocationComponent>();
            
            SetupQueryLocationGameObject(AddressMarkerTemplate, scale: new Vector3(AddressMarkerScale, AddressMarkerScale, AddressMarkerScale));
            _queryLocationLocation = _queryLocationGo.GetComponent<ArcGISLocationComponent>();
            
            var rootVisual = SearchBar.GetComponent<UIDocument>().rootVisualElement;
            _searchField = rootVisual.Q<TextField>("SearchBox");
            _searchButton = rootVisual.Q<Button>("SearchButton");
            _searchButton.RegisterCallback<ClickEvent>(OnButtonClick);
        }

        private void Update()
        {
            // Create a marker and address card after an address lookup
            if (_shouldPlaceMarker)
            {
                // Wait for a fixed time for the map to load
                if (_timer < MapLoadWaitTime)
                {
                    _timer += Time.deltaTime;
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

            // Determine the location that was clicked on and perform a location lookup
            // if (!Input.GetKey(KeyCode.LeftShift) || !Input.GetMouseButtonDown(0)) return;
            //
            // var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            // if (!Physics.Raycast(ray, out var hit)) return;
            //
            // var position = _mainCamera.transform.position;
            // // var direction = (hit.point - position);
            // var distanceFromCamera = Vector3.Distance(position, hit.point);
            // var scale = distanceFromCamera * LocationMarkerScale / 5000; // Scale the marker based on its distance from camera 
            // SetupQueryLocationGameObject(LocationMarkerTemplate, hit.point, _mainCamera.transform.rotation, new Vector3(scale, scale, scale));
            // ReverseGeocode(HitToGeoPosition(hit));
        }
        
        private void OnButtonClick(ClickEvent clickEvent)
        {
            print("Clicked search button");
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
            if (_waitingForResponse)
            {
                return;
            }

            _waitingForResponse = true;
            var results = await SendAddressQuery(address);

            if (results.Contains("error")) // Server returned an error
            {
                var response = JObject.Parse(results);
                var error = response.SelectToken("error");
                Debug.Log(error.SelectToken("message"));
            }
            else
            {
                const int cameraStartHeight = 3000; // Use a high elevation to do a raycast from

                // Parse the query response
                var response = JObject.Parse(results);
                var candidates = response.SelectToken("candidates");
                if (candidates is JArray array)
                {
                    if (array.Count > 0) // Check if the response included any result  
                    {
                        var location = array[0].SelectToken("location");
                        var lon = location.SelectToken("x");
                        var lat = location.SelectToken("y");
                        _responseAddress = (string)array[0].SelectToken("address");

                        // Move the camera to the queried address
                        _mainCamera.GetComponent<Camera>().cullingMask = 0; // blacken the camera view until the scene is updated

                        _cameraLocation.Rotation = new ArcGISRotation(0, 0, 0);
                        _cameraLocation.Position = new ArcGISPoint((double)lon, (double)lat, _cameraLocation.Position.Z, new ArcGISSpatialReference(4326));

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
        /// Perform a reverse geocoding query (location lookup) and parse the response. If the server returned an error, the message is shown to the user.
        /// The function is called when a location on the map is selected.
        /// </summary>
        /// <param name="location"></param>
        private async void ReverseGeocode(ArcGISPoint location)
        {
            if (_waitingForResponse)
            {
                return;
            }

            _waitingForResponse = true;
            var results = await SendLocationQuery(location.X.ToString() + "," + location.Y.ToString());

            if (results.Contains("error")) // Server returned an error
            {
                var response = JObject.Parse(results);
                var error = response.SelectToken("error");
                Debug.Log(error.SelectToken("message"));
            }
            else
            {
                var response = JObject.Parse(results);
                var address = response.SelectToken("address");
                var label = address.SelectToken("LongLabel");
                _responseAddress = (string)label;

                if (string.IsNullOrEmpty(_responseAddress))
                {
                    print("Query did not return a valid response.");
                }
                else
                {
                    print("Enter an address above to move there or shift+click on a location to see the address / description.");
                    CreateAddressCard(false);
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
        ///  Create and send an HTTP request for a reverse geocoding query and return the received response.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        private static async Task<string> SendLocationQuery(string location)
        {
            IEnumerable<KeyValuePair<string, string>> payload = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("location", location),
                new KeyValuePair<string, string>("langCode", "en"),
                new KeyValuePair<string, string>("f", "json"),
            };

            var client = new HttpClient();
            HttpContent content = new FormUrlEncodedContent(payload);
            var response = await client.PostAsync(LocationQueryURL, content);

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
                card.transform.localRotation = Quaternion.Euler(90, 0, 0);
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
