using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using POLARIS.Managers;

public class MenUI_Dropdown : MonoBehaviour
{
    DropdownField dropDown;
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
    // Start is called before the first frame update
    void Start()
    {
        var UIDoc = GetComponent<UIDocument>();
        dropDown = UIDoc.rootVisualElement.Q<DropdownField>("SearchFilter");

        dropDown.choices = new List<string>()
        {
            "SUGGESTED",
            "ALL",
            "VISITED",
            "NOT VISITED",
            "FAVORITES"
        };

        dropDown.value = dropDown.choices[0];
    }

    // Update is called once per frame
    void Update()
    {
        CurrentTab = ChangeTabImage._lastPressed;
    }
}
