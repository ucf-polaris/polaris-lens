using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace POLARIS.GeospatialScene
{
    public class PanelZoom : MonoBehaviour, IPointerDownHandler
    {
        private GameObject _arCamera;
        private Transform _marker;
        private FaceCamera _faceCamera;

        private Button _poiButton;
        private Button _eventsButton;
        private Button _favButton;

        public TextPanel Panel;

        private void Start()
        {
            _arCamera = GameObject.FindGameObjectWithTag("MainCamera");
            _faceCamera = transform.parent.GetComponent<FaceCamera>();
            _marker = gameObject.transform.parent.parent;
            AddPhysics2DRaycaster(_arCamera);

            _poiButton = gameObject.GetNamedChild("PoiButton").GetComponent<Button>();
            _eventsButton = gameObject.GetNamedChild("EventsButton").GetComponent<Button>();
            _favButton = gameObject.GetNamedChild("FavButton").GetComponent<Button>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // TODO: Check if button pressed (dont unzoom on press)
            
            if (_faceCamera.Zoomed)
            {
                DisableZoom();
            }
            else
            {
                EnableZoom();
            }
        }

        private void EnableZoom()
        {
            // Disable other zooms
            try
            {
                _arCamera.GetNamedChild("Panel").GetComponent<PanelZoom>().DisableZoom();
            }
            catch
            {
                // ignore - nothing is zoomed
            }
            
            // enable onclicks
            _poiButton.onClick.AddListener(Panel.PoiButtonClicked);
            _eventsButton.onClick.AddListener(Panel.EventsButtonClicked);
            _favButton.onClick.AddListener(Panel.FavoritedClicked);

            transform.parent.parent = _arCamera.transform;
            _faceCamera.Zoomed = true;
            Panel.VisitedPanel();
        }
        

        public void DisableZoom()
        {
            // disable onclicks
            _poiButton.onClick.RemoveAllListeners();
            _eventsButton.onClick.RemoveAllListeners();
            _favButton.onClick.RemoveAllListeners();
            
            transform.parent.parent = _marker.transform;
            _faceCamera.Zoomed = false;
        }

        private static void AddPhysics2DRaycaster(GameObject camera)
        {
            var physicsRaycaster = FindObjectOfType<PhysicsRaycaster>();
            if (physicsRaycaster == null)
            {
                camera.AddComponent<PhysicsRaycaster>();
            }
        }
    }
}
