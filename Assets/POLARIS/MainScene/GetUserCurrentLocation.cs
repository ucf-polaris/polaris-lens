using UnityEngine;
using System.Collections;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Esri.HPFramework;
using POLARIS.MainScene;
using Unity.Mathematics;
using UnityEngine.Serialization;

public class GetUserCurrentLocation : MonoBehaviour
{
    public GameObject LocationMarker;
    public float DesiredAccuracy;
    
    private float _longitude = 0f;
    private float _latitude = 0f;
    private float lonMin = -81.209995f;
    private float lonMax = -81.181589f;
    private float latMin = 28.580255f;
    private float latMax = 28.613986f;
    
    private ArcGISMapComponent _arcGisMapComponent;
    private HPRoot _hpRoot;
    private void Start()
    {
        _hpRoot = FindObjectOfType<HPRoot>();
        _arcGisMapComponent = FindObjectOfType<ArcGISMapComponent>();
        StartCoroutine(LocationCoroutine());
        CreateLocationMarker();
    }

    private void Update()
    {
        // if (_latitude != 0f) Debug.Log($"Latitude: {_latitude}");
        // if (_longitude != 0f) Debug.Log($"Longitude: {_longitude}");
        
        // Location service running and location updated
        if (Input.location.status == LocationServiceStatus.Running &&
            PointInBounds(Input.location.lastData.longitude, Input.location.lastData.latitude) &&
            (_longitude != Input.location.lastData.longitude || _latitude != Input.location.lastData.latitude))
        {
            _longitude = Input.location.lastData.longitude;
            _latitude = Input.location.lastData.latitude;
            UpdateLocationMarker(_longitude, _latitude);
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
            
            _longitude = Input.location.lastData.longitude;
            _latitude = Input.location.lastData.latitude;
        }
        
        // Stop service if there is no need to query location updates continuously
        // Input.location.Stop();
    }

    private void CreateLocationMarker()
    {
        // Random default coordinates I chose
        _longitude = -81.1991396266178f;
        _latitude = 28.6024642823258f;
        CreateLocationMarkerComponent(_longitude, _latitude);
        SetElevation(LocationMarker);
        Debug.Log("Marker created!");
    }
    
    private void CreateLocationMarkerComponent(float lon, float lat)
    {
        var location = LocationMarker.AddComponent<ArcGISLocationComponent>();
        location.Position = new ArcGISPoint(lon, lat, 2f, new ArcGISSpatialReference(4326));
    }

    private void UpdateLocationMarker(float lon, float lat)
    {
        UpdateLocationMarkerComponent(lon, lat);
        SetElevation(LocationMarker);
        Debug.Log("Marker updated!");
    }
    
    private void UpdateLocationMarkerComponent(float lon, float lat)
    {
        var location = LocationMarker.GetComponent<ArcGISLocationComponent>();
        location.Position = new ArcGISPoint(lon, lat, 2f, new ArcGISSpatialReference(4326));
    }
    
    private void SetElevation(GameObject locationMarker)
    {
        // start the raycast in the air at an arbitrary to ensure it is above the ground
        const int raycastHeight = 1000;
        var position = locationMarker.transform.position;
        var raycastStart = new Vector3(position.x, position.y + raycastHeight, position.z);
            
        if (!Physics.Raycast(raycastStart, Vector3.down, out var hitInfo)) return;
            
        var location = locationMarker.GetComponent<ArcGISLocationComponent>();
        location.Position = HitToGeoPosition(hitInfo, 2f);
    }
    
    private ArcGISPoint HitToGeoPosition(RaycastHit hit, float yOffset = 0)
    {
        var worldPosition = math.inverse(_arcGisMapComponent.WorldMatrix).HomogeneousTransformPoint(hit.point.ToDouble3());

        var geoPosition = _arcGisMapComponent.View.WorldToGeographic(worldPosition);
        var offsetPosition = new ArcGISPoint(geoPosition.X, geoPosition.Y, geoPosition.Z + yOffset, geoPosition.SpatialReference);

        var spatialRef = GeoUtils.ProjectToSpatialReference(offsetPosition, new ArcGISSpatialReference(4326));
        PersistData.DestinationPoint = spatialRef;
                
        return GeoUtils.ProjectToSpatialReference(offsetPosition, new ArcGISSpatialReference(4326));
    }

    private bool PointInBounds(float lon, float lat)
    {
        return lon >= lonMin && lon <= lonMax && lat >= latMin && lat <= latMax;
    }
}