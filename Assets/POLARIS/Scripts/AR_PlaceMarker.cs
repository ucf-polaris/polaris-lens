using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System;

public class AR_PlaceMarker : MonoBehaviour
{
    public GameObject groundBase;
    ARRaycastManager _raycastManager;
    DebugManager debug;
    Camera cam;
    List<ARRaycastHit> _Hits = new List<ARRaycastHit>();

    private float SpinningSpeed = 0.5f;
    private float SpinDest = 180f;
    private bool _hasHit = false;
    // Start is called before the first frame update
    void Start()
    {
        debug = DebugManager.GetInstance();
        _raycastManager = FindAnyObjectByType<ARRaycastManager>();
        cam = GameObject.Find("AR Camera").GetComponent<Camera>();
    }

    public void PlaceGroundMarker(Vector3 middle)
    {
        //if the raycast manager doesn't exist or the check already failed
        if (_raycastManager == null)
        {
            Debug.Log("NO RAYCAST MANAGER");
            return;
        }

        //Ray ray = new Ray(middle, Vector3.down);
        var ray = new Ray(middle, Vector3.down);
        //spin the base
        Quaternion spin = Quaternion.Slerp(groundBase.transform.rotation, Quaternion.Euler(270, 0, SpinDest), SpinningSpeed * Time.deltaTime);

        //change destination to keep base spinning
        if (Math.Abs(spin.z - SpinDest) <= 0.001f) SpinDest = (SpinDest + 180f) % 360;

        //try to raycast down to place marker
        if (_raycastManager.Raycast(ray, _Hits))
        {
            Vector3 pos = _Hits[0].pose.position;
            groundBase.transform.position = new Vector3(middle.x, pos.y, middle.z);
            groundBase.transform.localScale = new Vector3(40f, 40f, 20f);
            groundBase.transform.rotation = spin;
            _hasHit = true;
        }
        else
        {
            groundBase.transform.position = new Vector3(middle.x, 0, middle.z);
            groundBase.transform.localScale = new Vector3(0f, 0f, 0f);
            _hasHit = false;
        }
    }

    public void AddMessage(string place)
    {
        if(!_hasHit)
            debug.AddToMessage(place + " base active", "N/A");
        else
            debug.AddToMessage(place + " base active", "position: " + groundBase.transform.position.ToString() + " scale: " + groundBase.transform.localScale.ToString());
    }
}
