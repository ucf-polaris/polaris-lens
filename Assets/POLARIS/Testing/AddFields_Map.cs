using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class AddFields_Map : MonoBehaviour
{
    DebugManager debug;
    AROcclusionManager occlusion;
    // Start is called before the first frame update
    void Start()
    {
        debug = DebugManager.GetInstance();
        occlusion = FindObjectOfType<AROcclusionManager>();
    }

    
}
