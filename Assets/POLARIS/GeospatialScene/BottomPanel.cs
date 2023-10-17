using POLARIS.GeospatialScene;
using UnityEngine;
using UnityEngine.EventSystems;

public class BottomPanel : MonoBehaviour, IPointerDownHandler
{
    private PanelZoom _panelZoom;
    
    // Start is called before the first frame update
    private void Start()
    {
        _panelZoom = transform.parent.GetComponentInChildren<PanelZoom>();
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        _panelZoom.TouchedPanel = true;
    }
}
