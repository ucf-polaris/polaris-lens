using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace POLARIS.GeospatialScene
{
    public class FaceCamera : MonoBehaviour
    {
        public float Speed;
        public bool Zoomed { get; set; }
        
        private GameObject _arCamera;
        private GameObject _bottomPanel;

        // Start is called before the first frame update
        private void Start()
        {
            _arCamera = GameObject.FindGameObjectWithTag("MainCamera");
            
            var goList = new List<GameObject>();
            gameObject.GetChildGameObjects(goList);
            _bottomPanel = goList.Find(go => go.name.Equals("BottomPanel"));
        }

        // Update is called once per frame
        private void Update()
        {
            var objTransform = transform;
            if (Zoomed)
            {
                var zoomPos = Vector3.forward * 3.3f;
                if (_bottomPanel.activeSelf)
                {
                    zoomPos = (Vector3.forward * 3.3f) + (Vector3.up * 1f);
                }
                
                objTransform.SetLocalPositionAndRotation(
                    Vector3.Slerp(objTransform.localPosition, zoomPos, Speed * 4 * Time.deltaTime),
                    Quaternion.Slerp(objTransform.localRotation, Quaternion.Euler(0, 180, 0), Speed * 4 * Time.deltaTime));
                return;
            }
            
            var distance = _arCamera.transform.position - objTransform.position;
            var targetRotation = Quaternion.LookRotation(distance, Vector3.up);
            var eulerRot = targetRotation.eulerAngles;

            //Clamp x-rotation to 60 degrees each direction
            targetRotation = eulerRot.x switch
            {
                > 60 and < 180  => Quaternion.Euler(60, eulerRot.y, eulerRot.z),
                > 180 and < 300 => Quaternion.Euler(300, eulerRot.y, eulerRot.z),
                _               => targetRotation
            };

            var outerPosition = objTransform.parent.position +
                                (_arCamera.transform.position - objTransform.parent.position)
                                .normalized * 0.3f;

            transform.SetPositionAndRotation(
                Vector3.Slerp(transform.position, 
                new Vector3(outerPosition.x, outerPosition.y + 0.3f, outerPosition.z), Speed * Time.deltaTime),
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
