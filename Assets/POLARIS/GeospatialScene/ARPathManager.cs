using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Google.XR.ARCoreExtensions;
using POLARIS.MainScene;
using QuickEye.UIToolkit;
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
        
        private readonly List<GameObject> _pathAnchorObjects = new();
        // private LineRenderer _lineRenderer;
        private ArrowPoint _arrow;

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
            // _lineRenderer = gameObject.AddComponent<LineRenderer>();
            // _lineRenderer.enabled = false;
            // _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            // _lineRenderer.widthMultiplier = 0.2f;
            // _lineRenderer.startColor = Color.blue;
            // _lineRenderer.endColor = Color.cyan;
            // _lineRenderer.positionCount = 0;
            // _lineRenderer.numCapVertices = 6;
            // _lineRenderer.numCornerVertices = 6;
            
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
                StartCoroutine(ToggleRouting(PersistData.Routing));
            }
            
            if (!PersistData.Routing || _pathAnchorObjects.Count < 2) return;
            
            // Disable past route points
            var closest = GetClosestPathPoint();
                
            // _lineRenderer.positionCount = _pathAnchorObjects.Count - closest;
            // _lineRenderer.SetPositions(_pathAnchorObjects.Skip(closest).Select(anchor => anchor.transform.position).ToArray());

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
            
            UpdateRouteInfo(percentage);
        }

        public void ClearPath(List<GameObject> anchorObjects)
        {
            foreach (var anchor in _pathAnchorObjects)
            {
                anchorObjects.Remove(anchor);
            }
            _pathAnchorObjects.Clear();
            // _lineRenderer.positionCount = 0;
            // _lineRenderer.enabled = false;
            _arrow.SetEnabled(false);
        }

        public void LoadPathAnchors(List<GameObject> anchorObjects, ARAnchorManager anchorManager)
        {
            // remove old anchors
            ClearPath(anchorObjects);

            foreach (var point in PersistData.PathPoints)
            {
                PlacePathGeospatialAnchor(point, anchorObjects, anchorManager);
            }

            // _lineRenderer.positionCount = PersistData.PathPoints.Count;
            // _lineRenderer.enabled = PersistData.Routing;
            _arrow.SetEnabled(PersistData.Routing);
            
            _routingSrcLabel.text = PersistData.SrcName;
            _routingDestLabel.text = PersistData.DestName;
            _routingInfoLabel.text = $"Routing - {PersistData.TravelMinutes:0} min. ({PersistData.TravelMiles:0.0} mi.)";

            if (PersistData.Routing)
            {
                _lastRouting = PersistData.Routing;
                StartCoroutine(ToggleRouting(PersistData.Routing));
            }
        }

        private ARGeospatialAnchor PlacePathGeospatialAnchor(
                                                IReadOnlyList<double> point,
                                                ICollection<GameObject> anchorObjects,
                                                ARAnchorManager anchorManager)
        {
            var promise =
                anchorManager.ResolveAnchorOnTerrainAsync(
                    point[0], point[1],
                    0, Quaternion.Euler(90, 0, 90));

            StartCoroutine(CheckTerrainPromise(promise, anchorObjects));
            return null;
        }
        
        private IEnumerator CheckTerrainPromise(ResolveAnchorOnTerrainPromise promise,
                                                ICollection<GameObject> anchorObjects)
        {
            yield return promise;

            var result = promise.Result;
            
            if (result.TerrainAnchorState != TerrainAnchorState.Success ||
                result.Anchor == null) yield break;

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

            if (smallestDist > 50)
            {
                // TODO: Auto reroute
                Debug.Log("Should recalculate route!");
            }

            return smallestIndex;
        }

        private void UpdateRouteInfo(float pointPercent)
        {
            _routingInfoLabel.text = $"Routing - {(PersistData.TravelMinutes*pointPercent):0} min. ({(PersistData.TravelMiles*pointPercent):0.0} mi.)";
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
        }

        private IEnumerator ToggleRouting(bool routing)
        {
            if (routing)
            {
                _routingBox.style.left = Length.Percent(-80f);
                _routingBox.ToggleDisplayStyle(true);
                _routingBox.style.left = Length.Percent(-5f);
            }
            else
            {
                _routingBox.style.left = Length.Percent(-80f);
                yield return new WaitForSeconds(0.3f);
                _routingBox.ToggleDisplayStyle(false);
            }
        }
    }
}
