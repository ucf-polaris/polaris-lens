using System.Collections.Generic;
using Unity.Mathematics;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace POLARIS.GeospatialScene
{
    public class FaceCamera : MonoBehaviour
    {
        public float Speed;
        private float MAX_SPEED = 3.5f;
        public bool Zoomed { get; set; }
        
        private GameObject _arCamera;
        private GameObject _bottomPanel;
        private AR_PlaceMarker groundScript;

        DebugManager debug;
        string Named = "Default";

        private float _forwardAmount;
        public TransformMode posTrans = TransformMode.Anchor;
        public TransformMode rotTrans = TransformMode.Regular;
        private Vector3 truePosition = new Vector3();
        private Quaternion trueRotation = new Quaternion();

        // Start is called before the first frame update
        private void Start()
        {
            _arCamera = GameObject.FindGameObjectWithTag("MainCamera");
            
            var goList = new List<GameObject>();
            gameObject.GetChildGameObjects(goList);
            _bottomPanel = goList.Find(go => go.name.Equals("BottomPanel"));

            var fov = _arCamera.GetComponent<Camera>().fieldOfView;
            var panelWidth = _bottomPanel.GetComponent<BoxCollider>().size.x 
                             * _bottomPanel.transform.localScale.x 
                             * _bottomPanel.transform.parent.localScale.x;

            _forwardAmount = (float)((panelWidth / 2) / math.tan(PanelManager.DegreesToRadians(fov / 2)));
            // Add margin
            _forwardAmount *= 2.4f;

            //places the grounded base to mark where the panel is on the ground
            groundScript = GetComponent<AR_PlaceMarker>();
        }

        private void OnEnable()
        {
            debug = DebugManager.GetInstance();
        }
        public void SetName(string s)
        {
            Named = s;
            debug.AddToButton(s, ButtonShift);
            //debug.AddToButton(s + " ADD", ButtonAddSpeed);
            //debug.AddToButton(s + " MINUS", ButtonRemoveSpeed);
            debug.AddToButton(s + " POS", PosTransform);
            debug.AddToButton(s + " ROT", RotTransform);
        }

        private void ButtonShift()
        {
            Speed = Speed == 0f ? MAX_SPEED : 0f;
        }
        
        /*private void ButtonAddSpeed()
        {
            MAX_SPEED += 0.5f;
            debug.RemoveFromButton(Named);
            debug.AddToButton(Named, ButtonShift);
        }
        private void ButtonRemoveSpeed()
        {
            MAX_SPEED = MAX_SPEED <=  0 ? 0 : MAX_SPEED - 0.5f;
            debug.RemoveFromButton(Named);
            debug.AddToButton(Named, ButtonShift);
        }*/

        private void PosTransform()
        {
            if (posTrans == (TransformMode)(TransformMode.GetNames(typeof(TransformMode)).Length - 1))
                posTrans = 0;
            else
                posTrans = (posTrans + 1);
            //if (debug != null) debug.AddToMessage(Named + " PosTransform", posTrans.ToString());
        }

        private void RotTransform()
        {
            if (rotTrans == (TransformMode)(TransformMode.GetNames(typeof(TransformMode)).Length - 1))
                rotTrans = 0;
            else
                rotTrans = (rotTrans + 1);
            //if (debug != null) debug.AddToMessage(Named + " RotTransform", rotTrans.ToString());
        }

        public void SetValues(Vector3 pos, Quaternion rot)
        {
            truePosition = pos;
            trueRotation = rot;
        }

        // Update is called once per frame
        private void Update()
        {
            //if(debug != null) debug.AddToMessage(Named + " Speed", Speed.ToString());
            //if (debug != null) debug.AddToMessage(Named + " Button Speed", MAX_SPEED.ToString());
            var objTransform = transform;
            if (Zoomed)
            {
                // TODO: Scale down panel to set size
                var zoomPos = Vector3.forward * _forwardAmount;
                if (_bottomPanel.activeSelf)
                {
                    zoomPos = (Vector3.forward * _forwardAmount) + (Vector3.up * (_forwardAmount / 3));
                }

                groundScript.UseLastKnown();
                groundScript.AddMessage(Named);

                objTransform.SetLocalPositionAndRotation(
                    Vector3.Slerp(objTransform.localPosition, zoomPos, Speed * 4 * Time.deltaTime),
                    Quaternion.Slerp(objTransform.localRotation, Quaternion.Euler(0, 180, 0), Speed * 4 * Time.deltaTime));
                debug.AddToMessage(Named + " transform pos", objTransform.position.ToString());
                debug.AddToMessage(Named + " transform rot", objTransform.rotation.ToString());

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

            var usePosition = Vector3.Slerp(transform.position, new Vector3(outerPosition.x, outerPosition.y + 0.3f, outerPosition.z), Speed * Time.deltaTime);
            var useRotation = Quaternion.Slerp(transform.rotation, targetRotation, Speed * Time.deltaTime);

            switch (posTrans){
                case TransformMode.Anchor:
                    usePosition = truePosition;
                    break;
                case TransformMode.Local:
                    break;
                default:
                    break;
            }

            switch (rotTrans)
            {
                case TransformMode.Anchor:
                    useRotation = trueRotation;
                    break;
                case TransformMode.Local:
                    break;
                default:
                    break;
            }

            transform.SetPositionAndRotation(usePosition, useRotation);

            groundScript.PlaceGroundMarker(usePosition);
            groundScript.AddMessage(Named);

            //debug.AddToMessage(Named + " transform pos", objTransform.position.ToString());
            //debug.AddToMessage(Named + " transform rot", objTransform.rotation.ToString());
        }

        private void OnDisable()
        {
            debug.RemoveFromButton(Named);
            debug.RemoveFromMessage(Named + " Speed");
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

    public enum TransformMode
    {
        Regular,
        Local,
        Anchor
    }
}
