using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace POLARIS.Managers
{
    public class EventManager : BaseManager
    {
        private static EventManager Instance;
        private UserManager userAccess;

        private const string TestingToken = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3Mjg1NDUwOTF9.RPs4A5MjKsXqxIpR4ZL5xKsyqzcI8jqWuCXXKivFMWoghpD3KYdas-FXwv8MfE0kFmc1x3o5fWCEaU6xZwe_zg";
        private const string TestingRefreshToken = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3Mjg1NDUxMzN9.M8YQeGM6m4WNh4TDK4mgVLbUH3whGa64tpi78IwVQIm8L2_VBG-PlxTPBbBcem6236b_1Sfsk20H6W2VqN_oRQ";
        private const string BaseApiUrl = "https://api.ucfpolaris.com";
        private const string EventQueryURL = BaseApiUrl + "/event/scan";
        private const string EventGetURL = BaseApiUrl + "/event/get";

        public List<EventData> dataList;
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
                userAccess = UserManager.getInstance();
            }
        }

        private void Start()
        {
            //populates dataList
            CallScan();
        }

        static public EventManager getInstance()
        {
            return Instance;
        }

        //scans all elements right away
        public void CallScan()
        {
            if(running == null && Testing == false)
            {
                running = Scan(null);
                StartCoroutine(running);
            }
        }

        override protected IEnumerator Scan(IDictionary<string, string> request)
        {
            string Token = TestingToken;
            string RefreshToken = TestingRefreshToken;
            if (!Testing && UserManager.isNotNull())
            {
                Token = userAccess.data.Token;
                RefreshToken = userAccess.data.RefreshToken;
            }
            var www = UnityWebRequest.Post(EventQueryURL, null, "application/json");
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

                var events = jsonResponse["locations"]!.ToObject<List<EventData>>();
                dataList = events;

                foreach (EventData UCFEvent in dataList)
                {
                    Debug.Log($"{UCFEvent.Name} has description {UCFEvent.Description}");
                }
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
            UnityWebRequest www = UnityWebRequest.Post(EventGetURL, payload.ToString(), "application/json");
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
        public List<EventData> GetEventsFromSearch(string query, bool fuzzySearch)
        {
            const int TOLERANCE = 1;

            List<EventData> events = new List<EventData>();
            foreach (EventData UCFEvent in dataList)
            {
                if (UCFEvent.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (fuzzySearch && FuzzyMatch(UCFEvent.Name, query, TOLERANCE)))
                {
                    events.Add(UCFEvent);
                }
            }

            return events;
        }

    }
    [Serializable]
    public class EventData
    {
        [SerializeField]
        private string name;
        [SerializeField]
        private string description;
        [SerializeField]
        private Location location;
        [SerializeField]
        private string host;
        [SerializeField]
        private DateTime dateTime;
        [SerializeField]
        private DateTime endsOn;
        [SerializeField]
        private string image;
        [SerializeField]
        private string listedLocation;
        [SerializeField]
        private string EventID;
        [SerializeField]
        private string locationQueryID;
        [SerializeField]
        private long timeTilExpire;

        public EventData()
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

        #region Getters and Setters
        //this was all generated by VS
        public string Name { get => name; set => name = value; }
        public string Description { get => description; set => description = value; }
        public Location Location { get => location; set => location = value; }
        public string Host { get => host; set => host = value; }
        public DateTime DateTime { get => dateTime; set => dateTime = value; }
        public DateTime EndsOn { get => endsOn; set => endsOn = value; }
        public string Image { get => image; set => image = value; }
        public string ListedLocation { get => listedLocation; set => listedLocation = value; }
        public string EventID1 { get => EventID; set => EventID = value; }
        public string LocationQueryID { get => locationQueryID; set => locationQueryID = value; }
        public long TimeTilExpire { get => timeTilExpire; set => timeTilExpire = value; }
        #endregion
    }
    public class Location
    {
        public double BuildingLat;
        public double BuildingLong;
    }
}

