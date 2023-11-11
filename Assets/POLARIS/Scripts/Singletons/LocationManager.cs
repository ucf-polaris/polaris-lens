using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;
using POLARIS;
using Unity.Mathematics;

namespace POLARIS.Managers
{
    public class LocationManager : BaseManager
    {
        private static LocationManager Instance;
        private UserManager userAccess;

        private const string TestingToken = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3Mjg1NDUwOTF9.RPs4A5MjKsXqxIpR4ZL5xKsyqzcI8jqWuCXXKivFMWoghpD3KYdas-FXwv8MfE0kFmc1x3o5fWCEaU6xZwe_zg";
        private const string TestingRefreshToken = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3Mjg1NDUxMzN9.M8YQeGM6m4WNh4TDK4mgVLbUH3whGa64tpi78IwVQIm8L2_VBG-PlxTPBbBcem6236b_1Sfsk20H6W2VqN_oRQ";
        private const string BaseApiUrl = "https://api.ucfpolaris.com";

        public List<LocationData> dataList;
        public bool Testing;
        void Awake()
        {
            //create singleton
            if (Instance != this && Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                DontDestroyOnLoad(gameObject);
                Instance = this;
            }
        }

        private void Start()
        {
            Debug.Log("Hello location manager!");
            //populates dataList
            userAccess = UserManager.getInstance();
            CallScan();
        }

        static public LocationManager getInstance()
        {
            return Instance;
        }

        //scans all elements right away
        public void CallScan()
        {
            if (running == null && Testing == false)
            {
                Debug.Log("Running Building Scan...");
                running = Scan(null);
                StartCoroutine(running);
            }
        }

        override protected IEnumerator Scan(IDictionary<string, string> request)
        {
            Debug.Log("SCANNING....");
            string Token = TestingToken;
            string RefreshToken = TestingRefreshToken;
            if (UserManager.isNotNull() && !userAccess.Testing)
            {
                if (Token != "") Token = userAccess.data.Token;
                if (RefreshToken != "") RefreshToken = userAccess.data.RefreshToken;
            }
            var www = UnityWebRequest.Post(BaseApiUrl + "/building/scan", null, "application/json");
            www.SetRequestHeader("authorizationToken", "{\"token\":\"" + Token + "\",\"refreshToken\":\"" + RefreshToken + "\"}");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("Form upload complete!");
                Debug.Log("Status Code: " + www.responseCode);
                Debug.Log(www.result);
                Debug.Log("Response: " + www.downloadHandler.text);

                var jsonResponse = JObject.Parse(www.downloadHandler.text);

                var buildings = jsonResponse["locations"]!.ToObject<List<LocationData>>();
                dataList = buildings;

                // foreach (EventData UCFEvent in dataList)
                // {
                // Debug.Log($"Event {UCFEvent.Name} has description {UCFEvent.Description}");
                // }
                getAllRawImages();
            }
            running = null;
            //since this isn't inherent to the building, needs to be set from userAccess
            UpdateFavoritesInBuildings();
            UpdateVisitedInBuildings();
        }

        override protected IEnumerator Get(IDictionary<string, string> request)
        {
            string Token = TestingToken;
            string RefreshToken = TestingRefreshToken;
            if (Testing)
            {
                Token = userAccess.data.Token;
                RefreshToken = userAccess.data.RefreshToken;
            }

            JObject payload =
                new(
                    new JProperty("name", request["name"]),
                    new JProperty("dateTime", request["dateTime"])
                );
            UnityWebRequest www = UnityWebRequest.Post(BaseApiUrl + "/building/get", payload.ToString(), "application/json");
            www.SetRequestHeader("authorizationToken", "{\"token\":\"" + Token + "\",\"refreshToken\":\"" + RefreshToken + "\"}");
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("Form upload complete!");
                Debug.Log("Status Code: " + www.responseCode);
                Debug.Log(www.result);
                JObject jsonResponse = JObject.Parse(www.downloadHandler.text);
                Debug.Log("Response: " + jsonResponse);
            }
            Debug.LogWarning("Implement this");
        }

        override public IEnumerator UpdateFields(IDictionary<string, string> request)
        {
            yield return null;
            Debug.LogWarning("Should not be implement");
        }

        public void getAllRawImages()
        {
            if (dataList == null) return;
            foreach (var data in dataList)
            {
                //download if image exists (the ones with images are set, add boolean in backend from if it's from knights connect or ucf evetns)
                /*if (!string.IsNullOrEmpty(data.BuildingImage))
                {
                    if (data.rawImage == null)
                        StartCoroutine(data.DownloadImage("https://knightconnect.campuslabs.com/engage/image/" + data.BuildingImage));
                }
                else
                {
                    data.rawImage = Resources.Load<Texture2D>("UCF_Logo_2");
                }*/
                data.rawImage = Resources.Load<Texture2D>("UCF_Logo_2");
            }
        }
        public enum LocationFilter
        {
            None,
            Favorites,
            NotVisited,
            Visited,
            Closest,
            Events
        }
        private bool filterLocation(LocationData location, LocationFilter filter)
        {
            if (filter == LocationFilter.Favorites) return location.IsFavorited;
            else if (filter == LocationFilter.NotVisited) return !location.IsVisited;
            else if (filter == LocationFilter.Visited) return location.IsVisited;
            return true;
        }

        private List<LocationData> sortLocations(List<LocationData> list, LocationFilter filter)
        {
            if (filter == LocationFilter.Closest)
                list = list.OrderBy(loc => DistanceInMiBetweenEarthCoordinates(new double2(GetUserCurrentLocation._latitude, GetUserCurrentLocation._longitude), new double2(loc.BuildingLat, loc.BuildingLong))).ToList();
            else if (filter == LocationFilter.Events)
                list = list.OrderByDescending(loc => loc.BuildingEvents.Length).ToList();
            return list;
        }

        public List<LocationData> GetBuildingsFromSearch(string query, bool fuzzySearch, LocationFilter locFilter=LocationFilter.None)
        {
            const int TOLERANCE = 1;

            List<LocationData> buildings = new List<LocationData>();
            foreach (LocationData building in dataList)
            {
                if (building.BuildingName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (fuzzySearch && FuzzyMatch(building.BuildingName, query, TOLERANCE)))
                {
                    Debug.Log("Result: " + building.BuildingName);
                    //filter out based on the location filter
                    if(filterLocation(building, locFilter))
                        buildings.Add(building);
                    continue;
                }

                if (building.BuildingAllias == null) continue;
                foreach (string alias in building.BuildingAllias)
                {
                    if (alias.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (fuzzySearch && FuzzyMatch(alias, query, TOLERANCE)))
                    {
                        //filter out based on the location filter
                        if (filterLocation(building, locFilter))
                            buildings.Add(building);
                        break;
                    }
                }
            }
            buildings = sortLocations(buildings, locFilter);
            return buildings;
        }

        private void LoadPlayerPrefs()
        {
            //implement loading the full list of locations here
        }

        public void SetPlayerPrefs()
        {
            //implement saving the full list of locations here
        }

        private void UpdateFavoritesInBuildings()
        {
            for(var i = 0; i < dataList.Count; i++)
            {
                if (userAccess.isFavorite(dataList[i]))
                {
                    dataList[i].IsFavorited = true;
                }    
            }
        }

        private void UpdateVisitedInBuildings()
        {
            foreach(var building in dataList)
            {
                if (userAccess.isVisited(building))
                    building.IsVisited = true;
            }
        }

        public LocationData GetFromName(string name)
        {
            for (var i = 0; i < dataList.Count; i++)
            {
                if (dataList[i].BuildingName == name)
                    return dataList[i];
            }
            return null;
        }

        public double DistanceInMiBetweenEarthCoordinates(double2 pointA, double2 pointB)
        {
            const int earthRadiusKm = 6371;
            const double KmToMi = 0.621371;

            var distLat = DegreesToRadians(pointB.x - pointA.x);
            var distLon = DegreesToRadians(pointB.y - pointA.y);

            var latA = DegreesToRadians(pointA.x);
            var latB = DegreesToRadians(pointB.x);

            var a = Math.Sin(distLat / 2) * Math.Sin(distLat / 2) +
                    Math.Sin(distLon / 2) * Math.Sin(distLon / 2) * Math.Cos(latA) * Math.Cos(latB);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusKm * c * KmToMi;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }

    [Serializable]
    public class LocationData
    {
        [SerializeField]
        private float buildingLong;
        [SerializeField]
        private float buildingLat;
        [SerializeField]
        private string buildingDesc;
        [SerializeField]
        private string[] buildingEvents = new string[0];
        [SerializeField]
        private string buildingName;
        [SerializeField]
        private float buildingAltitude;
        [SerializeField]
        private string buildingLocationType;
        [SerializeField]
        private string[] buildingAbbreviation;
        [SerializeField]
        private string[] buildingAllias;
        [SerializeField]
        private string buildingAddress;
        [SerializeField]
        private string buildingImage;
        [SerializeField]
        private Location[] buildingEntrances;
        public Texture2D rawImage = null;
        [SerializeField]
        private bool isFavorited;
        [SerializeField]
        private bool isVisited;

        public float BuildingLong { get => buildingLong; set => buildingLong = value; }
        public float BuildingLat { get => buildingLat; set => buildingLat = value; }
        public string BuildingDesc { get => buildingDesc; set => buildingDesc = value; }
        public string[] BuildingEvents { get => buildingEvents; set => buildingEvents = value; }
        public string BuildingName { get => buildingName; set => buildingName = value; }
        public float BuildingAltitude { get => buildingAltitude; set => buildingAltitude = value; }
        public string BuildingLocationType { get => buildingLocationType; set => buildingLocationType = value; }
        public string[] BuildingAbbreviation { get => buildingAbbreviation; set => buildingAbbreviation = value; }
        public string[] BuildingAllias { get => buildingAllias; set => buildingAllias = value; }
        public string BuildingAddress { get => buildingAddress; set => buildingAddress = value; }
        public string BuildingImage { get => buildingImage; set => buildingImage = value; }
        public Location[] BuildingEntrances { get => buildingEntrances; set => buildingEntrances = value; }
        public bool IsFavorited { get => isFavorited; set => isFavorited = value; }
        public bool IsVisited { get => isVisited; set => isVisited = value; }

        public LocationData()
        {
            //LoadPlayerPrefs();
        }

        public IEnumerator DownloadImage(string MediaUrl)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(MediaUrl);
            Debug.Log("Downloading image from " + MediaUrl);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log("ERROR " + request.error);
                Resources.Load<Texture2D>("UCF_Logo");
            }
            else
            {
                rawImage = ((DownloadHandlerTexture)request.downloadHandler).texture;
                Debug.Log(BuildingName + " successfully downloaded");
            }

        }
    }
}

