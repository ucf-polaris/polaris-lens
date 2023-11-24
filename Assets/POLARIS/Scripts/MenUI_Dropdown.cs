using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using POLARIS.Managers;
using POLARIS.MainScene;
using System;

public class MenUI_Dropdown : MonoBehaviour
{
    DropdownField dropDown;
    MenUI_Search search;
    ChangeTabImage tab;
    public DropdownOptions Locations;
    public DropdownOptions Events;


    // Start is called before the first frame update
    void Start()
    {
        search = GetComponent<MenUI_Search>();
        tab = GetComponent<ChangeTabImage>();

        UIDocument UIDoc = GetComponent<UIDocument>();
        dropDown = UIDoc.rootVisualElement.Q<DropdownField>("SearchFilter");
        
        Locations = new DropdownOptions(new List<string>()
            {
                "SUGGESTED",
                "ALL",
                "VISITED",
                "NOT VISITED",
                "FAVORITES",
                "CLOSEST",
                "EVENTS"
            }, search.OnChangeDropdownLocation);

        Events = new DropdownOptions(new List<string>()
            {
                "ALL",
                "ASCEND DATE",
                "DESCEND DATE",
                "UPCOMING",
                "DISTANCE"
            }, search.OnChangeDropDownEvent);
        

        tab.ChangeTab += OnChangeTab;
    }

    public void OnChangeTab(object sender, EventArgs e)
    {
        if (dropDown == null) return;
        if (tab.MyLastPressed == "location")
            GetDropDown(Locations);
        else
            GetDropDown(Events);
    }

    public DropdownField GetDropDown(DropdownOptions options)
    {
        options.ApplyOptions(dropDown);
        return dropDown;
    }
}

public class DropdownOptions
{
    public string currentChoice;
    private int funcIndex;
    //have universal list of functions all DropdownOptions will access so you know what to remove
    static List<EventCallback<ChangeEvent<string>>> func = new List<EventCallback<ChangeEvent<string>>>();
    List<string> choices;

    public DropdownOptions(List<string> choices, Action<string> f)
    {
        this.choices = choices;
        this.currentChoice = this.choices[0];

        //get function's index
        funcIndex = func.Count;

        void add_f(ChangeEvent<string> evt)
        {
            currentChoice = evt.newValue;
            //update(currentChoice);
            f(currentChoice);
        }
        func.Add(add_f); 
    }

    public void ApplyOptions(DropdownField dropDown)
    {
        dropDown.choices = choices;
        dropDown.value = currentChoice;

        for (var i = 0; i < func.Count; i++) 
        {
            if (i == funcIndex) continue;
            dropDown.UnregisterCallback<ChangeEvent<string>>(func[i]);
        }

        dropDown.RegisterCallback<ChangeEvent<string>>(func[funcIndex]);
    }
}
