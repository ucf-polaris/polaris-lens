using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

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
                if (!string.IsNullOrEmpty(data.BuildingImage))
                {
                    if (data.rawImage == null)
                        StartCoroutine(data.DownloadImage("https://knightconnect.campuslabs.com/engage/image/" + data.BuildingImage));
                }
                else
                {
                    data.rawImage = Resources.Load<Texture2D>("UCF_Logo_2");
                }
            }
        }
        public List<LocationData> GetBuildingsFromSearch(string query, bool fuzzySearch, bool returnAll)
        {
            const int TOLERANCE = 1;

            List<LocationData> buildings = new List<LocationData>();
            foreach (LocationData building in dataList)
            {
                if (building.BuildingName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (fuzzySearch && FuzzyMatch(building.BuildingName, query, TOLERANCE)))
                {
                    Debug.Log("Result: " + building.BuildingName);
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
        private string[] buildingEvents;
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

        public LocationData()
        {
            //LoadPlayerPrefs();
        }

        public void LoadPlayerPrefs()
        {
            //implement getting events from player prefs here
        }

        public void SetPlayerPrefs()
        {
            //Up to you how you want to do this (if at all)
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

