using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
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

        private GameObject _bottomPanel;
        private TextMeshProUGUI _bottomText;

        private string _lastPressed;
        private bool _bottomPanelShown;

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
            _bottomPanel = goList.Find(go => go.name.Equals("BottomPanel"));
            _bottomText = _bottomPanel.GetComponentInChildren<TextMeshProUGUI>();

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
            if (Visited) return;
            
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
            if (_bottomPanelShown && _lastPressed == "poi")
            {
                _bottomPanelShown = false;
            }
            else
            {
                _bottomPanelShown = true;
                _bottomText.SetText("Information points of interest wow");
            }
            _lastPressed = "poi";
            
            _bottomPanel.SetActive(_bottomPanelShown);
        }
        
        public void EventsButtonClicked()
        {
            if (_bottomPanelShown && _lastPressed == "events")
            {
                _bottomPanelShown = false;
            }
            else
            {
                _bottomPanelShown = true;
                _bottomText.SetText("But I must explain to you how all this mistaken idea of denouncing pleasure and praising pain was born and I will give you a complete account of the system, and expound the actual teachings of the great explorer of the truth, the master-builder of human happiness. No one rejects, dislikes, or avoids pleasure itself, because it is pleasure, but because those who do not know how to pursue pleasure rationally encounter consequences that are extre");
            }
            _lastPressed = "events";

            _bottomPanel.SetActive(_bottomPanelShown);
        }

        public void DisableEventsPanel()
        {
            _bottomPanel.SetActive(false);
        }
    }
}
