using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;

namespace POLARIS.GeospatialScene
{
    public class PanelZoom : MonoBehaviour, IPointerDownHandler
    {
        private GameObject _arCamera;
        private Transform _marker;
        private FaceCamera _faceCamera;

        public TextPanel Panel;

        private void Start()
        {
            _arCamera = GameObject.FindGameObjectWithTag("MainCamera");
            _faceCamera = transform.parent.GetComponent<FaceCamera>();
            _marker = gameObject.transform.parent.parent;
            AddPhysics2DRaycaster(_arCamera);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
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
            
            transform.parent.parent = _arCamera.transform;
            _faceCamera.Zoomed = true;
            Panel.VisitedPanel();
        }
        

        public void DisableZoom()
        {
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
