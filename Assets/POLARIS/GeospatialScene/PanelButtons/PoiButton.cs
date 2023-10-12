using POLARIS.GeospatialScene;
using UnityEngine;
using UnityEngine.EventSystems;

public class PoiButton : MonoBehaviour, IPointerDownHandler
{
    private PanelZoom _panelZoom;
    private TextPanel _panel;
    
    // Start is called before the first frame update
    private void Start()
    {
        _panelZoom = transform.parent.GetComponent<PanelZoom>();
        _panel = _panelZoom.Panel;
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        _panelZoom.TouchedPanel = true;
        _panel.PoiButtonClicked();
    }
}
