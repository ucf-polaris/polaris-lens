using POLARIS.GeospatialScene;
using UnityEngine;
using UnityEngine.EventSystems;

public class FavButton : MonoBehaviour, IPointerDownHandler
{
    private PanelZoom _panelZoom;
    private TextPanel _panel;

    public Sprite FavoritedSprite;
    public Sprite UnfavoritedSprite;
    
    // Start is called before the first frame update
    private void Start()
    {
        _panelZoom = transform.parent.GetComponent<PanelZoom>();
        _panel = _panelZoom.Panel;
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        _panelZoom.TouchedPanel = true;
        _panel.FavoritedClicked();
    }

    public void UpdateSprite()
    {
        gameObject.GetComponent<SpriteRenderer>().sprite = _panel.Favorited ? FavoritedSprite : UnfavoritedSprite;
    }
}
