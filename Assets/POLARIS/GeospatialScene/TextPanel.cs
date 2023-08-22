using System.Collections.Generic;
using Google.XR.ARCoreExtensions.Samples.Geospatial;
using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro;

namespace POLARIS
{
    public class TextPanel : MonoBehaviour
    {
        public GameObject PanelPrefab;

        private readonly string _content;
        private GeospatialAnchorHistory _info;

        public TextPanel(string content, GeospatialAnchorHistory anchorInfo)
        {
            this._content = content;
            this._info = anchorInfo;
        }

        public ARGeospatialAnchor PlacePanelGeospatialAnchor(
            List<GameObject> anchorObjects, ARAnchorManager anchorManager)
        {
            var anchor = anchorManager.AddAnchor(
                _info.Latitude, _info.Longitude, _info.Altitude, _info.EunRotation);
            
            if (anchor != null)
            {
                var anchorGo = Instantiate(PanelPrefab, anchor.transform);
                anchorGo.GetComponentInChildren<TextMeshPro>().SetText(_content);

                anchorObjects.Add(anchor.gameObject);

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
