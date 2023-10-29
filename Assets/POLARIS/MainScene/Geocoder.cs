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
        private float _timer = 0;
        private const float SlowLoadFactor = 1;

        private void Start()
        {
            _arcGisMapComponent = FindObjectOfType<ArcGISMapComponent>();
            _mainCamera = Camera.main;
            _cameraLocation = _mainCamera.GetComponent<ArcGISLocationComponent>();

            SetupQueryLocationGameObject(addressMarkerTemplate, scale: new Vector3(addressMarkerScale, addressMarkerScale, addressMarkerScale));
            _queryLocationLocation = _queryLocationGo.GetComponent<ArcGISLocationComponent>();
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
        public void SetStuff(double lon, double lat, string address)
        {
            _responseAddress = address;

            _oldCameraPosition = new double3(_cameraLocation.Position.X, _cameraLocation.Position.Y, _cameraLocation.Position.Z);
            _oldCameraRotation = new double3(_cameraLocation.Rotation.Heading, _cameraLocation.Rotation.Pitch, _cameraLocation.Rotation.Roll);

            _newCameraRotation = new double3(0, 0, 0);
            _newCameraPosition = new double3(lon, lat, _cameraLocation.Position.Z);

            _shouldPlaceMarker = true;
            _timer = 0;
        }
    }

}