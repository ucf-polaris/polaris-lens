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
        private Camera _mainCamera;
        private ArcGISLocationComponent _cameraLocation;
        private double3 _oldCameraPosition;
        private double3 _newCameraPosition;
        private double3 _oldCameraRotation;
        private double3 _newCameraRotation;

        private ArcGISMapComponent _arcGisMapComponent;
        private bool _shouldMoveCamera = false;
        private float _timer = 0;
        private const float SlowLoadFactor = 1;

        private void Start()
        {
            _arcGisMapComponent = FindObjectOfType<ArcGISMapComponent>();
            _mainCamera = Camera.main;
            _cameraLocation = _mainCamera.GetComponent<ArcGISLocationComponent>();
        }

        private void Update()
        {
            if (_shouldMoveCamera)
            {
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
                    _shouldMoveCamera = false;
                }
            }
        }
        
        public void MoveCameraToCoordinates(double lon, double lat)
        {
            _oldCameraPosition = new double3(_cameraLocation.Position.X, _cameraLocation.Position.Y, _cameraLocation.Position.Z);
            _oldCameraRotation = new double3(_cameraLocation.Rotation.Heading, _cameraLocation.Rotation.Pitch, _cameraLocation.Rotation.Roll);

            _newCameraRotation = new double3(0, 0, 0);
            _newCameraPosition = new double3(lon, lat, _cameraLocation.Position.Z);

            _shouldMoveCamera = true;
            _timer = 0;
        }
    }

}