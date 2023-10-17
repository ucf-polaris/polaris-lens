using System.Collections;
using POLARIS.GeospatialScene;
using UnityEngine;
using UnityEngine.EventSystems;

public class PoiButton : MonoBehaviour, IPointerDownHandler
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
        _panel.PoiButtonClicked();
        StartCoroutine(ChangeIcon());
    }
    
    private IEnumerator ChangeIcon ()
    {
        _spriteRenderer.color = new Color(83/256f, 54/256f, 93/256f);
        yield return new WaitForSeconds (0.5f);
        _spriteRenderer.color = new Color(165/256f, 107/256f, 185/256f);
    }
}
