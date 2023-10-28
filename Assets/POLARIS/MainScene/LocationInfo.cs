using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class LocationInfo : MonoBehaviour
{
    private const string BaseApiUrl = "https://v21x6ajyg9.execute-api.us-east-2.amazonaws.com/dev";
    private const string BuildingQueryUrl = BaseApiUrl + "/building/scan";
    // TODO: Remove hardcoded tokens on release
    private const string Token = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3Mjg1NDUwOTF9.RPs4A5MjKsXqxIpR4ZL5xKsyqzcI8jqWuCXXKivFMWoghpD3KYdas-FXwv8MfE0kFmc1x3o5fWCEaU6xZwe_zg";
    private const string RefreshToken = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3Mjg1NDUxMzN9.M8YQeGM6m4WNh4TDK4mgVLbUH3whGa64tpi78IwVQIm8L2_VBG-PlxTPBbBcem6236b_1Sfsk20H6W2VqN_oRQ";
    public bool testing = false;

    private void Start()
    {
        if (!testing)
            StartCoroutine(RequestLocations());
        else
        {
            Locations.LocationList = new Building[20];
            for(int i = 0; i < Locations.LocationList.Length; i++)
            {
                Locations.LocationList[i] = new Building();
            }
        }
            
    }

    private static IEnumerator RequestLocations()
    {
        var www = UnityWebRequest.Post(BuildingQueryUrl, null, "application/json");
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
            
            var buildings = jsonResponse["locations"]!.ToObject<Building[]>();
            Locations.LocationList = buildings;

            foreach (Building building in Locations.LocationList)
            {
                Debug.Log($"{building.BuildingName} has description {building.BuildingDesc}");
            }
        }
    }
}

public class Building
{
    public string BuildingName;
    public string[] BuildingAllias;
    public string[] BuildingAbbreviation;
    public string BuildingDesc;
    public double BuildingLong;
    public double BuildingLat;
    public string BuildingAddress;
    public string[] BuildingEvents;
    public int BuildingAltitude;
    public string BuildingLocationType;
    public string BuildingImage;

    //testing constructor
    public Building()
    {
        BuildingName = "Nicholson School of Communication and Media";
        BuildingAllias = new string[] { "bb", "ba" };
        BuildingAbbreviation = new string[] { "bb", "ba" };
        BuildingDesc = "A historic building";
        BuildingLong = -81.20296054416477;
        BuildingLat = 28.59790334799203;
        BuildingAddress = "none";
        BuildingEvents = new string[] { };
        BuildingAltitude = 100;
        BuildingLocationType = "100";
        BuildingImage = "fewafes";
    }
}

public static class Locations
{
    public static Building[] LocationList;
}
