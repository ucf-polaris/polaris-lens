using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Google.XR.ARCoreExtensions;
using Google.XR.ARCoreExtensions.Samples.Geospatial;
using POLARIS.MainScene;
using QuickEye.UIToolkit;
using Unity.Mathematics;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;

namespace POLARIS.GeospatialScene
{
    public class ARPathManager : MonoBehaviour
    {
        public Camera Camera;
        public GameObject PathPrefab;
        public GameObject RouteInfo;
        public PanelManager PanelManager;
        public GeospatialController GeospatialController;
        
        private readonly List<GameObject> _pathAnchorObjects = new();
        private ArrowPoint _arrow;

        private float _closeTime = 0;
        private bool _lastRouting = false;
        private bool _closed = false;
        private Label _routingSrcLabel;
        private Label _routingDestLabel;
        private Label _routingInfoLabel;
        private VisualElement _routingBox;
        private Button _slideButton;
        private Button _stopButton;

        private void Start()
        {
            PathPrefab = Resources.Load("Polaris/RaisedArrow") as GameObject;
            
            var goList = new List<GameObject>();
            Camera.gameObject.GetChildGameObjects(goList);
            _arrow = goList.Find(go => go.name.Equals("Arrow")).GetComponent<ArrowPoint>();
            
            var rootVisual = RouteInfo.GetComponent<UIDocument>().rootVisualElement;
            _routingSrcLabel = rootVisual.Q<Label>("RoutingSrc");
            _routingDestLabel = rootVisual.Q<Label>("RoutingDest");
            _routingInfoLabel = rootVisual.Q<Label>("RoutingInfos");
            _routingBox = rootVisual.Q<VisualElement>("RoutingInfo");
            _slideButton = rootVisual.Q<Button>("SlideButton");
            _stopButton = rootVisual.Q<Button>("StopButton");

            _lastRouting = PersistData.Routing;
            _routingBox.ToggleDisplayStyle(false);
            _slideButton.clickable.clicked += ToggleSlide;
            _stopButton.clickable.clicked += StopClicked;
        }

        // Update is called once per frame
        private void Update()
        {
            if (PersistData.Routing != _lastRouting)
            {
                _lastRouting = PersistData.Routing;
                StartCoroutine(UcfRouteManager.ToggleRoutingBox(false, _routingBox, _closeTime));
            }
            
            if (!PersistData.Routing || _pathAnchorObjects.Count < 2) return;
            
            // Disable past route points
            var closest = GetClosestPathPoint();
            for (var i = 0; i < _pathAnchorObjects.Count; i++)
            {
                _pathAnchorObjects[i].gameObject.SetActive(i >= closest);
            }

            // Set arrows
            for (var i = closest; i < _pathAnchorObjects.Count - 1; i++)
            {
                var direction = _pathAnchorObjects[i + 1].transform.position -
                                _pathAnchorObjects[i].transform.position;
                var lookRot = Quaternion.LookRotation(direction, Vector3.up);
                
                _pathAnchorObjects[i].transform.rotation = Quaternion.Euler(lookRot.eulerAngles.x, lookRot.eulerAngles.y, lookRot.eulerAngles.z);
            }

            var percentage = UcfRouteManager.PointPercentage(
                _pathAnchorObjects.Select(anchor => anchor.transform.position).ToArray(), closest);

            _routingInfoLabel.text = UcfRouteManager.GetUpdatedRouteText(percentage);
        }

        public void ClearPath()
        {
            foreach (var anchor in _pathAnchorObjects)
            {
                GeospatialController.GetAnchorObjects().Remove(anchor);
                Destroy(anchor);
            }
            _pathAnchorObjects.Clear(); 
            if (_arrow) _arrow.SetEnabled(false);
        }

        public void LoadPathAnchors(List<GameObject> anchorObjects, ARAnchorManager anchorManager)
        {
            Debug.Log("Loading Path...");
            // remove old anchors
            ClearPath();

            foreach (var point in PersistData.PathPoints)
            {
                Debug.Log("zzw point " + point[0] + ", " + point[1]);
                PlacePathGeospatialAnchor(
                    PanelManager.SmallTestMode 
                        ? new double2(PanelManager.TestMakeSmallDist(point[0], PanelManager.TestCenterCoords.x),
                                      PanelManager.TestMakeSmallDist(point[1], PanelManager.TestCenterCoords.y))
                    : point, 
                    anchorObjects, 
                    anchorManager);
            }

            _arrow.SetEnabled(true);
            
            _routingSrcLabel.text = PersistData.SrcName;
            _routingDestLabel.text = PersistData.DestName;
            _routingInfoLabel.text = $"Routing - {PersistData.TravelMinutes:0} min. ({PersistData.TravelMiles:0.0} mi.)";
            
            _closeTime = Time.time;
            StartCoroutine(UcfRouteManager.ToggleRoutingBox(true, _routingBox, _closeTime));
        }

        private ARGeospatialAnchor PlacePathGeospatialAnchor(
                                                double2 point,
                                                ICollection<GameObject> anchorObjects,
                                                ARAnchorManager anchorManager)
        {
            Debug.Log("ZZ Placing routing anchor at " + point.x + ", " + point.y);
            
            var promise =
                anchorManager.ResolveAnchorOnTerrainAsync(
                    point[0], point[1], 0, Quaternion.Euler(90, 0, 90));

            StartCoroutine(CheckTerrainPromise(promise, anchorObjects));
            return null;
        }
        
        private IEnumerator CheckTerrainPromise(ResolveAnchorOnTerrainPromise promise,
                                                ICollection<GameObject> anchorObjects)
        {
            yield return promise;

            var result = promise.Result;
            
            if (result.TerrainAnchorState != TerrainAnchorState.Success ||
                result.Anchor == null)
            {
                Debug.LogError("Failed to set a routing terrain anchor!");
                yield break;
            }

            var resultGo = result.Anchor.gameObject;
            var anchorGo = Instantiate(PathPrefab,
                                       resultGo.transform);
            anchorGo.transform.parent = resultGo.transform;

            anchorObjects.Add(resultGo);
            _pathAnchorObjects.Add(anchorGo);
        }

        private int GetClosestPathPoint()
        {
            var smallestDist = float.MaxValue;
            var smallestIndex = 0;
            for (var i = 0; i < _pathAnchorObjects.Count; i++)
            {
                var dist = Vector3.Distance(_pathAnchorObjects[i].transform.position,
                                            Camera.transform.position);
                if (dist < smallestDist)
                {
                    smallestDist = dist;
                    smallestIndex = i;
                }
            }

            // if (smallestDist > 50)
            // {
            //     // TODO: Auto reroute
            //     Debug.Log("Should recalculate route!");
            // }

            return smallestIndex;
        }

        private void ToggleSlide()
        {
            _closed = !_closed;
            _routingBox.style.left = Length.Percent(_closed ? -67f : -5f);
            _slideButton.style.rotate = new Rotate(_closed ? 180 : 0);
            _stopButton.style.width = _stopButton.style.height = _closed ? 0 : 100;
        }

        private void StopClicked()
        {
            PersistData.Routing = false;
            StartCoroutine(UcfRouteManager.ToggleRoutingBox(false, _routingBox, _closeTime));
            PersistData.ClearStops();
            ClearPath();
        }
    }
}
