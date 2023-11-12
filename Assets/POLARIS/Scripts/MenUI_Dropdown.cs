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
    public static string currentChoice;

    // Start is called before the first frame update
    void Start()
    {
        search = GetComponent<MenUI_Search>();
        tab = GetComponent<ChangeTabImage>();

        var UIDoc = GetComponent<UIDocument>();
        dropDown = UIDoc.rootVisualElement.Q<DropdownField>("SearchFilter");

        dropDown.choices = new List<string>()
        {
            "SUGGESTED",
            "ALL",
            "VISITED",
            "NOT VISITED",
            "FAVORITES",
            "CLOSEST",
            "EVENTS"
        };

        dropDown.value = dropDown.choices[0];
        currentChoice = dropDown.value;

        //assign value to currentChoice;
        dropDown.RegisterCallback<ChangeEvent<string>>((evt) =>
        {
            currentChoice = evt.newValue;
            search.OnChangeDropdown(currentChoice);
        });

        tab.ChangeTab += OnChangeTab;
    }

    public void OnChangeTab(object sender, EventArgs e)
    {
        if (dropDown == null) return;
        if (tab.MyLastPressed == "location")
            dropDown.style.display = DisplayStyle.Flex;
        else
            dropDown.style.display = DisplayStyle.None;
    }
}
