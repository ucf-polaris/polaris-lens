using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace POLARIS.GeospatialScene
{
    public class TextPanel : MonoBehaviour
    {
        public GameObject PanelPrefab;
        public GameObject LoadingPrefab;
        
        public bool Loaded;
        public bool Visited;

        private GeospatialAnchorContent _content;
        private ARGeospatialAnchor _anchor;
        private GameObject _currentPrefab;

        public void Instantiate(GeospatialAnchorContent content)
        {
            _content = content;
        }

        public ARGeospatialAnchor PlacePanelGeospatialAnchor(
            List<GameObject> anchorObjects, ARAnchorManager anchorManager)
        {
            _anchor = anchorManager.AddAnchor(
                _content.History.Latitude,
                _content.History.Longitude,
                _content.History.Altitude,
                _content.History.EunRotation);
            
            LoadingPrefab = Resources.Load("Polaris/stand") as GameObject;

            if (_anchor != null)
            {
                if (LoadingPrefab == null || PanelPrefab == null)
                {
                    Debug.LogError("Panel prefab is null!");
                }

                _currentPrefab = Instantiate(LoadingPrefab, _anchor.transform);
                anchorObjects.Add(_anchor.gameObject);

                print("Anchor Set!");
            }
            else
            {
                print("Failed to set an anchor!");
            }

            return _anchor;
        }

        public void LoadPanel()
        {
            Loaded = true;
            Destroy(_currentPrefab);
            
            PanelPrefab = Resources.Load("Polaris/PanelMarker") as GameObject;
            _currentPrefab = Instantiate(PanelPrefab, _anchor.transform);
            
            _currentPrefab.GetComponentInChildren<TextMeshPro>().SetText(_content.Text);
            _currentPrefab.GetComponentInChildren<PanelZoom>().Panel = this;
        }

        public void UnloadPanel()
        {
            Loaded = false;
            Destroy(_currentPrefab);
            
            _currentPrefab = Instantiate(LoadingPrefab, _anchor.transform);
        }

        public void VisitedPanel()
        {
            print("visited!");
            Visited = true;
            
            // Update API
        }
    }
}
