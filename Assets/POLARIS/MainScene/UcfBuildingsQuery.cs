// Copyright 2022 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISMapsSDK.Components;
using Esri.GameEngine.Geometry;
using Esri.HPFramework;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Unity.Mathematics;
using TMPro;
using POLARIS.Managers;

namespace POLARIS
{ 
    // The follow System.Serializable classes are used to define the REST API response
    // in order to leverage Unity's JsonUtility.
    // When implementing your own version of this the Baseball Properties would need to 
    // be updated.
    [Serializable]
    internal class FeatureCollectionData
    {
        public string geometryType;
        public List<Feature> features;
    }

    [Serializable]
    internal class Feature
    {
        public Geometry geometry;
        public BuildingProperties attributes;
    }

    [Serializable]
    internal class BuildingProperties
    {
        public string BuildingNa;
        public string BuildingNu;
    }

    [Serializable]
    internal class Geometry
    {
        public string type;
        public List<List<List<double>>> rings;
    }

    // This class issues a query request to a Feature Layer which it then parses to create GameObjects at accurate locations
    // with correct property values. This is a good starting point if you are looking to parse your own feature layer into Unity.
    public class UcfBuildingsQuery : MonoBehaviour
    {
        [SerializeField] private Color32 baseBuildingColor = new Color32(23, 103, 194, 255);
        [SerializeField] private Color32 topBuildingColor = new Color32(123, 13, 194, 255);
        // The feature layer we are going to query
        public string FeatureLayerURL = "https://services.arcgis.com/dVL5xxth19juhrDY/ArcGIS/rest/services/MainCampus_RPbldgs/FeatureServer/0";

        // The height where we spawn the building before finding the ground height
        private const int BuildingSpawnHeight = 10000;

        // This will hold a reference to each feature we created
        // public List<GameObject> Buildings = new List<GameObject>();

        // In the query request we can denote the Spatial Reference we want the return geometries in.
        // It is important that we create the GameObjects with the same Spatial Reference
        private const int FeatureSRWKID = 4326;

        public ArcGISMapComponent ArcGisMapComponent;
        private ArcGISLocationComponent _locationComponent;
        private double3 _rootPos;

        private ArcGISCameraComponent _arcGisCamera;

        private PolyExtruder _polyExtruder;

        private bool _runCreate;

        // Get all the features when the script starts
        private void Start()
        {
            var loader = FindChildWithTag(ArcGisMapComponent.gameObject, "Location");
            _locationComponent = loader.GetComponent<ArcGISLocationComponent>();
            _rootPos = loader.GetComponent<HPTransform>().UniversePosition;

            if (ArcGisMapComponent.HasSpatialReference())
            {
                _runCreate = true;
            }
            else
            {
                ArcGisMapComponent.View.SpatialReferenceChanged += () => _runCreate = true;
            }
        }
        
        private void Update()
        {
            if (!_runCreate) return;
            
            StartCoroutine(GetFeatures());
            _runCreate = false;
        }

        // Sends the Request to get features from the service
        private IEnumerator GetFeatures() //Action<UnityWebRequest> callback
        {
            // To learn more about the Feature Layer rest API and all the things that are possible checkout
            // https://developers.arcgis.com/rest/services-reference/enterprise/query-feature-service-layer-.htm

            var queryRequestURL = FeatureLayerURL + "/Query?" + MakeRequestHeaders();
            Debug.Log(queryRequestURL);
            var request = UnityWebRequest.Get(queryRequestURL);
            
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
            }
            else
            {
                // Wait until locations are filled to use numEvents of each building for coloring
                while (Locations.LocationList == null) yield return null;
                
                CreateGameObjectsFromResponse(request.downloadHandler.text);
            }
        }


        // Creates the Request Headers to be used in our HTTP Request
        private static string MakeRequestHeaders()
        {
            string[] outFields =
            {
                "BuildingNa",
                "BuildingNu",
            };

            var outFieldHeader = "outFields=";
            for (var i = 0; i < outFields.Length; i++)
            {
                outFieldHeader += outFields[i];
            
                if (i < outFields.Length - 1)
                {
                    outFieldHeader += ",";
                }
            }

            // f=json is the output format
            // where=1=1 gets every feature. geometry based or more intelligent where clauses should be used
            //     with larger datasets
            // outSR=4326 gets the return geometries in the SR 4326
            string[] requestHeaders =
            {
                "f=json",
                "where=1=1",
                "outSR=" + FeatureSRWKID.ToString(),
                outFieldHeader
            };

            var returnValue = "";
            for (var i = 0; i < requestHeaders.Length; i++)
            {
                returnValue += requestHeaders[i];

                if (i < requestHeaders.Length - 1)
                {
                    returnValue += "&";
                }
            }

            return returnValue;
        }

        // Given a valid response from our query request to the feature layer, this method will parse the response text
        // into geometries and properties which it will use to create new GameObjects and locate them correctly in the world.
        // This logic will differ based on the properties you are trying to parse out of the response.
        private void CreateGameObjectsFromResponse(string response)
        {
            // Deserialize the JSON response from the query.
            var deserialized = JsonConvert.DeserializeObject<FeatureCollectionData>(response);

            foreach (var feature in deserialized.features)
            {
                var pointsList = new List<Vector2>();
                for (var index = 0; index < feature.geometry.rings[0].Count; index++)
                {
                    var pair = feature.geometry.rings[0][index];
                    var position = new ArcGISPoint(pair[0], pair[1], 0, new ArcGISSpatialReference(FeatureSRWKID));
                    var localCoords = ArcGisMapComponent.View.GeographicToWorld(position);
                    pointsList.Add(
                        new Vector2((float)(localCoords.x - _rootPos.x), (float)(localCoords.z - _rootPos.z)));
                }
                var vertices2D = pointsList.ToArray();

                var buildingObject = new GameObject(feature.attributes.BuildingNa);
                buildingObject.transform.SetParent(_locationComponent.transform);

                _polyExtruder = buildingObject.AddComponent<PolyExtruder>();
                _polyExtruder.isOutlineRendered = false;

                var numEventsOfBuilding = GetNumEventsBuilding(feature.attributes.BuildingNa);
                var colorOfBuilding = Color.Lerp(
                    baseBuildingColor, topBuildingColor, 1 - (1.0f / (numEventsOfBuilding + 1))
                    );
                // Debug.Log("calculated r: " + baseBuildingColor.r * (1.0f / (numEventsOfBuilding + 1)));
                // Debug.Log("calculated g: " + baseBuildingColor.g * (1.0f / (numEventsOfBuilding + 1)));
                // Debug.Log("calculated b: " + baseBuildingColor.b * (1.0f / (numEventsOfBuilding + 1)));
                //Debug.Log("Building " + feature.attributes.BuildingNa + " has new color: " + colorOfBuilding);
                _polyExtruder.createPrism(feature.attributes.BuildingNa, 50.0f, vertices2D, 
                    colorOfBuilding, true, false, true);
                // TODO: Add mesh collider to walls
                // gameObject.AddComponent<MeshCollider>();
            }
        }
        
        private int GetNumEventsBuilding(String buildingName)
        {
            Building foundBuilding = null;
            foreach (Building building in Locations.LocationList)
            {
                if (String.Equals(buildingName, building.BuildingName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    foundBuilding = building;
                    break;
                }

                if (building.BuildingAllias == null) continue;
                foreach (string alias in building.BuildingAllias)
                {
                    if (String.Equals(buildingName, alias,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        foundBuilding = building;
                        break;
                    }
                }
            }

            if (foundBuilding == null || foundBuilding.BuildingEvents == null) return 0;
            return foundBuilding.BuildingEvents.Length;
        }
        
        private static GameObject FindChildWithTag(GameObject parent, string tag)
        {
            return (from Transform transform in parent.transform where transform.CompareTag(tag) select transform.gameObject).FirstOrDefault();
        }
    }
}