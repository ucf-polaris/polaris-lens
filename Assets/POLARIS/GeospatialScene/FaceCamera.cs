using UnityEngine;

namespace POLARIS.GeospatialScene
{
    public class FaceCamera : MonoBehaviour
    {
        private GameObject _arCamera;
        public float Speed = 0.5f;

        // Start is called before the first frame update
        private void Start()
        {
            _arCamera = GameObject.FindGameObjectWithTag("MainCamera");
            print("Hello out there + " + _arCamera.name);
            gameObject.transform.Rotate(Vector3.right, -90f);
            gameObject.transform.Rotate(Vector3.forward, 180f);
        }

        // Update is called once per frame
        // private float tick = 0;
        private void Update()
        {
            var targetRotation = Quaternion.LookRotation(_arCamera.transform.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Speed * Time.deltaTime);
            
            // tick += 1;
            // tick %= 91;
        }
    }
}
