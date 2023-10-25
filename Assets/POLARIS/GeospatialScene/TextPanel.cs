using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.XR.ARCoreExtensions;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using POLARIS.Managers;

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
        private GameObject _bottomLayout;
        private GameObject _visitedIndicator;

        private string _lastPressed;
        private bool _bottomPanelShown;
        private bool _eventsLoaded;

        private EventManager eventManager;

        private void Start()
        {
            eventManager = EventManager.getInstance();
        }

        public void Instantiate(GeospatialAnchorContent content)
        {
            Content = content;
            PanelPrefab = Resources.Load("Polaris/PanelParent") as GameObject;
            LoadingPrefab = Resources.Load("Polaris/Capsule") as GameObject;
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
            _bottomLayout = _bottomPanel.GetComponentInChildren<VerticalLayoutGroup>().gameObject;
            
            // Check for favorited / visited
            if (Favorited)
            {
                GetComponentInChildren<FavButton>().UpdateSprite();
            }

            if (!Visited)
            {
                _visitedIndicator =
                    Instantiate(Resources.Load("Polaris/simplearrow") as GameObject, CurrentPrefab.transform);
                _visitedIndicator.transform.localPosition = Vector3.up;
            }
        }

        public void UnloadPanel()
        {
            Loaded = false;
            Destroy(CurrentPrefab);
            
            CurrentPrefab = Instantiate(LoadingPrefab, _anchor.transform);
        }

        public void VisitedPanel()
        {
            if (Visited) return;
            
            Visited = true;
            Destroy(_visitedIndicator);
            // Update API
        }

        public void FavoritedClicked()
        {
            Favorited = !Favorited;
            print("zz favorited? " + Favorited);
            GetComponentInChildren<FavButton>().UpdateSprite();
            
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
            }
            _lastPressed = "events";

            if (!_eventsLoaded)
            {
                AddEvents();
            }
            
            _bottomPanel.SetActive(_bottomPanelShown);
        }

        public void DisableEventsPanel()
        {
            _bottomPanel.SetActive(false);

            var goList = new List<GameObject>();
            _bottomLayout.GetChildGameObjects(goList);
            foreach (var go in goList)
            {
                Destroy(go);
            }

            _eventsLoaded = false;
        }

        public static string GenerateLocationText(Building location)
        {
            var sb = new StringBuilder(location.BuildingDesc.Length);

            sb.Append("<style=Title>");
            sb.Append(location.BuildingName);
            sb.Append("</style>\n\n");

            sb.Append("<style=Description>");
            sb.Append(location.BuildingDesc);
            sb.Append("</style>");

            return sb.ToString();
        }

        private void AddEvents()
        {
            // Mock using student union coords 28.601927704512025, -81.20044219923692
            if (eventManager is null || eventManager.dataList is null) return;
            
            var events = eventManager.dataList.Where(e =>
                                                    Math.Abs(e.Location.BuildingLat -
                                                             28.601927704512025) < // this.Content.History.Latitude
                                                    0.000001 &&
                                                    Math.Abs(e.Location.BuildingLong -
                                                             -81.20044219923692) < // this.Content.History.Longitude
                                                    0.000001);
            
            // TODO: APPEND HEADER somehow

            foreach (var e in events)
            {
                var textObj = Resources.Load<GameObject>("Polaris/AREventText");

                var text = GenerateEventText(e);
                textObj.GetComponent<TextMeshProUGUI>().SetText(text);
                StartCoroutine(SetImage(e.Image, textObj.GetComponentInChildren<Image>()));

                Instantiate(textObj, _bottomLayout.transform);
            }

            _eventsLoaded = true;
        }

        private static string GenerateEventText(EventData e)
        {
            var sb = new StringBuilder();

            sb.Append(e.Name + "\n\n");
            sb.Append("<indent=45%><line-height=120%>" + e.ListedLocation + "\n");
            sb.Append("<line-height=120%>" + GenerateTime(e.DateTime) + "\n");
            sb.Append("<line-height=120%>" + e.Host + "\n\n");
            sb.Append("<indent=0%>" + HtmlParser.RichParse(e.Description));

            return sb.ToString();
        }

        private static string GenerateTime(DateTime dt)
        {
            return $"{dt:h:mm tt - dddd, MMMM dd}";
        }
        
        private static IEnumerator SetImage(string url, Graphic img)
        {   
            var request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
            }
            else
            {
                img.material.mainTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            }
        }
    }
}
