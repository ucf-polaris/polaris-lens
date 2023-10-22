using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.XR.ARCoreExtensions;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
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
        private GameObject _bottomLayout;

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
            _bottomLayout = _bottomPanel.GetComponentInChildren<VerticalLayoutGroup>().gameObject;

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
                // _bottomText.SetText("Information points of interest wow");
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
                // _bottomText.SetText("But I must explain to you how all this mistaken idea of denouncing pleasure and praising pain was born and I will give you a complete account of the system, and expound the actual teachings of the great explorer of the truth, the master-builder of human happiness. No one rejects, dislikes, or avoids pleasure itself, because it is pleasure, but because those who do not know how to pursue pleasure rationally encounter consequences that are extre");
            }
            _lastPressed = "events";

            AddEvents();
            _bottomPanel.SetActive(_bottomPanelShown);
        }

        public void DisableEventsPanel()
        {
            _bottomPanel.SetActive(false);
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
            var events = Events.EventList.Where(e =>
                                                    Math.Abs(e.location.BuildingLat -
                                                             this.Content.History.Latitude) <
                                                    0.000001 &&
                                                    Math.Abs(e.location.BuildingLong -
                                                             this.Content.History.Longitude) <
                                                    0.000001);
            
            // APPEND HEADER somehow

            foreach (var e in events)
            {
                var textObj = Resources.Load<GameObject>("Polaris/AREventText");

                var text = GenerateEventText(e);
                textObj.GetComponent<TextMeshProUGUI>().SetText(text);
                 _ = SetImage(e.image, textObj.GetComponentInChildren<Image>());

                Instantiate(textObj, _bottomLayout.transform);
            }
        }

        private static string GenerateEventText(Event e)
        {
            var sb = new StringBuilder();

            sb.Append(e.name + "\n\n");
            sb.Append(e.listedLocation + "\n");
            sb.Append( e.dateTime + "-" + e.endsOn + "\n");
            sb.Append(e.host + "\n\n");
            sb.Append(e.description);

            return sb.ToString();
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
