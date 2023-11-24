using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.XR.ARCoreExtensions;
using Google.XR.ARCoreExtensions.Samples.Geospatial;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
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
        
        public GameObject Indicator;

        private TextPanel[] _alternatePanels;
        public TextPanel MainPanel;
        
        private ARGeospatialAnchor _anchor;

        private GameObject _bottomPanel;
        private GameObject _bottomLayout;
        private GameObject _visitedIndicator;

        private string _lastPressed;
        private bool _bottomPanelShown;
        private bool _eventsLoaded;

        private EventManager _eventManager;
        private UserManager _userManager;
        private DisplayPanel _display;

        private void Start()
        {
            _eventManager = EventManager.getInstance();
            _userManager = UserManager.getInstance();
        }

        public void Instantiate(GeospatialAnchorContent content, DisplayPanel display, TextPanel[] alternates)
        {
            Content = content;
            _display = display;
            _alternatePanels = alternates;
            PanelPrefab = Resources.Load("Polaris/PanelParent") as GameObject;
            LoadingPrefab = Resources.Load("Polaris/Capsule") as GameObject;
        }
        
        public ARGeospatialAnchor PlacePanelGeospatialAnchor(
            List<GameObject> anchorObjects, ARAnchorManager anchorManager)
        {
            print("PLACED at " + Content.History.Latitude + ", " + Content.History.Longitude);
            
            if (Content.History.AnchorType == AnchorType.Terrain)
            {
                return PlacePanelGeospatialTerrainAnchor(anchorObjects, anchorManager);
            }
            
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
        
        private ARGeospatialAnchor PlacePanelGeospatialTerrainAnchor(
            ICollection<GameObject> anchorObjects,
            ARAnchorManager anchorManager)
        {
            if (LoadingPrefab == null || PanelPrefab == null)
            {
                Debug.LogError("Panel prefab is null!");
            }
            
            var promise =
                anchorManager.ResolveAnchorOnTerrainAsync(
                    Content.History.Latitude,
                    Content.History.Longitude,
                    Content.History.Altitude,
                    Content.History.EunRotation);

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
                Debug.LogError("Failed to set a terrain anchor!");
                yield break;
            }

            var resultGo = result.Anchor.gameObject;
            CurrentPrefab = Instantiate(LoadingPrefab,
                                       resultGo.transform);
            anchorObjects.Add(resultGo);

            _anchor = result.Anchor;
            print("Anchor Set!");
        }

        public void LoadPanel()
        {
            Loaded = true;
            Destroy(CurrentPrefab);
            
            CurrentPrefab = Instantiate(PanelPrefab, _anchor.transform);
            
            CurrentPrefab.GetComponentInChildren<TextMeshPro>().SetText(Content.Text);
            var panelZoom = CurrentPrefab.GetComponentInChildren<PanelZoom>();
            panelZoom.Panel = this;
            panelZoom.Display = _display;
            
            var goList = new List<GameObject>();
            CurrentPrefab.GetChildGameObjects(goList);
            _bottomPanel = goList.Find(go => go.name.Equals("BottomPanel"));
            _bottomLayout = _bottomPanel.GetComponentInChildren<VerticalLayoutGroup>().gameObject;

            // TODO: FIX NULL REF ERROR

            // Check for favorited / visited
            if (!_userManager.isVisited(Content.Location))
            {
                _visitedIndicator = goList.Find(go => go.name.Equals("VisitedPin"));
                _visitedIndicator.SetActive(true);
            }

            if (_userManager.isFavorite(Content.Location))
            {
                GetComponentInChildren<FavButton>().UpdateSprite(true);
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
            Visited = _userManager.isVisited(Content.Location);
            if (Visited) return;
            
            // Alternate
            if (_alternatePanels == null)
            {
                MainPanel.VisitedPanel();
            }
            else
            // Main
            {
                _userManager.UpdateVisited(true, Content.Location);
                _visitedIndicator.SetActive(false);
                foreach (var alt in _alternatePanels)
                {
                    alt._visitedIndicator.SetActive(false);
                }
            }
        }

        public void FavoritedClicked()
        {
            // Alternate
            if (_alternatePanels == null)
            {
                MainPanel.FavoritedClicked();
            }
            // Main
            else
            {
                var favorited = !_userManager.isFavorite(Content.Location);
                _userManager.UpdateFavorites(favorited, Content.Location);
                GetComponentInChildren<FavButton>().UpdateSprite(favorited);
                foreach (var alt in _alternatePanels)
                {
                    alt.GetComponentInChildren<FavButton>().UpdateSprite(favorited);
                }
            }
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

        public static string GenerateLocationText(LocationData location)
        {
            location.BuildingDesc ??= "This is definitely one the buildings ever at UCF.";

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
            if (_eventManager.dataList is null) return;

            var events = _eventManager.dataList.Where(e =>
                                                          Content.Location.BuildingEvents.Any(
                                                              s => s.Equals(e.EventID)));

            // TODO: APPEND HEADER somehow
            
            foreach (var e in events)
            {
                var textObj = Resources.Load<GameObject>("Polaris/AREventText");

                var text = GenerateEventText(e);
                textObj.GetComponent<TextMeshProUGUI>().SetText(text);
                
                // Set image - need better design
                // try
                // {
                //     var eventSprite = Sprite.Create(e.rawImage, new Rect(0, 0, e.rawImage.width, e.rawImage.height), new Vector2(0.5f, 0.5f));
                //     var image = textObj.GetComponentInChildren<Image>();
                //     image.sprite = eventSprite;
                // }
                // catch (Exception exception)
                // {
                //     Console.WriteLine("Could not get sprite " + exception);
                // }
                
                Instantiate(textObj, _bottomLayout.transform);
            }

            _eventsLoaded = true;
        }

        // TODO: Make this look nicer
        private static string GenerateEventText(EventData e)
        {
            var sb = new StringBuilder();

            sb.Append("<style=\"EvTitle\">" + e.Name + "</style>\n");
            sb.Append("<style=\"EvHost\">" + e.Host + "</style>\n");
            sb.Append("<style=\"EvTime\">" + GenerateTime(e.DateTime) + "</style>\n");
            sb.Append("<style=\"EvLocation\">" + e.ListedLocation + "</style>\n");
            sb.Append("<style=\"EvDescription\">" + HtmlParser.RichParse(e.Description) + "</style>");

            return sb.ToString();
        }

        private static string GenerateTime(DateTime dt)
        {
            return $"{dt:h:mm tt - dddd, MMMM dd}";
        }
        
        // private static IEnumerator SetImage(EventData e, Graphic img)
        // {   
        //     
        //     var request = UnityWebRequestTexture.GetTexture(e.Image);
        //     yield return request.SendWebRequest();
        //     if (request.result != UnityWebRequest.Result.Success)
        //     {
        //         Debug.Log(request.error);
        //     }
        //     else
        //     {
        //         img.material.mainTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;
        //         Debug.Log("maybe success");
        //     }
        // }
    }
}
