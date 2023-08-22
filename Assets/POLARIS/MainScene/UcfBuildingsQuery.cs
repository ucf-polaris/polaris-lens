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
        // The feature layer we are going to query
        public string FeatureLayerURL = "https://services.arcgis.com/dVL5xxth19juhrDY/ArcGIS/rest/services/MainCampus_RPbldgs/FeatureServer/0";

        // The height where we spawn the stadium before finding the ground height
        private const int BuildingSpawnHeight = 10000;

        // This will hold a reference to each feature we created
        public List<GameObject> Buildings = new List<GameObject>();

        // In the query request we can denote the Spatial Reference we want the return geometries in.
        // It is important that we create the GameObjects with the same Spatial Reference
        private const int FeatureSRWKID = 4326;

        // public Dropdown StadiumSelector;

        private ArcGISMapComponent _arcGisMapComponent;
        private ArcGISLocationComponent _locationComponent;
        private double3 _rootPos;

        private PolyExtruder _polyExtruder;

        // Get all the features when the script starts
        private void Start()
        {
            _arcGisMapComponent = FindObjectOfType<ArcGISMapComponent>();
            var loader = FindChildWithTag(_arcGisMapComponent.gameObject, "Location");
            _locationComponent = loader.GetComponent<ArcGISLocationComponent>();
            _rootPos = loader.GetComponent<HPTransform>().UniversePosition;
            
            StartCoroutine(GetFeatures());

            // StadiumSelector.onValueChanged.AddListener(delegate
            // {
            //     StadiumSelected();
            // });
        }

        // Sends the Request to get features from the service
        private IEnumerator GetFeatures()
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
                CreateGameObjectsFromResponse(request.downloadHandler.text);
                // PopulateStadiumDropdown();
            }
        }

        // Creates the Request Headers to be used in our HTTP Request
        // f=json is the output format
        // where=1=1 gets every feature. geometry based or more intelligent where clauses should be used
        //     with larger datasets
        // outSR=4326 gets the return geometries in the SR 4326
        // outFields=LEAGUE,TEAM,NAME specifies the fields we want in the response
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
            
                if(i < outFields.Length - 1)
                {
                    outFieldHeader += ",";
                }
            }

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
                    var localCoords = _arcGisMapComponent.View.GeographicToWorld(position);
                    pointsList.Add(
                        new Vector2((float)(localCoords.x - _rootPos.x), (float)(localCoords.z - _rootPos.z)));
                }

                var vertices2D = pointsList.ToArray();

                var gameObject = new GameObject(feature.attributes.BuildingNa);
                gameObject.transform.SetParent(_locationComponent.transform);
                
                _polyExtruder = gameObject.AddComponent<PolyExtruder>();
                _polyExtruder.isOutlineRendered = false;
                
                _polyExtruder.createPrism(feature.attributes.BuildingNa, 50.0f, vertices2D, 
                    new Color32(23, 103, 194, 255), true, false, true);
                // TODO: Add mesh collider to walls
                // gameObject.AddComponent<MeshCollider>();
            }
        }
        
        private static GameObject FindChildWithTag(GameObject parent, string tag)
        {
            return (from Transform transform in parent.transform where transform.CompareTag(tag) select transform.gameObject).FirstOrDefault();
        }
        
        // ADDITIONAL REFERENCE STUFF:
        
        // Populates the stadium drown down with all the stadium names from the Stadiums list
        // private void PopulateStadiumDropdown()
        // {
        //     //Populate Stadium name drop down
        //     List<string> StadiumNames = new List<string>();
        //     foreach (GameObject Stadium in Stadiums)
        //     {
        //         StadiumNames.Add(Stadium.name);
        //     }
        //     StadiumNames.Sort();
        //     StadiumSelector.AddOptions(StadiumNames);
        // }

        // When a new entry is selected in the stadium dropdown move the camera to the new position
        // private void StadiumSelected()
        // {
        //     var StadiumName = StadiumSelector.options[StadiumSelector.value].text;
        //     foreach (GameObject Stadium in Stadiums)
        //     {
        //         if(StadiumName == Stadium.name)
        //         {
        //             var StadiumLocation = Stadium.GetComponent<ArcGISLocationComponent>();
        //             if (StadiumLocation == null)
        //             {
        //                 return;
        //             }
        //             var CameraLocation = ArcGISCamera.GetComponent<ArcGISLocationComponent>();
        //             double Longitude = StadiumLocation.Position.X;
        //             double Latitude  = StadiumLocation.Position.Y;
        //
        //             ArcGISPoint NewPosition = new ArcGISPoint(Longitude, Latitude, StadiumSpawnHeight, StadiumLocation.Position.SpatialReference);
        //
        //             CameraLocation.Position = NewPosition;
        //             CameraLocation.Rotation = StadiumLocation.Rotation;
        //         }
        //     }
        // }
    }
}