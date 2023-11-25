using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using POLARIS.MainScene;
using System.Linq;
using Unity.Mathematics;
using UnityEngine.SceneManagement;

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

        public List<EventData> dataList = new List<EventData>();
        public bool Testing;

        public CallStatus ScanStatus = CallStatus.NotStarted;

        public event EventHandler ImageDownloaded;
        public event EventHandler ScanSucceed;

        static public EventManager getInstance()
        {
            return Instance;
        }

        #region Monobehaviors
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
            //populates dataList
            userAccess = UserManager.getInstance();
            CallScan();
        }
        #endregion

        #region EventFunctions
        private void OnImageDownloaded()
        {
            if (ImageDownloaded != null)
            {
                ImageDownloaded(this, EventArgs.Empty);
            }
        }

        private void OnScanSucceed()
        {
            if (ScanSucceed != null)
            {
                ScanSucceed(this, EventArgs.Empty);
            }
        }
        #endregion

        #region EndpointFunctions

        //scans all elements right away
        public void CallScan()
        {
            if (running == null && Testing == false)
            {
                running = Scan(null);
                StartCoroutine(running);
            }
        }

        override protected IEnumerator Scan(IDictionary<string, string> request)
        {
            string Token = TestingToken;
            string RefreshToken = TestingRefreshToken;
            if (UserManager.isNotNull() && !userAccess.Testing)
            {
                if (Token != "") Token = userAccess.data.Token;
                if (RefreshToken != "") RefreshToken = userAccess.data.RefreshToken;
            }
            ScanStatus = CallStatus.InProgress;
            var www = UnityWebRequest.Post(EventQueryURL, null, "application/json");
            www.SetRequestHeader("authorizationToken", "{\"token\":\"" + Token + "\",\"refreshToken\":\"" + RefreshToken + "\"}");
            yield return www.SendWebRequest();

            ScanStatus = CallStatus.Failed;
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                if (www.error.Contains("Forbidden"))
                {
                    UserManager.getInstance().Logout(false);
                    TransitionManager tm = TransitionManager.getInstance();
                    if (tm != null) tm.StartPlay("Login", Transitions.FromTopIn, Transitions.FromTopOut, 0.5f, 0f, 0.5f, 0f);
                    else SceneManager.LoadScene("Login");
                }
            }
            else
            {
                ScanStatus = CallStatus.Succeeded;
                Debug.Log("Form upload complete!");
                Debug.Log("Status Code: " + www.responseCode);
                Debug.Log(www.result);
                Debug.Log("Response: " + www.downloadHandler.text);

                var jsonResponse = JObject.Parse(www.downloadHandler.text);

                var events = jsonResponse["locations"]!.ToObject<List<EventData>>();
                dataList = events;

                // foreach (EventData UCFEvent in dataList)
                // {
                // Debug.Log($"Event {UCFEvent.Name} has description {UCFEvent.Description}");
                // }
                OnScanSucceed();
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

        public IEnumerator DownloadImage(string MediaUrl, EventData e)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(MediaUrl);
            //Debug.Log("Downloading image from " + MediaUrl);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log("ERROR " + request.error);
                Resources.Load<Texture2D>("UCF_Logo");
            }
            else
            {
                e.rawImage = ((DownloadHandlerTexture)request.downloadHandler).texture;
                OnImageDownloaded();
                //Debug.Log(name + " successfully downloaded");
            }
        }

        public void getAllRawImages()
        {
            if (dataList == null) return;
            foreach(var data in dataList)
            {
                //download if image exists (the ones with images are set, add boolean in backend from if it's from knights connect or ucf evetns)
                if (!string.IsNullOrEmpty(data.Image))
                {
                    if (data.rawImage == null)
                    {
                        data.rawImage = Resources.Load<Texture2D>("UCF_Logo");
                        StartCoroutine(DownloadImage("https://knightconnect.campuslabs.com/engage/image/" + data.Image, data));
                    }
                        
                }  
                else
                {
                    data.rawImage = Resources.Load<Texture2D>("UCF_Logo_2");
                }
            }
        }

        #endregion

        #region FilterFunctions
        private bool filterEvents(EventData e, EventFilters filter)
        {
            if (filter == EventFilters.Upcoming) return e.DateTime > DateTime.Now;
            return true;
        }

        private List<EventData> sortEvents(List<EventData> list, EventFilters filter)
        {
            if (filter == EventFilters.DateClosest)
                list = list.OrderBy(c => c.DateTime.Date).ThenBy(c => c.DateTime.TimeOfDay).ToList();
            else if(filter == EventFilters.Upcoming)
                list = list.OrderBy(c => c.DateTime.Date).ThenBy(c => c.DateTime.TimeOfDay).ToList();
            else if (filter == EventFilters.DateFarthest)
                list = list.OrderByDescending(c => c.DateTime.Date).ThenBy(c => c.DateTime.TimeOfDay).ToList();
            else if(filter == EventFilters.Distance)
                list = list.OrderBy(evt => DistanceInMiBetweenEarthCoordinates(new double2(GetUserCurrentLocation._latitude, GetUserCurrentLocation._longitude), new double2(evt.Location.BuildingLat, evt.Location.BuildingLong))).ToList();
            return list;
        }

        public List<EventData> GetEventsFromSearch(string query, bool fuzzySearch, EventFilters eventFilter = EventFilters.None)
        {
            const int TOLERANCE = 1;

            List<EventData> events = new List<EventData>();
            foreach (EventData UCFEvent in dataList)
            {
                if (UCFEvent.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (fuzzySearch && FuzzyMatch(UCFEvent.Name, query, TOLERANCE)))
                {
                    if(filterEvents(UCFEvent, eventFilter))
                     events.Add(UCFEvent);
                }
            }
            events = sortEvents(events, eventFilter);
            return events;
        }
        #endregion

        #region HelperFunctions
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
        #endregion
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
        private string eventID;
        [SerializeField]
        private string locationQueryID;
        [SerializeField]
        private long timeTilExpire;
        public Texture2D rawImage = null;

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
        // Wow that is so cool! -Kevin
        public string Name { get => name; set => name = value; }
        public string Description { get => description; set => description = value; }
        public Location Location { get => location; set => location = value; }
        public string Host { get => host; set => host = value; }
        public DateTime DateTime { get => dateTime; set => dateTime = value; }
        public DateTime EndsOn { get => endsOn; set => endsOn = value; }
        public string Image { get => image; set => image = value; }
        public string ListedLocation { get => listedLocation; set => listedLocation = value; }
        public string EventID { get => eventID; set => eventID = value; }
        public string LocationQueryID { get => locationQueryID; set => locationQueryID = value; }
        public long TimeTilExpire { get => timeTilExpire; set => timeTilExpire = value; }
        #endregion
    }
    public class Location
    {
        public double BuildingLat;
        public double BuildingLong;
    }

    public enum EventFilters
    {
        None,
        DateFarthest,
        DateClosest,
        Distance,
        Upcoming
    }
}

