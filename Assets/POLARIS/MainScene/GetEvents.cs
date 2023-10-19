using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class GetEvents : MonoBehaviour
{
    private const string BaseApiUrl = "https://api.ucfpolaris.com";
    private const string EventQueryURL = BaseApiUrl + "/event/scan";
    private const string Token = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3Mjg1NDUwOTF9.RPs4A5MjKsXqxIpR4ZL5xKsyqzcI8jqWuCXXKivFMWoghpD3KYdas-FXwv8MfE0kFmc1x3o5fWCEaU6xZwe_zg";
    private const string RefreshToken = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3Mjg1NDUxMzN9.M8YQeGM6m4WNh4TDK4mgVLbUH3whGa64tpi78IwVQIm8L2_VBG-PlxTPBbBcem6236b_1Sfsk20H6W2VqN_oRQ";
    
    private void Start()
    {
        StartCoroutine(RequestEvents());
    }

    private static IEnumerator RequestEvents()
    {
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
            
            var events = jsonResponse["locations"]!.ToObject<Event[]>();
            Events.EventList = events;

            foreach (Event UCFEvent in Events.EventList)
            {
                Debug.Log($"{UCFEvent.name} has description {UCFEvent.description}");
            }
        }
    }
}

public class Event
{
    public string name;
    public string description;
    public Location location;
    public string host;
    public DateTime dateTime;
    public DateTime endsOn;
    public string image;
    public string listedLocation;
    public string EventID;
    public string locationQueryID;
    public long timeTilExpire;
}

public class Location
{
    public double BuildingLat;
    public double BuildingLong;
}

public static class Events
{
    public static Event[] EventList;
}
