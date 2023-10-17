using System.Collections;
using POLARIS.GeospatialScene;
using UnityEngine;
using UnityEngine.EventSystems;

public class EventsButton : MonoBehaviour, IPointerDownHandler
{
    private PanelZoom _panelZoom;
    private TextPanel _panel;
    private SpriteRenderer _spriteRenderer;
    
    // Start is called before the first frame update
    private void Start()
    {
        _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        _panelZoom = transform.parent.GetComponent<PanelZoom>();
        _panel = _panelZoom.Panel;
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        _panelZoom.TouchedPanel = true;
        _panel.EventsButtonClicked();
        StartCoroutine(ChangeIcon());
    }
    
    private IEnumerator ChangeIcon ()
    {
        _spriteRenderer.color = new Color(91/256f, 60/256f, 24/256f);
        yield return new WaitForSeconds (0.5f);
        _spriteRenderer.color = new Color(182/256f, 119/256f, 48/256f);
    }
}