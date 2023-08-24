using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using Google.XR.ARCoreExtensions.Samples.Geospatial;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace POLARIS.GeospatialScene
{
    public class TextPanel : MonoBehaviour
    {
        public GameObject PanelPrefab;

        private string _content;
        private GeospatialAnchorHistory _info;

        public void Instantiate(string content, GeospatialAnchorHistory anchorInfo)
        {
            this._content = content;
            this._info = anchorInfo;
            // PanelPrefab = Resources.Load("Polaris/Panel") as GameObject;
        }

        public ARGeospatialAnchor PlacePanelGeospatialAnchor(
            List<GameObject> anchorObjects, ARAnchorManager anchorManager)
        {
            var anchor = anchorManager.AddAnchor(
                _info.Latitude, _info.Longitude, _info.Altitude, _info.EunRotation);
            
            PanelPrefab = Resources.Load("Polaris/Panel") as GameObject;

            if (anchor != null)
            {
                if (PanelPrefab == null)
                {
                    Debug.LogError("Panel prefab is null!");
                }

                var anchorGo = Instantiate(PanelPrefab, anchor.transform);
                anchorGo.GetComponentInChildren<TextMeshPro>().SetText(_content);

                anchorObjects.Add(anchor.gameObject);
                
                Debug.LogWarning(anchorObjects[0]);

                print("Anchor Set!");
            }
            else
            {
                print("Failed to set an anchor!");
            }

            return anchor;
        }
    }
}
