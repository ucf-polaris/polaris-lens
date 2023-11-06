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

        if (bestPanel == _lastBestPanel) return;
        
        if (bestPanel == null)
        {
            _lastBestPanel.GetComponentInChildren<PanelZoom>().DisableZoom();
        }
        else
        {
            bestPanel.GetComponentInChildren<PanelZoom>().EnableZoom();
        }

        _lastBestPanel = bestPanel;

    }

    private TextPanel GetBestPanel(List<TextPanel> panels)
    {
        TextPanel bestPanel = null;
        var bestDist = float.MaxValue;
        foreach (var panel in panels)
        {   
            // Within distance threshold
            var dist = Vector3.Distance(panel.CurrentPrefab.transform.position,
                                        Camera.transform.position);
            if (!(dist < MaxDist)) continue;
            
            // Within angle threshold
            var withinAngle =
                Vector3.Angle(Camera.transform.forward,
                              panel.CurrentPrefab.transform.position) < MaxAngle;
            if (!withinAngle) continue;
            
            // Now sort by dist
            if (dist < bestDist)
            {
                bestDist = dist;
                bestPanel = panel;
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
