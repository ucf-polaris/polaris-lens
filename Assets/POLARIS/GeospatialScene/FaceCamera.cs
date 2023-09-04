using System;
using UnityEngine;

namespace POLARIS.GeospatialScene
{
    public class FaceCamera : MonoBehaviour
    {
        private GameObject _arCamera;
        public float Speed;

        // Start is called before the first frame update
        private void Start()
        {
            _arCamera = GameObject.FindGameObjectWithTag("MainCamera");
            print("Hello out there + " + _arCamera.name);
        }

        // Update is called once per frame
        private void Update()
        {
            var distance = _arCamera.transform.position - transform.position;
            var targetRotation = Quaternion.LookRotation(distance, Vector3.down);
            var eulerRot = targetRotation.eulerAngles;
            
            //Clamp x-rotation to 45 degrees each direction
            // targetRotation = eulerRot.x switch
            // {
            //     > 90 + 45 => Quaternion.Euler(90 + 45, eulerRot.y, eulerRot.z),
            //     < 90 - 45 => Quaternion.Euler(90 - 45, eulerRot.y, eulerRot.z),
            //     _ => targetRotation
            // };

            var outerPosition = transform.parent.position + (_arCamera.transform.position - transform.parent.position).normalized * 0.3f;
            
            transform.SetPositionAndRotation(new Vector3(outerPosition.x, outerPosition.y + 0.3f, outerPosition.z),
                                Quaternion.Slerp(transform.rotation, targetRotation, Speed * Time.deltaTime));
        }

        // private static Vector3 getClosestPointOnCircle(Vector3 point, Vector3 circleCenter, float radius)
        // {
        //     var denominator = (float)Math.Sqrt((point.x - circleCenter.x) * (point.x - circleCenter.x) 
        //                                        + (point.y - circleCenter.y) * (point.y - circleCenter.y));
        //
        //     var ret = new Vector3(circleCenter.x + radius * (point.x - circleCenter.x) / denominator,
        //                        circleCenter.y + radius * (point.y - circleCenter.y) / denominator, 
        //                        circleCenter.z + 0.2f);
        //     print(point + " wawa " + circleCenter + " wawa " + ret);
        //     return ret;
        // }
    }
}
