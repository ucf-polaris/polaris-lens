using Esri.GameEngine.Geometry;
using POLARIS.MainScene;
using UnityEngine;
using UnityEngine.Serialization;

namespace POLARIS.GeospatialScene
{
    public class ArrowPoint : MonoBehaviour
    {
        private ArcGISPoint _destPoint;
        private GameObject _arCamera;
        private bool _enabled;

        public float RotationSpeed;
        // Start is called before the first frame update
        private void Start()
        {
            _arCamera = GameObject.FindGameObjectWithTag("MainCamera");
            _destPoint = PersistData.DestinationPoint;
            
            var offset = _arCamera.transform.forward * 8 + _arCamera.transform.up * -3;
            transform.position = offset;
        }

        public void SetEnabled(bool enable)
        {
            _enabled = enable;
            if (_enabled && _destPoint != null)
            {                
                _destPoint = PersistData.DestinationPoint;
            }
            gameObject.SetActive(_enabled);
        }

        // Update is called once per frame
        private void Update()
        {
            if (!enabled || _destPoint == null) return;
            
            if (!_destPoint.IsValid)
            {
                gameObject.SetActive(false);
                return;
            };
            
            var camPos = _arCamera.transform.position;
            var xDiff = (float)_destPoint.X - camPos.x;
            var yDiff = (float)_destPoint.Y - camPos.y;
            var direction = new Vector3(yDiff, 0, xDiff).normalized;

            var lookRot = Quaternion.LookRotation(direction, Vector3.forward);
            var rotFinal = Quaternion.Euler(90, lookRot.eulerAngles.y, lookRot.eulerAngles.z);

            transform.rotation = Quaternion.Slerp(transform.rotation, rotFinal, RotationSpeed * Time.deltaTime);
        }
    }
}
