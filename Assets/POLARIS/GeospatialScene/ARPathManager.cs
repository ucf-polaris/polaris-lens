using System.Collections;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using POLARIS.MainScene;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace POLARIS.GeospatialScene
{
    public class ARPathManager : MonoBehaviour
    {
        private readonly List<GameObject> _pathAnchorObjects = new();
        private LineRenderer _lineRenderer;
        public GameObject PathPrefab;

        private void Start()
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.widthMultiplier = 0.2f;
            _lineRenderer.startColor = Color.blue;
            _lineRenderer.endColor = Color.cyan;
            _lineRenderer.positionCount = 0;
            _lineRenderer.numCapVertices = 12;
            _lineRenderer.numCornerVertices = 6;
            PathPrefab = Resources.Load("Polaris/simplearrow") as GameObject;
        }

        // Update is called once per frame
        private void Update()
        {
            if (_pathAnchorObjects.Count < 2)
            {
                return;
            }

            _lineRenderer.SetPositions(_pathAnchorObjects.ConvertAll(anchor => anchor.transform.position).ToArray());
            
            for (var i = 0; i < _pathAnchorObjects.Count - 1; i++)
            {
                var direction = _pathAnchorObjects[i + 1].transform.position -
                                _pathAnchorObjects[i].transform.position;
                var lookRot = Quaternion.LookRotation(direction, Vector3.forward);
                
                _pathAnchorObjects[i].transform.rotation = Quaternion.Euler(0, lookRot.eulerAngles.y + 90, -90);
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
        }

        public void ClearPathAnchors()
        {
            _pathAnchorObjects.Clear();
            _lineRenderer.positionCount = 0;
        }
        
    
        public void LoadPathAnchors(List<GameObject> anchorObjects, ARAnchorManager anchorManager)
        {
            // remove old anchors
            ClearPath(anchorObjects);

            // mock fetch for now
            // var pathPoints = new List<double[]>
            // {
            //     new[]{28.614402, -81.195860},
            //     new[]{28.614469, -81.195702},
            //     new[]{28.614369, -81.195760}
            // };
            
            foreach (var point in PersistData.PathPoints)
            {
                PlacePathGeospatialAnchor(point, anchorObjects, anchorManager);
            }

            _lineRenderer.positionCount = PersistData.PathPoints.Count;
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
    }
}
