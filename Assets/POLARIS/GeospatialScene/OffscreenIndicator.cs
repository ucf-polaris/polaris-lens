using System;
using POLARIS.GeospatialScene;
using Unity.Mathematics;
using UnityEngine;

public class OffscreenIndicator : MonoBehaviour
{
    public GameObject IndicatorPrefab;
    public PanelManager PanelManager;
    public Camera Camera;

    public float Margin;
    public float RenderDist;
    public float SlerpFactor;
    
    private float _minX;
    private float _maxX;
    private float _minY;
    private float _maxY;

    private void Start()
    {
        _minX = Margin;
        _maxX = Screen.width - Margin;
        _minY = Margin;
        _maxY = Screen.height - Margin;
    }

    // Update is called once per frame
    private void Update()
    {
        var panels = PanelManager.GetPanels();

        foreach (var panel in panels)
        {
            if (!panel.Loaded) continue;
            
            var pos = panel.CurrentPrefab.transform.position;
            var cameraPos = Camera.transform.position;
            
            if (math.abs((pos - cameraPos).magnitude) > RenderDist) continue;
            
            var cameraForward = Camera.transform.forward;
            var screenPos = Camera.WorldToScreenPoint(pos);

            // If behind or out of FOV
            if (Vector3.Dot(pos - cameraPos, cameraForward) < 0
                || Vector3.Angle(pos - cameraPos, cameraForward) > Camera.fieldOfView * ((float)Screen.width / Screen.height))
            {
                var justCreated = false;
                if (panel.Indicator == null)
                {
                    panel.Indicator = Instantiate(IndicatorPrefab, transform);
                    justCreated = true;
                }
                
                // Reverse if behind
                if (screenPos.z < 0)
                {
                    screenPos.x = Screen.width - screenPos.x;
                    screenPos.y = Screen.height - screenPos.y;
                }
                screenPos.z = 0;
                
                // Slerp direction (Dont change too much at once)
                if (!justCreated)
                {
                    var oldScreenPos = panel.Indicator.transform.position;
                    screenPos = Vector3.Slerp(oldScreenPos, screenPos, SlerpFactor);
                }
                
                // Get pos on left or right edge
                screenPos.x = (screenPos.x < Screen.width / 2f) ? _minX : _maxX;
                screenPos.y = Mathf.Clamp(screenPos.y, _minY, _maxY);

                panel.Indicator.transform.position = screenPos;

                // Rotate away from center
                var center = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
                panel.Indicator.transform.up = center - panel.Indicator.transform.position;

                panel.Indicator.SetActive(true);
            }
            else if (panel.Indicator != null)
            {
                Destroy(panel.Indicator);
                panel.Indicator = null;
            }
        }
        
    }
}
