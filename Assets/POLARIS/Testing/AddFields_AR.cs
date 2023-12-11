using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using POLARIS.GeospatialScene;


public class AddFields_AR : MonoBehaviour
{
    public AROcclusionManager occlusion;
    DebugManager debug;
    // Start is called before the first frame update
    void Start()
    {
        debug = DebugManager.GetInstance();
        if(occlusion == null) occlusion = FindObjectOfType<AROcclusionManager>();
        if (occlusion != null)
        {
            occlusion.enabled = false;
            debug.AddToButton("occlusion", ToggleOcclusion);
            debug.AddToMessage("occlusion", occlusion.enabled.ToString());
        }

    }

    public void ToggleOcclusion()
    {
        occlusion.enabled = !occlusion.enabled;
        debug.AddToMessage("occlusion", occlusion.enabled.ToString());
    }
}
