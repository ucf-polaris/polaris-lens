using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
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
            _lineRenderer.positionCount = 0;
            PathPrefab = Resources.Load("Polaris/Capsule") as GameObject;
        }

        // Update is called once per frame
        private void Update()
        {
            if (_pathAnchorObjects.Count < 2)
            {
                return;
            }

            _lineRenderer.SetPositions(_pathAnchorObjects.ConvertAll(anchor => anchor.transform.position).ToArray());
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
            var pathPoints = new List<double[]>
            {
                new[]{28.614402, -81.195860},
                new[]{28.614469, -81.195702},
                new[]{28.614369, -81.195760}
            };
            
            foreach (var point in pathPoints)
            {
                PlacePathGeospatialAnchor(point, anchorObjects, anchorManager);
            }

            _lineRenderer.positionCount = _pathAnchorObjects.Count;
        }
        
        private void PlacePathGeospatialAnchor(IReadOnlyList<double> point,
            ICollection<GameObject> anchorObjects, ARAnchorManager anchorManager)
        {
            var anchor = anchorManager.AddAnchor(
                point[0],
                point[1],
                -6,
                Quaternion.identity);

            if (anchor != null)
            {
                if (PathPrefab == null)
                {
                    Debug.LogError("Panel prefab is null!");
                    return;
                }

                Instantiate(PathPrefab, anchor.transform);
                anchorObjects.Add(anchor.gameObject);
                _pathAnchorObjects.Add(anchor.gameObject);


                print("Path anchor set!");
            }
            else
            {
                print("Failed to set an anchor!");
            }
        }
    }
}
