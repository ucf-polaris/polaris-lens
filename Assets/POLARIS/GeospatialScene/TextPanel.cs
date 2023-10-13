using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.ARFoundation;

namespace POLARIS.GeospatialScene
{
    public class TextPanel : MonoBehaviour
    {
        public GameObject PanelPrefab;
        public GameObject LoadingPrefab;
        public GameObject CurrentPrefab;

        public GeospatialAnchorContent Content;

        public bool Loaded;
        public bool Visited;
        public bool Favorited;

        private ARGeospatialAnchor _anchor;

        private GameObject _eventPanel;

        private bool _eventsShown;

        public void Instantiate(GeospatialAnchorContent content)
        {
            Content = content;
            PanelPrefab = Resources.Load("Polaris/PanelParent") as GameObject;
            LoadingPrefab = Resources.Load("Polaris/stand") as GameObject;
        }

        public ARGeospatialAnchor PlacePanelGeospatialAnchor(
            List<GameObject> anchorObjects, ARAnchorManager anchorManager)
        {
            _anchor = anchorManager.AddAnchor(
                Content.History.Latitude,
                Content.History.Longitude,
                Content.History.Altitude,
                Content.History.EunRotation);

            if (_anchor != null)
            {
                if (LoadingPrefab == null || PanelPrefab == null)
                {
                    Debug.LogError("Panel prefab is null!");
                }

                CurrentPrefab = Instantiate(LoadingPrefab, _anchor.transform);
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
            Destroy(CurrentPrefab);
            
            CurrentPrefab = Instantiate(PanelPrefab, _anchor.transform);
            
            CurrentPrefab.GetComponentInChildren<TextMeshPro>().SetText(Content.Text);
            CurrentPrefab.GetComponentInChildren<PanelZoom>().Panel = this;
            
            var goList = new List<GameObject>();
            CurrentPrefab.GetChildGameObjects(goList);
            _eventPanel = goList.Find(go => go.name.Equals("EventPanel"));

            // Check for favorited / visited
        }

        public void UnloadPanel()
        {
            Loaded = false;
            Destroy(CurrentPrefab);
            
            CurrentPrefab = Instantiate(LoadingPrefab, _anchor.transform);
        }

        public void VisitedPanel()
        {
            print("visited!");
            Visited = true;
            
            // Update API
        }

        public void FavoritedClicked()
        {
            Favorited = !Favorited;
            print("zz favorited? " + Favorited);
            
            // Update API
        }

        public void PoiButtonClicked()
        {
            print("zz poi clicked!");
        }
        
        public void EventsButtonClicked()
        {
            print("zz events clicked!");
            _eventsShown = !_eventsShown;

            _eventPanel.SetActive(_eventsShown);
        }

        public void DisableEventsPanel()
        {
            _eventPanel.SetActive(false);
        }
    }
}
