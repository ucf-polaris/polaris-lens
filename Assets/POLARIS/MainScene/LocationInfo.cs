using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class LocationInfo : MonoBehaviour
{
    private const string BaseApiUrl = "https://api.ucfpolaris.com";
    private const string BuildingQueryUrl = BaseApiUrl + "/building/scan";
    // TODO: Remove hardcoded tokens on release
    private const string Token = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3Mjg1NDUwOTF9.RPs4A5MjKsXqxIpR4ZL5xKsyqzcI8jqWuCXXKivFMWoghpD3KYdas-FXwv8MfE0kFmc1x3o5fWCEaU6xZwe_zg";
    private const string RefreshToken = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3Mjg1NDUxMzN9.M8YQeGM6m4WNh4TDK4mgVLbUH3whGa64tpi78IwVQIm8L2_VBG-PlxTPBbBcem6236b_1Sfsk20H6W2VqN_oRQ";
    
    private void Start()
    {
        StartCoroutine(RequestLocations());
    }

    private static IEnumerator RequestLocations()
    {
        var payload =
            new JObject(
                new JProperty("radius", 100),
                new JProperty("latitude", 28.60615907398381),
                new JProperty("longitude", -81.19795797388768)
            );
        
        var www = UnityWebRequest.Post(BuildingQueryUrl, payload.ToString(), "application/json");
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
    public string BuildingDesc;
    public string[] BuildingEvents;
    public string BuildingName;
    public int BuildingAltitude;
    public string BuildingLocationType;
    public string BuildingImage;
    public string BuildingAddress;
    public string[] BuildingAllias;
    public string[] BuildingAbbreviation;
    public double BuildingLong;
    public double BuildingLat;
}

public static class Locations
{
    public static Building[] LocationList;
}
