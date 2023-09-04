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

        public float RotationSpeed;
        // Start is called before the first frame update
        private void Start()
        {
            _destPoint = SwapData.DestinationPoint;
            _arCamera = GameObject.FindGameObjectWithTag("MainCamera");
            
            var offset = _arCamera.transform.forward * 8 + _arCamera.transform.up * -3;
            transform.position = offset;
        }

        // Update is called once per frame
        private void Update()
        {
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
            
            gameObject.SetActive(true);
        }
    }
}
