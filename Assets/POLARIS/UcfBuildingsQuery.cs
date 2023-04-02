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
using UnityEngine.Serialization;
using UnityEngine.UI;
using Newtonsoft.Json;
using UnityEditor;
using Random = UnityEngine.Random;

namespace POLARIS
{
    // The follow System.Serializable classes are used to define the REST API response
// in order to leverage Unity's JsonUtility.
// When implementing your own version of this the Baseball Properties would need to 
// be updated.
    [System.Serializable]
    public class FeatureCollectionData
    {
        public string geometryType;
        public List<Feature> features;
    }

    [System.Serializable]
    public class Feature
    {
        public Geometry geometry;
        public BuildingProperties attributes;
    }

    [System.Serializable]
    public class BuildingProperties
    {
        public string BuildingNa;
        public string BuildingNu;
    }

    [System.Serializable]
    public class Geometry
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
    
        // This prefab will be instantiated for each feature we parse
        // public GameObject StadiumPrefab;

        // The height where we spawn the stadium before finding the ground height
        private const int BuildingSpawnHeight = 10000;

        // This will hold a reference to each feature we created
        public List<GameObject> Buildings = new List<GameObject>();

        // In the query request we can denote the Spatial Reference we want the return geometries in.
        // It is important that we create the GameObjects with the same Spatial Reference
        private const int FeatureSRWKID = 4326;

        // This camera reference will be passed to the stadiums to calculate the distance from the camera to each stadium
        // public ArcGISCameraComponent ArcGISCamera;

        // public Dropdown StadiumSelector;

        private ArcGISMapComponent _arcGisMapComponent;
        private ArcGISLocationComponent _locationComponent;
        private ArcGISPoint _rootPos;

        private PolyExtruder _polyExtruder;

        // Get all the features when the script starts
        private void Start()
        {
            _arcGisMapComponent = FindObjectOfType<ArcGISMapComponent>();
            _locationComponent = FindChildWithTag(_arcGisMapComponent.gameObject, "Location").GetComponent<ArcGISLocationComponent>();
            _rootPos = _locationComponent.Position;
            
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

            string queryRequestURL = FeatureLayerURL + "/Query?" + MakeRequestHeaders();
            Debug.Log(queryRequestURL);
            UnityWebRequest request = UnityWebRequest.Get(queryRequestURL);
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
        // f=geojson is the output format
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

            string outFieldHeader = "outFields=";
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

            string returnValue = "";
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

            // Debug.Log(_rootPos.X);

            foreach (Feature feature in deserialized.features)
            {
                // var shape = new Esri.GameEngine.Geometry.ArcGISPolygonBuilder(ArcGISSpatialReference.WGS84());
                // foreach (var pair in feature.geometry.rings[0])
                // {
                //     shape.AddPoint(pair[0], pair[1]);
                // }
                //
                // ArcGISGeometry polygon = shape.ToGeometry();
                // polygon.

                var pointsList = new List<Vector2>();
                foreach (var pair in feature.geometry.rings[0])
                {
                    pointsList.Add(new Vector2((float)((pair[0] - _rootPos.X) * 111139), (float)((pair[1] - _rootPos.Y) * 111139)));
                }

                var vertices2D = pointsList.ToArray();
                vertices2D.Reverse();
                
                // Debug.Log(vertices2D.Select(i => i.ToString()).Aggregate((i, j) => i + j));

                // var vertices3D = System.Array.ConvertAll<Vector2, Vector3>(vertices2D, 
                //     v => new Vector3(v.x, 0.0f, v.y));

                // var triangulator = new Triangulator(vertices2D);
                // var indices = triangulator.Triangulate();
                
                // var colors = Enumerable.Range(0, vertices3D.Length)
                //     .Select(i => Random.ColorHSV())
                //     .ToArray();

                var gameObject = new GameObject(feature.attributes.BuildingNa);
                gameObject.transform.SetParent(_locationComponent.transform);
                // gameObject.AddComponent<MeshCollider>();
                _polyExtruder = gameObject.AddComponent<PolyExtruder>();
                _polyExtruder.isOutlineRendered = false;
                
                _polyExtruder.createPrism(feature.attributes.BuildingNa, 50.0f, vertices2D, 
                    new Color32(23, 103, 194, 255), true, false, true);

                // var mesh = new Mesh
                // {
                //     vertices = vertices3D,
                //     triangles = indices,
                //     colors = colors
                // };
                //
                // Debug.Log(vertices3D[0][0]);
                //
                // mesh.RecalculateNormals();
                // mesh.RecalculateBounds();
                //
                // Material mat = Resources.Load("SampleMat", typeof(Material)) as Material;
                //
                // var gameObject = new GameObject(feature.attributes.BuildingNa,
                //     typeof(MeshFilter), typeof(MeshRenderer));
                //
                // gameObject.transform.SetParent(_locationComponent.transform);
                // gameObject.GetComponent<MeshFilter>().mesh = mesh;
                // gameObject.GetComponent<MeshRenderer>().material = mat;

                // ArcGISPoint position = new ArcGISPoint(longitude, latitude, BuildingSpawnHeight, new ArcGISSpatialReference(FeatureSRWKID));

                // var newStadium = Instantiate(StadiumPrefab, this.transform);
                // newStadium.name = feature.Properties.NAME;
                // Stadiums.Add(newStadium);
                // newStadium.SetActive(true);
                //
                // var LocationComponent = newStadium.GetComponent<ArcGISLocationComponent>();
                // LocationComponent.enabled = true;
                // LocationComponent.Position = position;
                //
                // var StadiumInfo = newStadium.GetComponent<StadiumInfo>();
                //
                // StadiumInfo.SetInfo(feature.Properties.NAME);
                // StadiumInfo.SetInfo(feature.Properties.TEAM);
                // StadiumInfo.SetInfo(feature.Properties.League);
                //
                // StadiumInfo.ArcGISCamera = ArcGISCamera;
                // StadiumInfo.SetSpawnHeight(StadiumSpawnHeight);
            }
        }
        
        private GameObject FindChildWithTag(GameObject parent, string tag) {
            GameObject child = null;
     
            foreach(Transform transform in parent.transform) {
                if(transform.CompareTag(tag)) {
                    child = transform.gameObject;
                    break;
                }
            }
     
            return child;
        }
        

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