using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Google.XR.ARCoreExtensions;
using POLARIS.MainScene;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.ARFoundation;

namespace POLARIS.GeospatialScene
{
    public class ARPathManager : MonoBehaviour
    {
        private readonly List<GameObject> _pathAnchorObjects = new();
        private LineRenderer _lineRenderer;
        private ArrowPoint _arrow;
        
        public Camera Camera;
        public GameObject PathPrefab;

        private void Start()
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.widthMultiplier = 0.2f;
            _lineRenderer.startColor = Color.blue;
            _lineRenderer.endColor = Color.cyan;
            _lineRenderer.positionCount = 0;
            _lineRenderer.numCapVertices = 6;
            _lineRenderer.numCornerVertices = 6;
            
            PathPrefab = Resources.Load("Polaris/RaisedArrow") as GameObject;
            
            var goList = new List<GameObject>();
            Camera.gameObject.GetChildGameObjects(goList);
            _arrow = goList.Find(go => go.name.Equals("Arrow")).GetComponent<ArrowPoint>();
        }

        // Update is called once per frame
        private void Update()
        {
            if (_pathAnchorObjects.Count < 2)
            {
                return;
            }

            var closest = GetClosestPathPoint();

            _lineRenderer.positionCount = _pathAnchorObjects.Count - closest;
            _lineRenderer.SetPositions(_pathAnchorObjects.Skip(closest).Select(anchor => anchor.transform.position).ToArray());

            for (var i = 0; i < closest; i++)
            {
                _pathAnchorObjects[i].gameObject.SetActive(false);
            }

            // Set arrows
            for (var i = closest; i < _pathAnchorObjects.Count - 1; i++)
            {
                var direction = _pathAnchorObjects[i + 1].transform.position -
                                _pathAnchorObjects[i].transform.position;
                var lookRot = Quaternion.LookRotation(direction, Vector3.up);
                
                _pathAnchorObjects[i].transform.rotation = Quaternion.Euler(lookRot.eulerAngles.x, lookRot.eulerAngles.y, lookRot.eulerAngles.z);
            }
        }

        public void ClearPath(List<GameObject> anchorObjects)
        {
            foreach (var anchor in _pathAnchorObjects)
            {
                anchorObjects.Remove(anchor);
            }
            _pathAnchorObjects.Clear();
            _lineRenderer.positionCount = 0;
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

            _lineRenderer.positionCount = PersistData.PathPoints.Count;
            
            _arrow.SetEnabled(true);
        }

        private ARGeospatialAnchor PlacePathGeospatialAnchor(
                                                IReadOnlyList<double> point,
                                                ICollection<GameObject> anchorObjects,
                                                ARAnchorManager anchorManager)
        {
            var promise =
                anchorManager.ResolveAnchorOnTerrainAsync(
                    point[0], point[1],
                    -6, Quaternion.Euler(90, 0, 90));

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
    }
}
