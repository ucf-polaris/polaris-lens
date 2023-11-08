using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using POLARIS.Managers;

public class MenUI_Dropdown : MonoBehaviour
{
    DropdownField dropDown;
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
        
    }
}
