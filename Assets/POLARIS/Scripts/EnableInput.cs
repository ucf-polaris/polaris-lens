using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EnableInput : MonoBehaviour
{
    public void InputEnable()
    {
        GameObject.Find("EventSystem").GetComponent<EventSystem>().enabled = true;
    }
}
