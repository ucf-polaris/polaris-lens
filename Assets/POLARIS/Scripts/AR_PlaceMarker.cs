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
    List<ARRaycastHit> _Hits = new List<ARRaycastHit>();
    Vector3 lastKnownPos = Vector3.positiveInfinity;

    private float rotationAmount = 180f;
    private float rotationRate = 10.0f;  //speed of rotation in degrees/sec
    private Quaternion startingRotation;
    private Quaternion destinationRotation;
    private float rotationProgress = 0.0f;
    private float rotationDuration; // How long the rotation will take


    private bool _hasHit = false;
    private string Named = "";
    // Start is called before the first frame update
    void Start()
    {
        debug = DebugManager.GetInstance();
        _raycastManager = FindAnyObjectByType<ARRaycastManager>();

        rotationDuration = rotationAmount / rotationRate;

        groundBase.transform.rotation = Quaternion.Euler(270f, groundBase.transform.rotation.eulerAngles.y, 0f);
        startingRotation = groundBase.transform.rotation;

        //Convert z-axis rotation in degrees to a quaternion
        destinationRotation = groundBase.transform.rotation * Quaternion.Euler(0.0f, 0.0f, rotationAmount);
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
        SpinBase();

        //try to raycast down to place marker
        if (_raycastManager.Raycast(ray, _Hits))
        {
            Vector3 pos = _Hits[0].pose.position;
            groundBase.transform.position = new Vector3(middle.x, pos.y, middle.z);
            lastKnownPos = pos;
            groundBase.transform.localScale = new Vector3(40f, 40f, 20f);
            _hasHit = true;
        }
        else
        {
            if(lastKnownPos == Vector3.positiveInfinity)
            {
                groundBase.transform.position = new Vector3(middle.x, 0, middle.z);
                groundBase.transform.localScale = new Vector3(0f, 0f, 0f);
            }
            else
            {
                groundBase.transform.position = new Vector3(middle.x, lastKnownPos.y, middle.z);
                groundBase.transform.localScale = new Vector3(40f, 40f, 20f);
            }
            _hasHit = false;
        }
    }

    public void UseLastKnown()
    {
        SpinBase();
        if (lastKnownPos == Vector3.positiveInfinity)
        {
            groundBase.transform.localScale = new Vector3(0f, 0f, 0f);
        }
        else
        {
            groundBase.transform.position = new Vector3(lastKnownPos.x, lastKnownPos.y, lastKnownPos.z);
            groundBase.transform.localScale = new Vector3(40f, 40f, 20f);
        }
    }

    private void SpinBase()
    {
        debug.AddToMessage(Named + " BASE ROT", groundBase.transform.rotation.eulerAngles.ToString());
        float rotateAmount = Time.deltaTime / rotationDuration;
        if (rotationProgress < 1.0f - rotateAmount)
        {
            rotationProgress += rotateAmount;
            groundBase.transform.rotation = Quaternion.Slerp(startingRotation, destinationRotation, rotationProgress);
        }
        else
        {
            //reset
            rotationProgress = 0;
            groundBase.transform.rotation = startingRotation;
        }
    }

    public void AddMessage(string place)
    {
        Named = place;
        if(!_hasHit)
            debug.AddToMessage(place + " base active", "N/A");
        else
            debug.AddToMessage(place + " base active", "position: " + groundBase.transform.position.ToString() + " rotation: " + groundBase.transform.rotation.eulerAngles.ToString());
    }
}
