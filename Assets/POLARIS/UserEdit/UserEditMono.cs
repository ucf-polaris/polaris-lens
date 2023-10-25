using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class UserEditMono : MonoBehaviour
{
    private UserEditTransition transition;

    private UserEditTransition.Press enter;
    private UserEditTransition.Press exit;
    private VisualElement background;
    public string enterName;
    public string exitName;
    // Start is called before the first frame update
    void Start()
    {
        //set values with uiDoc
        var uiDoc = gameObject.GetComponent<UIDocument>();

        //make transition object
        transition = GetComponent<UserEditTransition>();

        //set background variables
        background = uiDoc.rootVisualElement.Q("Background");
        background.style.bottom = Length.Percent(120);
        transition.SetClosed(background.style.bottom.value.Equals(Length.Percent(120)));

        //set values with transition buttons
        enter = new UserEditTransition.Press(uiDoc, enterName);
        exit = new UserEditTransition.Press(uiDoc, exitName);

        enter.AddEvent(OnOpenClick);
        exit.AddEvent(OnCloseClick);

        background.RegisterCallback<TransitionStartEvent>(transition.PreTransition);
        background.RegisterCallback<TransitionEndEvent>(transition.PostTransition);
    }

    private void OnOpenClick(ClickEvent evt)
    {
        transition.TransitionInAction();
    }

    private void OnCloseClick(ClickEvent evt)
    {
        transition.TransitionOutAction();
    }
}
