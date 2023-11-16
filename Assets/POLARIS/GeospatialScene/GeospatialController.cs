// <copyright file="GeospatialController.cs" company="Google LLC">
//
// Copyright 2022 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

using POLARIS.GeospatialScene;
using POLARIS.MainScene;
using POLARIS.Managers;
using Unity.Mathematics;

namespace Google.XR.ARCoreExtensions.Samples.Geospatial
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    using UnityEngine.XR.ARFoundation;
    using UnityEngine.XR.ARSubsystems;
#if UNITY_ANDROID

    using UnityEngine.Android;
#endif

    /// <summary>
    /// Controller for Geospatial sample.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:ParameterMustNotSpanMultipleLines",
        Justification = "Bypass source check.")]
    public class GeospatialController : MonoBehaviour
    {
        private LocationManager locationManager;
        
        [Header("AR Components")]

        /// <summary>
        /// The ARSessionOrigin used in the sample.
        /// </summary>
        public ARSessionOrigin SessionOrigin;

        /// <summary>
        /// The ARSession used in the sample.
        /// </summary>
        public ARSession Session;

        /// <summary>
        /// The ARAnchorManager used in the sample.
        /// </summary>
        public ARAnchorManager AnchorManager;

        /// <summary>
        /// The ARRaycastManager used in the sample.
        /// </summary>
        public ARRaycastManager RaycastManager;

        /// <summary>
        /// The AREarthManager used in the sample.
        /// </summary>
        public AREarthManager EarthManager;

        /// <summary>
        /// The ARStreetscapeGeometryManager used in the sample.
        /// </summary>
        public ARStreetscapeGeometryManager StreetscapeGeometryManager;
        
        public ARPathManager PathManager;

        public PanelManager PanelManager;

        /// <summary>
        /// The ARCoreExtensions used in the sample.
        /// </summary>
        public ARCoreExtensions ARCoreExtensions;

        /// <summary>
        /// The StreetscapeGeometry materials for rendering geometry building meshes.
        /// </summary>
        public List<Material> StreetscapeGeometryMaterialBuilding;

        /// <summary>
        /// The StreetscapeGeometry material for rendering geometry terrain meshes.
        /// </summary>
        public Material StreetscapeGeometryMaterialTerrain;

        [Header("UI Elements")]
        
        /// <summary>
        /// UI element showing privacy prompt.
        /// </summary>
        public GameObject PrivacyPromptCanvas;

        /// <summary>
        /// UI element showing VPS availability notification.
        /// </summary>
        public GameObject VPSCheckCanvas;

        /// <summary>
        /// UI element containing all AR view contents.
        /// </summary>
        public GameObject ARViewCanvas;

        /// <summary>
        /// Text displaying <see cref="GeospatialPose"/> information at runtime.
        /// </summary>
        public Text InfoText;

        /// <summary>
        /// Text displaying in a snack bar at the bottom of the screen.
        /// </summary>
        public Text SnackBarText;

        /// <summary>
        /// Help message shown while localizing.
        /// </summary>
        private const string LocalizingMessage = "Localizing your device to set anchor.";

        /// <summary>
        /// Help message shown while initializing Geospatial functionalities.
        /// </summary>
        private const string LocalizationInitializingMessage =
            "Initializing Geospatial functionalities.";

        /// <summary>
        /// Help message shown when <see cref="AREarthManager.EarthTrackingState"/> is not tracking
        /// or the pose accuracies are beyond thresholds.
        /// </summary>
        private const string LocalizationInstructionMessage =
            "Point your camera at buildings, landmarks, and signs near you.";

        /// <summary>
        /// Help message shown when location fails or hits timeout.
        /// </summary>
        private const string LocalizationFailureMessage =
            "Localization not possible.\n" +
            "Close and open the app to restart the session.";

        /// <summary>
        /// Help message shown when localization is completed.
        /// </summary>
        private const string LocalizationSuccessMessage = "Localization completed.";

        /// <summary>
        /// The timeout period waiting for localization to be completed.
        /// </summary>
        private const float TimeoutSeconds = 180;

        /// <summary>
        /// Indicates how long a information text will display on the screen before terminating.
        /// </summary>
        private const float ErrorDisplaySeconds = 3;

        /// <summary>
        /// The key name used in PlayerPrefs which indicates whether the privacy prompt has
        /// displayed at least one time.
        /// </summary>
        private const string HasDisplayedPrivacyPromptKey = "HasDisplayedGeospatialPrivacyPrompt";

        /// <summary>
        /// Accuracy threshold for orientation yaw accuracy in degrees that can be treated as
        /// localization completed.
        /// </summary>
        private const double OrientationYawAccuracyThreshold = 25;

        /// <summary>
        /// Accuracy threshold for heading degree that can be treated as localization completed.
        /// </summary>
        private const double HeadingAccuracyThreshold = 25;

        /// <summary>
        /// Accuracy threshold for altitude and longitude that can be treated as localization
        /// completed.
        /// </summary>
        private const double HorizontalAccuracyThreshold = 20;

        /// <summary>
        /// Represents the current anchor type of the anchor being placed in the scene.
        /// </summary>
        private AnchorType _anchorType = AnchorType.Geospatial;

        /// <summary>
        /// Determines if streetscape geometry is rendered in the scene.
        /// </summary>
        private bool _streetscapeGeometryVisibility = false;

        /// <summary>
        /// Determines which building material will be used for the current building mesh.
        /// </summary>
        private int _buildingMatIndex = 0;

        /// <summary>
        /// Dictionary of streetscapeGeometry handles to render objects for rendering
        /// streetscapeGeometry meshes.
        /// </summary>
        private Dictionary<TrackableId, GameObject> _streetscapeGeometryGOs = new();

        /// <summary>
        /// ARStreetscapeGeometries added in the last Unity Update.
        /// </summary>
        List<ARStreetscapeGeometry> _addedStreetscapeGeometries = new();

        /// <summary>
        /// ARStreetscapeGeometries updated in the last Unity Update.
        /// </summary>
        List<ARStreetscapeGeometry> _updatedStreetscapeGeometries = new();

        /// <summary>
        /// ARStreetscapeGeometries removed in the last Unity Update.
        /// </summary>
        List<ARStreetscapeGeometry> _removedStreetscapeGeometries = new();

        /// <summary>
        /// Determines if streetscape geometry should be removed from the scene.
        /// </summary>
        private bool _clearStreetscapeGeometryRenderObjects = false;

        private bool _waitingForLocationService = false;
        private bool _isInARView = false;
        private bool _isReturning = false;
        private bool _isLocalizing = false;
        private bool _enablingGeospatial = false;
        private bool _shouldResolvingHistory = false;
        private float _localizationPassedTime = 0f;
        private float _configurePrepareTime = 3f;
        private readonly List<GameObject> _anchorObjects = new();
        private IEnumerator _startLocationService = null;
        private IEnumerator _asyncCheck = null;
        
        private bool _lastRouting = false;

        /// <summary>
        /// Callback handling "Get Started" button click event in Privacy Prompt.
        /// </summary>
        public void OnGetStartedClicked()
        {
            PlayerPrefs.SetInt(HasDisplayedPrivacyPromptKey, 1);
            PlayerPrefs.Save();
            SwitchToARView(true);
        }

        /// <summary>
        /// Callback handling "Learn More" Button click event in Privacy Prompt.
        /// </summary>
        public void OnLearnMoreClicked()
        {
            Application.OpenURL(
                "https://developers.google.com/ar/data-privacy");
        }

        /// <summary>
        /// Callback handling "Continue" button click event in AR View.
        /// </summary>
        public void OnContinueClicked()
        {
            VPSCheckCanvas.SetActive(false);
        }

        /// <summary>
        /// Callback handling "Geometry" toggle event in AR View.
        /// </summary>
        /// <param name="enable">Whether to enable Streetscape Geometry visibility.</param>
        public void OnGeometryToggled(bool enable)
        {
            _streetscapeGeometryVisibility = enable;
            if (!_streetscapeGeometryVisibility)
            {
                _clearStreetscapeGeometryRenderObjects = true;
            }
        }

        private void Start()
        {
            locationManager = LocationManager.getInstance();
        }
        
        /// <summary>
        /// Unity's Awake() method.
        /// </summary>
        public void Awake()
        {
            // Lock screen to portrait.
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.orientation = ScreenOrientation.Portrait;

            // Enable geospatial sample to target 60fps camera capture frame rate
            // on supported devices.
            // Note, Application.targetFrameRate is ignored when QualitySettings.vSyncCount != 0.
            Application.targetFrameRate = 60;

            if (SessionOrigin == null)
            {
                Debug.LogError("Cannot find ARSessionOrigin.");
            }

            if (Session == null)
            {
                Debug.LogError("Cannot find ARSession.");
            }

            if (ARCoreExtensions == null)
            {
                Debug.LogError("Cannot find ARCoreExtensions.");
            }
        }

        /// <summary>
        /// Unity's OnEnable() method.
        /// </summary>
        public void OnEnable()
        {
            _startLocationService = StartLocationService();
            StartCoroutine(_startLocationService);

            _isReturning = false;
            _enablingGeospatial = false;

            _localizationPassedTime = 0f;
            _isLocalizing = true;
            SnackBarText.text = LocalizingMessage;

            SwitchToARView(PlayerPrefs.HasKey(HasDisplayedPrivacyPromptKey));

            if (StreetscapeGeometryManager == null)
            {
                Debug.LogWarning("StreetscapeGeometryManager must be set in the " +
                    "GeospatialController Inspector to render StreetscapeGeometry.");
            }

            if (StreetscapeGeometryMaterialBuilding.Count == 0)
            {
                Debug.LogWarning("StreetscapeGeometryMaterialBuilding in the " +
                    "GeospatialController Inspector must contain at least one material " +
                    "to render StreetscapeGeometry.");
                return;
            }

            if (StreetscapeGeometryMaterialTerrain == null)
            {
                Debug.LogWarning("StreetscapeGeometryMaterialTerrain must be set in the " +
                    "GeospatialController Inspector to render StreetscapeGeometry.");
                return;
            }

            // get access to ARstreetscapeGeometries in ARStreetscapeGeometryManager
            if (StreetscapeGeometryManager)
            {
                StreetscapeGeometryManager.StreetscapeGeometriesChanged += GetStreetscapeGeometry;
            }
        }

        /// <summary>
        /// Unity's OnDisable() method.
        /// </summary>
        public void OnDisable()
        {
            StopCoroutine(_asyncCheck);
            _asyncCheck = null;
            StopCoroutine(_startLocationService);
            _startLocationService = null;
            // Debug.Log("Stop location services.");
            // Input.location.Stop();

            PathManager.ClearPath();
            PersistData.Routing = false;

            foreach (var anchor in _anchorObjects)
            {
                Destroy(anchor);
            }
            _anchorObjects.Clear();
            
            PanelManager.ClearPanels();

            if (StreetscapeGeometryManager)
            {
                StreetscapeGeometryManager.StreetscapeGeometriesChanged -=
                    GetStreetscapeGeometry;
            }
        }

        /// <summary>
        /// Unity's Update() method.
        /// </summary>
        public void Update()
        {
            if (!_isInARView)
            {
                return;
            }

            UpdateDebugInfo();

            // Check session error status.
            LifecycleUpdate();
            if (_isReturning)
            {
                return;
            }

            if (ARSession.state != ARSessionState.SessionInitializing &&
                ARSession.state != ARSessionState.SessionTracking)
            {
                return;
            }

            // Check feature support and enable Geospatial API when it's supported.
            var featureSupport = EarthManager.IsGeospatialModeSupported(GeospatialMode.Enabled);
            switch (featureSupport)
            {
                case FeatureSupported.Unknown:
                    return;
                case FeatureSupported.Unsupported:
                    ReturnWithReason("The Geospatial API is not supported by this device.");
                    return;
                case FeatureSupported.Supported:
                    if (ARCoreExtensions.ARCoreExtensionsConfig.GeospatialMode ==
                        GeospatialMode.Disabled)
                    {
                        Debug.Log("Geospatial sample switched to GeospatialMode.Enabled.");
                        ARCoreExtensions.ARCoreExtensionsConfig.GeospatialMode =
                            GeospatialMode.Enabled;
                        ARCoreExtensions.ARCoreExtensionsConfig.StreetscapeGeometryMode =
                            StreetscapeGeometryMode.Enabled;
                        _configurePrepareTime = 3.0f;
                        _enablingGeospatial = true;
                        return;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Waiting for new configuration to take effect.
            if (_enablingGeospatial)
            {
                _configurePrepareTime -= Time.deltaTime;
                if (_configurePrepareTime < 0)
                {
                    _enablingGeospatial = false;
                }
                else
                {
                    return;
                }
            }

            // Check earth state.
            var earthState = EarthManager.EarthState;
            if (earthState == EarthState.ErrorEarthNotReady)
            {
                SnackBarText.text = LocalizationInitializingMessage;
                return;
            }
            if (earthState != EarthState.Enabled)
            {
                var errorMessage =
                    "Geospatial sample encountered an EarthState error: " + earthState;
                Debug.LogWarning(errorMessage);
                SnackBarText.text = errorMessage;
                return;
            }

            // Check earth localization.
            var isSessionReady = ARSession.state == ARSessionState.SessionTracking &&
                                 Input.location.status == LocationServiceStatus.Running;
            var earthTrackingState = EarthManager.EarthTrackingState;
            var pose = earthTrackingState == TrackingState.Tracking ?
                EarthManager.CameraGeospatialPose : new GeospatialPose();
            if (!isSessionReady || earthTrackingState != TrackingState.Tracking ||
                pose.OrientationYawAccuracy > OrientationYawAccuracyThreshold ||
                pose.HorizontalAccuracy > HorizontalAccuracyThreshold)
            {
                // Lost localization during the session.
                if (!_isLocalizing)
                {
                    _isLocalizing = true;
                    _localizationPassedTime = 0f;
                    foreach (var go in _anchorObjects)
                    {
                        go.SetActive(false);
                    }
                }

                if (_localizationPassedTime > TimeoutSeconds)
                {
                    Debug.LogError("Geospatial sample localization timed out.");
                    ReturnWithReason(LocalizationFailureMessage);
                }
                else
                {
                    _localizationPassedTime += Time.deltaTime;
                    SnackBarText.text = LocalizationInstructionMessage;
                }
            }
            else if (_isLocalizing)
            {
                // Finished localization.
                _isLocalizing = false;
                _localizationPassedTime = 0f;
                SnackBarText.text = LocalizationSuccessMessage;
                foreach (var go in _anchorObjects)
                {
                    go.SetActive(true);
                }
            }
            else
            {
                // Screetscape Geometry Rendering
                // if (_streetscapeGeometryVisibility)
                // {
                //     foreach (
                //         var streetscapeGeometry in _addedStreetscapeGeometries)
                //     {
                //         InstantiateRenderObject(streetscapeGeometry);
                //     }
                //
                //     foreach (
                //         var streetscapeGeometry in _updatedStreetscapeGeometries)
                //     {
                //         // This second call to instantiate is required if geometry is toggled on
                //         // or off after the app has started.
                //         InstantiateRenderObject(streetscapeGeometry);
                //         UpdateRenderObject(streetscapeGeometry);
                //     }
                //
                //     foreach (
                //         var streetscapeGeometry in _removedStreetscapeGeometries)
                //     {
                //         DestroyRenderObject(streetscapeGeometry);
                //     }
                // }
                // else if (_clearStreetscapeGeometryRenderObjects)
                // {
                //     DestroyAllRenderObjects();
                //     _clearStreetscapeGeometryRenderObjects = false;
                // }

                // Load in nearby panels (wait for locations array to be loaded)
                if (locationManager.dataList is not null)
                {
                    PanelManager.FetchNearbyIfNeeded(new double2(pose.Latitude, pose.Longitude), _anchorObjects);
                }
                // Load/unload panels depending on distance
                PanelManager.LoadNearby();
                
                // Load path
                if (PersistData.Routing != _lastRouting)
                {
                    _lastRouting = PersistData.Routing;

                    if (PersistData.Routing && PersistData.PathPoints != null)
                    {
                        PathManager.LoadPathAnchors(_anchorObjects, AnchorManager);
                    }
                }
            }
            
            if (earthTrackingState == TrackingState.Tracking)
            {
                var acc = pose.HorizontalAccuracy switch
                {
                    < 1  => "Magnificent",
                    < 2  => "Excellent",
                    < 5  => "Great",
                    < 10 => "Good",
                    < 20 => "OK",
                    _ => "Waiting"
                };
                if (pose.OrientationYawAccuracy < OrientationYawAccuracyThreshold)
                {
                    acc = "Waiting";
                }

                InfoText.text = string.Format(
                "Accuracy: {8}",
                Environment.NewLine, pose.Latitude, pose.Longitude, 
                pose.HorizontalAccuracy, pose.Altitude, pose.VerticalAccuracy,
                pose.EunRotation.ToString("F1"), pose.OrientationYawAccuracy, acc);
            }
            else
            {
                InfoText.text = "Not tracking :(";
            }
        }

        /// <summary>
        /// Connects the <c>ARStreetscapeGeometry</c> to the specified lists for access.
        /// </summary>
        /// <param name="eventArgs">The
        /// <c><see cref="ARStreetscapeGeometriesChangedEventArgs"/></c> containing the
        /// <c>ARStreetscapeGeometry</c>.
        /// </param>
        private void GetStreetscapeGeometry(ARStreetscapeGeometriesChangedEventArgs eventArgs)
        {
            _addedStreetscapeGeometries = eventArgs.Added;
            _updatedStreetscapeGeometries = eventArgs.Updated;
            _removedStreetscapeGeometries = eventArgs.Removed;
        }

        /// <summary>
        /// Sets up a render object for this <c>ARStreetscapeGeometry</c>.
        /// </summary>
        /// <param name="streetscapeGeometry">The
        /// <c><see cref="ARStreetscapeGeometry"/></c> object containing the mesh
        /// to be rendered.</param>
        private void InstantiateRenderObject(ARStreetscapeGeometry streetscapeGeometry)
        {
            if (streetscapeGeometry.mesh == null)
            {
                return;
            }

            // Check if a render object already exists for this streetscapeGeometry and
            // create one if not.
            if (_streetscapeGeometryGOs.ContainsKey(streetscapeGeometry.trackableId))
            {
                return;
            }

            var renderObject = new GameObject(
                "StreetscapeGeometryMesh", typeof(MeshFilter), typeof(MeshRenderer));

            if (!renderObject) return;
            
            renderObject.transform.position = new Vector3(0, 0.5f, 0);
            renderObject.GetComponent<MeshFilter>().mesh = streetscapeGeometry.mesh;

            // Add a material with transparent diffuse shader.
            if (streetscapeGeometry.streetscapeGeometryType ==
                StreetscapeGeometryType.Building)
            {
                renderObject.GetComponent<MeshRenderer>().material =
                    StreetscapeGeometryMaterialBuilding[_buildingMatIndex];
                _buildingMatIndex =
                    (_buildingMatIndex + 1) % StreetscapeGeometryMaterialBuilding.Count;
            }
            else
            {
                renderObject.GetComponent<MeshRenderer>().material =
                    StreetscapeGeometryMaterialTerrain;
            }

            renderObject.transform.position = streetscapeGeometry.pose.position;
            renderObject.transform.rotation = streetscapeGeometry.pose.rotation;

            _streetscapeGeometryGOs.Add(streetscapeGeometry.trackableId, renderObject);
        }

        /// <summary>
        /// Updates the render object transform based on this StreetscapeGeometries pose.
        /// It must be called every frame to update the mesh.
        /// </summary>
        /// <param name="streetscapeGeometry">The <c><see cref="ARStreetscapeGeometry"/></c>
        /// object containing the mesh to be rendered.</param>
        private void UpdateRenderObject(ITrackable streetscapeGeometry)
        {
            if (!_streetscapeGeometryGOs.ContainsKey(streetscapeGeometry.trackableId)) return;
            
            var renderObject = _streetscapeGeometryGOs[streetscapeGeometry.trackableId];
            renderObject.transform.position = streetscapeGeometry.pose.position;
            renderObject.transform.rotation = streetscapeGeometry.pose.rotation;
        }

        /// <summary>
        /// Destroys the render object associated with the
        /// <c><see cref="ARStreetscapeGeometry"/></c>.
        /// </summary>
        /// <param name="streetscapeGeometry">The <c><see cref="ARStreetscapeGeometry"/></c>
        /// containing the render object to be destroyed.</param>
        private void DestroyRenderObject(ITrackable streetscapeGeometry)
        {
            if (!_streetscapeGeometryGOs.ContainsKey(streetscapeGeometry.trackableId)) return;
            
            var geometry = _streetscapeGeometryGOs[streetscapeGeometry.trackableId];
            _streetscapeGeometryGOs.Remove(streetscapeGeometry.trackableId);
            Destroy(geometry);
        }

        /// <summary>
        /// Destroys all stored <c><see cref="ARStreetscapeGeometry"/></c> render objects.
        /// </summary>
        private void DestroyAllRenderObjects()
        {
            var keys = _streetscapeGeometryGOs.Keys;
            foreach (var key in keys)
            {
                var renderObject = _streetscapeGeometryGOs[key];
                Destroy(renderObject);
            }

            _streetscapeGeometryGOs.Clear();
        }

        private static float GetRooftopAnchorScale(Vector3 anchor, Vector3 camera)
        {
            // Return the scale in range [1, 2] after mapping a distance between camera and anchor
            // to [2, 20].
            var distance =
                Mathf.Sqrt(
                    Mathf.Pow(anchor.x - camera.x, 2.0f)
                    + Mathf.Pow(anchor.y - camera.y, 2.0f)
                    + Mathf.Pow(anchor.z - camera.z, 2.0f));
            var mapDistance = Mathf.Min(Mathf.Max(2.0f, distance), 20.0f);
            return (mapDistance - 2.0f) / (20.0f - 2.0f) + 1.0f;
        }

        public List<GameObject> GetAnchorObjects()
        {
            return _anchorObjects;
        }

        private GeospatialAnchorHistory CreateHistory(Pose pose, AnchorType anchorType)
        {
            var geospatialPose = EarthManager.Convert(pose);

            var history = new GeospatialAnchorHistory(
                geospatialPose.Latitude, geospatialPose.Longitude, geospatialPose.Altitude,
                anchorType, geospatialPose.EunRotation);
            return history;
        }

        private static Quaternion CreateRotation(GeospatialAnchorHistory history)
        {
            var eunRotation = history.EunRotation;
            if (eunRotation == Quaternion.identity)
            {
                // This history is from a previous app version and EunRotation was not used.
                eunRotation =
                    Quaternion.AngleAxis(180f - (float)history.Heading, Vector3.up);
            }

            return eunRotation;
        }

        private void SwitchToARView(bool enable)
        {
            _isInARView = enable;
            SessionOrigin.gameObject.SetActive(enable);
            Session.gameObject.SetActive(enable);
            ARCoreExtensions.gameObject.SetActive(enable);
            ARViewCanvas.SetActive(enable);
            PrivacyPromptCanvas.SetActive(!enable);
            VPSCheckCanvas.SetActive(false);
            if (!enable || _asyncCheck != null) return;
            _asyncCheck = AvailabilityCheck();
            StartCoroutine(_asyncCheck);
        }

        private IEnumerator AvailabilityCheck()
        {
            if (ARSession.state == ARSessionState.None)
            {
                yield return ARSession.CheckAvailability();
            }

            // Waiting for ARSessionState.CheckingAvailability.
            yield return null;

            if (ARSession.state == ARSessionState.NeedsInstall)
            {
                yield return ARSession.Install();
            }

            // Waiting for ARSessionState.Installing.
            yield return null;
#if UNITY_ANDROID

            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Debug.Log("Requesting camera permission.");
                Permission.RequestUserPermission(Permission.Camera);
                yield return new WaitForSeconds(3.0f);
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                // User has denied the request.
                Debug.LogWarning(
                    "Failed to get the camera permission. VPS availability check isn't available.");
                yield break;
            }
#endif

            while (_waitingForLocationService)
            {
                yield return null;
            }

            if (Input.location.status != LocationServiceStatus.Running)
            {
                Debug.LogWarning(
                    "Location services aren't running. VPS availability check is not available.");
                yield break;
            }

            // Update event is executed before coroutines so it checks the latest error states.
            if (_isReturning)
            {
                yield break;
            }

            var location = Input.location.lastData;
            var vpsAvailabilityPromise =
                AREarthManager.CheckVpsAvailabilityAsync(location.latitude, location.longitude);
            yield return vpsAvailabilityPromise;

            Debug.LogFormat("VPS Availability at ({0}, {1}): {2}",
                location.latitude, location.longitude, vpsAvailabilityPromise.Result);
            VPSCheckCanvas.SetActive(vpsAvailabilityPromise.Result != VpsAvailability.Available);
        }

        private IEnumerator StartLocationService()
        {
            _waitingForLocationService = true;
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Debug.Log("Requesting the fine location permission.");
                Permission.RequestUserPermission(Permission.FineLocation);
                yield return new WaitForSeconds(3.0f);
            }
#endif

            if (!Input.location.isEnabledByUser)
            {
                Debug.Log("Location service is disabled by the user.");
                _waitingForLocationService = false;
                yield break;
            }

            Debug.Log("Starting location service.");
            Input.location.Start();

            while (Input.location.status == LocationServiceStatus.Initializing)
            {
                yield return null;
            }

            _waitingForLocationService = false;
            
            if (Input.location.status == LocationServiceStatus.Running) yield break;
            
            Debug.LogWarningFormat(
                "Location service ended with {0} status.", Input.location.status);
            Input.location.Stop();
        }

        private void LifecycleUpdate()
        {
            // Pressing 'back' button quits the app.
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                Application.Quit();
            }

            if (_isReturning)
            {
                return;
            }

            // Only allow the screen to sleep when not tracking.
            var sleepTimeout = SleepTimeout.NeverSleep;
            if (ARSession.state != ARSessionState.SessionTracking)
            {
                sleepTimeout = SleepTimeout.SystemSetting;
            }

            Screen.sleepTimeout = sleepTimeout;

            // Quit the app if ARSession is in an error status.
            var returningReason = string.Empty;
            if (ARSession.state != ARSessionState.CheckingAvailability &&
                ARSession.state != ARSessionState.Ready &&
                ARSession.state != ARSessionState.SessionInitializing &&
                ARSession.state != ARSessionState.SessionTracking)
            {
                returningReason =
                    $"Geospatial sample encountered an ARSession error state {ARSession.state}.\n" +
                    "Please restart the app.";
            }
            else if (Input.location.status == LocationServiceStatus.Failed)
            {
                returningReason =
                    "Geospatial sample failed to start location service.\n" +
                    "Please restart the app and grant the fine location permission.";
            }
            else if (SessionOrigin == null || Session == null || ARCoreExtensions == null)
            {
                returningReason = "Geospatial sample failed due to missing AR Components.";
            }

            ReturnWithReason(returningReason);
        }

        private void ReturnWithReason(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return;
            }

            Debug.LogError(reason);
            SnackBarText.text = reason;
            _isReturning = true;
            Invoke(nameof(QuitApplication), ErrorDisplaySeconds);
        }

        private void QuitApplication()
        {
            Application.Quit();
        }

        private void UpdateDebugInfo()
        {
            if (!Debug.isDebugBuild || EarthManager == null)
            {
                return;
            }

            var pose = EarthManager.EarthState == EarthState.Enabled &&
                EarthManager.EarthTrackingState == TrackingState.Tracking ?
                EarthManager.CameraGeospatialPose : new GeospatialPose();
            var supported = EarthManager.IsGeospatialModeSupported(GeospatialMode.Enabled);
            // DebugText.text =
            //     $"IsReturning: {_isReturning}\n" +
            //     $"IsLocalizing: {_isLocalizing}\n" +
            //     $"SessionState: {ARSession.state}\n" +
            //     $"LocationServiceStatus: {Input.location.status}\n" +
            //     $"FeatureSupported: {supported}\n" +
            //     $"EarthState: {EarthManager.EarthState}\n" +
            //     $"EarthTrackingState: {EarthManager.EarthTrackingState}\n" +
            //     $"  LAT/LNG: {pose.Latitude:F6}, {pose.Longitude:F6}\n" +
            //     $"  HorizontalAcc: {pose.HorizontalAccuracy:F6}\n" +
            //     $"  ALT: {pose.Altitude:F2}\n" +
            //     $"  VerticalAcc: {pose.VerticalAccuracy:F2}\n" +
            //     $". EunRotation: {pose.EunRotation:F2}\n" +
            //     $"  OrientationYawAcc: {pose.OrientationYawAccuracy:F2}";
        }
    }
}
