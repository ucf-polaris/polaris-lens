using System.Collections;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using POLARIS.GeospatialScene;
using POLARIS.Managers;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DisplayPanel : MonoBehaviour
{
    public Camera Camera;
    
    public bool ManuallyZoomed;

    private const float MaxDist = 200f; // m
    private const float MaxAngle = 30f; // deg

    private PanelManager _panelManager;
    private TextPanel _lastBestPanel;
    private Vector3 _lastPosition;

    // Start is called before the first frame update
    private void Start()
    {
        _panelManager = GetComponent<PanelManager>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (ManuallyZoomed) return;
        
        var panels = _panelManager.GetPanels();
        var bestPanel = GetBestPanel(panels);
        
        print("Best panel " + (bestPanel ? bestPanel.Content.Location.BuildingName : "null"));

        if (bestPanel == _lastBestPanel) return;
        
        if (bestPanel == null)
        {
            print("last best " + _lastBestPanel);
            _lastBestPanel.CurrentPrefab.GetComponentInChildren<PanelZoom>().DisableZoom();
        }
        else if (_lastBestPanel == null)
        {
            print("bestest " + bestPanel);
            bestPanel.CurrentPrefab.GetComponentInChildren<PanelZoom>().EnableZoom();
        }

        _lastBestPanel = bestPanel;
    }

    private TextPanel GetBestPanel(List<TextPanel> panels)
    {
        var cameraPos = Camera.transform.position;
        
        TextPanel bestPanel = null;
        var bestDist = float.MaxValue;
        foreach (var panel in panels)
        {
            var panelPos = panel.transform.position;
            if (panel == _lastBestPanel && panel != null)
            {
                panelPos = _lastPosition;
            }
            
            // Within distance threshold
            var dist = Vector3.Distance(panelPos, cameraPos);
            if (!(dist < MaxDist)) continue;
            
            // Within angle threshold
            var withinAngle = Vector3.Angle(Camera.transform.forward, panelPos - cameraPos) < MaxAngle;
            if (!withinAngle) continue;

            // Now sort by dist
            if (dist < bestDist)
            {
                bestDist = dist;
                bestPanel = panel;
                _lastPosition = panelPos;
            }
        }

        return bestPanel;
    }

    private LocationData getBestLocation()
    {
        // Idea: Raycast to geospatial streetscape mesh then get closest location to hit

        return new LocationData();
    }
    
}
