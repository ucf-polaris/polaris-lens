using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;

public class DisableInput : MonoBehaviour
{
    void OnEnable()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }

    public Focusable getFocusedElement()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return null;
        }

        GameObject selectedGameObject = eventSystem.currentSelectedGameObject;
        if (selectedGameObject == null)
        {
            return null;
        }

        PanelEventHandler panelEventHandler = selectedGameObject.GetComponent<PanelEventHandler>();
        if (panelEventHandler != null)
        {
            return panelEventHandler.panel.focusController.focusedElement;
        }

        return null;
    }
}
