using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using POLARIS.GeospatialScene;

public class DebugMenu : MonoBehaviour
{
    PanelManager panelManager;
    // Start is called before the first frame update
    void Start()
    {
        if(!Debug.isDebugBuild && !Application.isEditor)
        {
            gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
