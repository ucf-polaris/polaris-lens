using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UIElements;

abstract public class TransitionClass : MonoBehaviour
{
    public GameObject blockingScreen;
    public abstract void TransitionInAction();
    public abstract void TransitionOutAction();

    public void PreTransition(TransitionStartEvent evt)
    {
        //Block the screen
        if (!blockingScreen.activeSelf) blockingScreen.SetActive(true);
        Debug.Log(blockingScreen.activeSelf);

        //unfocus on current element
        EventSystem e = EventSystem.current;
        if (e != null) e.SetSelectedGameObject(null);
    }

    public void PostTransition(TransitionEndEvent evt)
    {
        //Unblock the screen
        if(blockingScreen.activeSelf) blockingScreen.SetActive(false);

        //unfocus on current element
        EventSystem e = EventSystem.current;
        if (e != null) e.SetSelectedGameObject(null);
    }

}
