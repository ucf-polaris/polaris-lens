using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using POLARIS.Managers;
using POLARIS.MainScene;

public class MenUI_Dropdown : MonoBehaviour
{
    DropdownField dropDown;
    MenUI_Panels panels;
    public string CurrentTab { get => _currentTab; 
        set {
            _currentTab = value;
            if (dropDown == null) return;
            if (value == "location")
                dropDown.style.display = DisplayStyle.Flex;
            else
                dropDown.style.display = DisplayStyle.None;
        } }
    private string _currentTab = "";
    public static string currentChoice;

    // Start is called before the first frame update
    void Start()
    {
        panels = GetComponent<MenUI_Panels>();

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
            panels.OnChangeDropdown(currentChoice);
        });
    }

    // Update is called once per frame
    void Update()
    {
        //keep track if tab changes (get rid of drop down)
        CurrentTab = ChangeTabImage._lastPressed;
    }
}
