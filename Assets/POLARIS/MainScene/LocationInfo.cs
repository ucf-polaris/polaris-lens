using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class LocationInfo : MonoBehaviour
{
    private const string BaseApiUrl = "https://api.ucfpolaris.com";
    private const string BuildingQueryUrl = BaseApiUrl + "/building/scan";
    
    private void Start()
    {
        StartCoroutine(RequestLocations());
    }

    // TODO: GET THIS WORKING!!
    private static IEnumerator RequestLocations()
    {
        var payload =
            new JObject(
                new JProperty("token", PlayerPrefs.GetString("AuthToken")),
                new JProperty("radius", 100),
                new JProperty("latitude", 28.60615907398381),
                new JProperty("longitude", -81.19795797388768)
            );
        
        var www = UnityWebRequest.Post(BuildingQueryUrl, payload.ToString(), "application/json");

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
            
            PlayerPrefs.SetString("AuthToken", jsonResponse["token"]!.Value<string>());

            var buildings = jsonResponse["locations"]!.ToObject<Building[]>();
            Locations.LocationList = buildings;
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
    public string BuildingAlias;
    public string BuildingAbbreviation;
    public double BuildingLong;
    public double BuildingLat;
}

public static class Locations
{
    public static Building[] LocationList;
}
