using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace POLARIS.GeospatialScene
{
    public class PanelZoom : MonoBehaviour, IPointerDownHandler
    {
        public TextPanel Panel;
        public bool TouchedPanel;

        private GameObject _arCamera;
        private Transform _grandparent;
        private FaceCamera _faceCamera;

        private BoxCollider _poiButton;
        private BoxCollider _eventsButton;
        private BoxCollider _favButton;


        private void Start()
        {
            _arCamera = GameObject.FindGameObjectWithTag("MainCamera");
            _faceCamera = transform.parent.GetComponent<FaceCamera>();
            _grandparent = gameObject.transform.parent.parent;
            AddPhysics2DRaycaster(_arCamera);

            _poiButton = gameObject.GetNamedChild("PoiButton").GetComponent<BoxCollider>();
            _eventsButton = gameObject.GetNamedChild("EventsButton").GetComponent<BoxCollider>();
            _favButton = gameObject.GetNamedChild("FavButton").GetComponent<BoxCollider>();
        }

        private void LateUpdate()
        {
            if (!_faceCamera.Zoomed) return;
            if (Input.touchCount == 0)
            {
                TouchedPanel = false;
                return;
            };
            if (TouchedPanel) return;
            
            DisableZoom();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            TouchedPanel = true;

            if (_faceCamera.Zoomed) return;

            EnableZoom();
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
            
            // enable colliders
            _poiButton.enabled = true;
            _eventsButton.enabled = true;
            _favButton.enabled = true;

            transform.parent.parent = _arCamera.transform;
            _faceCamera.Zoomed = true;
            Panel.VisitedPanel();
        }
        

        public void DisableZoom()
        {
            // disable colliders
            _poiButton.enabled = false;
            _eventsButton.enabled = false;
            _favButton.enabled = false;
            
            transform.parent.parent = _grandparent.transform;
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
