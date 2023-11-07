using POLARIS.MainScene;
using UnityEngine;

namespace POLARIS.GeospatialScene
{
    public class ArrowPoint : MonoBehaviour
    {
        private GameObject _arCamera;
        private bool _enabled;

        public float RotationSpeed;
        // Start is called before the first frame update
        private void Start()
        {
            _arCamera = GameObject.FindGameObjectWithTag("MainCamera");

            var offset = _arCamera.transform.forward * 8 + _arCamera.transform.up * -3;
            transform.position = offset;
        }

        public void SetEnabled(bool enable)
        {
            _enabled = enable;
            gameObject.SetActive(_enabled && PersistData.StopLocations.Count > 0);
        }

        // Update is called once per frame
        private void Update()
        {
            if (!_enabled) return;

            var destPoint = PersistData.DestPoint;
            if (destPoint.Equals(Vector3.zero))
            {
                gameObject.SetActive(false);
                _enabled = false;
                return;
            };
            
            var camPos = _arCamera.transform.position;
            var xDiff = destPoint.x - camPos.x;
            var yDiff = destPoint.y - camPos.y;
            var direction = new Vector3(yDiff, 0, xDiff).normalized;

            var lookRot = Quaternion.LookRotation(direction, Vector3.forward);
            var rotFinal = Quaternion.Euler(90, lookRot.eulerAngles.y, lookRot.eulerAngles.z);

            transform.rotation = Quaternion.Slerp(transform.rotation, rotFinal, RotationSpeed * Time.deltaTime);
        }
    }
}
