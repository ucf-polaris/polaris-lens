using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class UserEditTransition : TransitionClass
{
    [SerializeField]
    private bool closed;
    private VisualElement background;
    private Press enter;
    private Press exit;

    public string enterName;
    public string exitName;
    public void Start()
    {
        UIDocument uiDoc = gameObject.GetComponent<UIDocument>();
        background = uiDoc.rootVisualElement.Q<VisualElement>("Background");

        //set values with transition buttons
        enter = new Press(uiDoc, enterName);
        exit = new Press(uiDoc, exitName);

        //set background variables
        background = uiDoc.rootVisualElement.Q("Background");
        background.style.bottom = Length.Percent(120);
        closed = background.style.bottom.value.Equals(Length.Percent(120));

        //set events
        enter.AddEvent(OnOpenClick);
        exit.AddEvent(OnCloseClick);

        //background.RegisterCallback<TransitionRunEvent>(PreTransition);
        background.RegisterCallback<TransitionEndEvent>(PostTransition);
    }

    [Serializable]
    public class Press
    {
        private UIDocument uidoc;
        private string search;
        public Press(UIDocument uidoc, string search)
        {
            this.uidoc = uidoc;
            this.search = search;
        }

        public void AddEvent(Action a)
        {
            Button Clickable = uidoc.rootVisualElement.Q<Button>(search);
            if (Clickable != null)
            {
                Clickable.clickable.clicked += a;
            }
        }
        public void AddEvent(EventCallback<ClickEvent> a)
        {
            VisualElement Clickable = uidoc.rootVisualElement.Q(search);
            if (Clickable != null)
            {
                Clickable.RegisterCallback(a);
            }
        }
    }
    override public void TransitionInAction()
    {
        if (background == null)
        {
            Debug.LogWarning("Background isnt set");
            return;
        }
        background.style.bottom = Length.Percent(0);
        closed = false;
    }

    override public void TransitionOutAction()
    {
        if (background == null)
        {
            Debug.LogWarning("Background isnt set");
            return;
        }
        background.style.bottom = Length.Percent(120);
        closed = true;
    }

    private void OnOpenClick(ClickEvent evt)
    {
        PreTransition(null);
        TransitionInAction();
    }

    private void OnCloseClick(ClickEvent evt)
    {
        PreTransition(null);
        TransitionOutAction();
    }
    public void SetClosed(bool b)
    {
        closed = b;
    }
    public bool GetClosed()
    {
        return closed;
    }
}
