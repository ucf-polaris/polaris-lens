using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Esri.HPFramework;
using POLARIS.MainScene;
using Unity.Mathematics;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class GetUserCurrentLocation : MonoBehaviour
{
    public GameObject LocationMarker;
    public GameObject raycaster;
    public float DesiredAccuracy;
    
    public static float _longitude;
    public static float _latitude;
    public static bool displayLocation;
    private float lonMin = -81.209995f;
    private float lonMax = -81.181589f;
    private float latMin = 28.580255f;
    private float latMax = 28.613986f;
    
    public bool testing = false;
    public float testingLong;
    public float testingLat;
    
    private ArcGISMapComponent _arcGisMapComponent;
    private HPRoot _hpRoot;

    private int numUpdates = 0;

    private bool updating, creating, created, elevating = false;
    
    private void Start()
    {
        _hpRoot = FindObjectOfType<HPRoot>();
        _arcGisMapComponent = FindObjectOfType<ArcGISMapComponent>();
        StartCoroutine(LocationCoroutine());
    }

    private void Update()
    {
        if (!testing)
        {
            // Location service running and in bounds of map
            if (Input.location.status == LocationServiceStatus.Running &&
                PointInBounds(Input.location.lastData.longitude, Input.location.lastData.latitude)  &&
                (_longitude != Input.location.lastData.longitude || _latitude != Input.location.lastData.latitude))
            {
                displayLocation = true;
                _longitude = Input.location.lastData.longitude;
                _latitude = Input.location.lastData.latitude;
                string info = $"lat: {_latitude}, long: {_longitude}, updates: {numUpdates}";
                Debug.Log(info);
                if (!created && !creating) CreateMarker();
                else if (created && !updating) UpdateLocationMarker();
                
                Input.location.Stop();
                StartCoroutine(LocationCoroutine());
            }
            else
            {
                displayLocation = false;
            }
        }
        else
        {
            displayLocation = true;
            if (!created && !creating) CreateMarker();
            else if (created && !updating)
            {
                _latitude = testingLat;
                _longitude = testingLong;
                string info = $"lat: {_latitude}, long: {_longitude}, updates: {numUpdates}";
                Debug.Log(info);
                UpdateLocationMarker();
            }
        }
    }
    
    IEnumerator LocationCoroutine() 
    {
    // Uncomment if you want to test with Unity Remote
/*#if UNITY_EDITOR
        yield return new WaitWhile(() => !UnityEditor.EditorApplication.isRemoteConnected);
        yield return new WaitForSecondsRealtime(5f);
#endif*/
#if UNITY_EDITOR
        // No permission handling needed in Editor
#elif UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.CoarseLocation)) {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.CoarseLocation);
        }

        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser) {
            // TODO Failure
            Debug.LogFormat("Android - Location not enabled");
            yield break;
        }

#elif UNITY_IOS
        if (!Input.location.isEnabledByUser) {
            // TODO Failure
            Debug.LogFormat("IOS - Location not enabled");
            yield break;
        }
#endif
        yield return new WaitForSecondsRealtime(3);
        
        // Start service before querying location
        Input.location.Start(DesiredAccuracy, DesiredAccuracy);
                
        // Wait until service initializes
        int maxWait = 15;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0) {
            yield return new WaitForSecondsRealtime(1);
            maxWait--;
        }

        // Editor has a bug which doesn't set the service status to Initializing. So extra wait in Editor.
#if UNITY_EDITOR
        int editorMaxWait = 15;
        while (Input.location.status == LocationServiceStatus.Stopped && editorMaxWait > 0) {
            yield return new WaitForSecondsRealtime(1);
            editorMaxWait--;
        }
#endif

        // Service didn't initialize in 15 seconds
        if (maxWait < 1) {
            // TODO Failure
            Debug.LogFormat("Timed out");
            yield break;
        }

        // Connection has failed
        if (Input.location.status != LocationServiceStatus.Running) 
        {
            // TODO Failure
            Debug.LogFormat("Unable to determine device location. Failed with status {0}", Input.location.status);
            yield break;
        } 
        else
        {
            Debug.LogFormat("Location service live. status {0}", Input.location.status);
            // Access granted and location value could be retrieved
            Debug.LogFormat("Location: " 
                + Input.location.lastData.latitude + " " 
                + Input.location.lastData.longitude + " " 
                + Input.location.lastData.altitude + " " 
                + Input.location.lastData.horizontalAccuracy + " " 
                + Input.location.lastData.timestamp);
        }
    }

    private void CreateMarker()
    {
        StartCoroutine(CreateLocationMarker());
    }
    
    private IEnumerator CreateLocationMarker()
    {
        creating = true;
        
        CreateRaycasterComponent();
        StartCoroutine(CreateMarkerWithCorrectElevation());
        yield return new WaitWhile(() => elevating);
        
        Debug.Log("Marker created!");
        creating = false;
        created = true;
    }
    
    private void CreateRaycasterComponent()
    {
        var component = raycaster.AddComponent<ArcGISLocationComponent>();
        component.Position = new ArcGISPoint(_longitude, _latitude, 2f, new ArcGISSpatialReference(4326));
    }

    private void UpdateLocationMarker()
    {
        StartCoroutine(UpdateLocationMarkerComponent());
    }
    
    private IEnumerator UpdateLocationMarkerComponent()
    {
        updating = true;
        
        var markerComponent = LocationMarker.GetComponent<ArcGISLocationComponent>();
        Destroy(markerComponent);
        yield return new WaitWhile(() => markerComponent != null);
        
        var raycasterComponent = raycaster.GetComponent<ArcGISLocationComponent>();
        Destroy(raycasterComponent);
        yield return new WaitWhile(() => raycasterComponent != null);
        
        CreateRaycasterComponent();
        
        StartCoroutine(CreateMarkerWithCorrectElevation());
        yield return new WaitWhile(() => elevating);
        
        Debug.Log("Marker updated!");
        numUpdates++;
        
        updating = false;
    }
    
    private IEnumerator CreateMarkerWithCorrectElevation()
    {
        elevating = true;
        // start the raycast in the air at an arbitrary to ensure it is above the ground
        const int raycastHeight = 1000;
        var position = raycaster.transform.position;
        var raycastStart = new Vector3(position.x, position.y + raycastHeight, position.z);

        if (!Physics.Raycast(raycastStart, Vector3.down, out var hitInfo))
        {
            elevating = false;
            yield break;
        }
        
        var location = LocationMarker.AddComponent<ArcGISLocationComponent>();
        location.Position = HitToGeoPosition(hitInfo, 2f);
        Debug.Log("Hit da cast");
        elevating = false;
    }
    
    private ArcGISPoint HitToGeoPosition(RaycastHit hit, float yOffset = 0)
    {
        var worldPosition = math.inverse(_arcGisMapComponent.WorldMatrix).HomogeneousTransformPoint(hit.point.ToDouble3());

        var geoPosition = _arcGisMapComponent.View.WorldToGeographic(worldPosition);
        var offsetPosition = new ArcGISPoint(geoPosition.X, geoPosition.Y, geoPosition.Z + yOffset, geoPosition.SpatialReference);

        return GeoUtils.ProjectToSpatialReference(offsetPosition, new ArcGISSpatialReference(4326));
    }

    private bool PointInBounds(float lon, float lat)
    {
        return lon >= lonMin && lon <= lonMax && lat >= latMin && lat <= latMax;
    }
}