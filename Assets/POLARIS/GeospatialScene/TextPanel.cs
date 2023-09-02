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
        
        private GeospatialAnchorContent _content;

        public void Instantiate(GeospatialAnchorContent content)
        {
            this._content = content;
        }

        public ARGeospatialAnchor PlacePanelGeospatialAnchor(
            List<GameObject> anchorObjects, ARAnchorManager anchorManager)
        {
            var anchor = anchorManager.AddAnchor(
                _content.History.Latitude,
                _content.History.Longitude,
                _content.History.Altitude,
                _content.History.EunRotation);
            
            PanelPrefab = Resources.Load("Polaris/PanelParent") as GameObject;

            if (anchor != null)
            {
                if (PanelPrefab == null)
                {
                    Debug.LogError("Panel prefab is null!");
                }

                var anchorGo = Instantiate(PanelPrefab, anchor.transform);
                anchorGo.GetComponentInChildren<TextMeshPro>().SetText(_content.Text);

                anchorObjects.Add(anchor.gameObject);

                print("Anchor Set!");
                
                Debug.LogWarning(_content.Text);
            }
            else
            {
                print("Failed to set an anchor!");
            }

            return anchor;
        }
    }
}
