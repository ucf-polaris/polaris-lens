using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using POLARIS.GeospatialScene;
using UnityEngine.UIElements;
using System;

public class DebugMenu : MonoBehaviour
{
    VisualElement popOut;
    VisualElement buttonContainer;
    Label debugInformation;
    DebugManager data;
    ScrollView scroll;
    float _loadTime;
    public float THREASH = 0;

    bool isOut = false;
    // Start is called before the first frame update
    void Start()
    {
        var UIDoc = GetComponent<UIDocument>();
        popOut = UIDoc.rootVisualElement.Q<VisualElement>("PopOut");
        debugInformation = UIDoc.rootVisualElement.Q<Label>("DebugInformation");
        buttonContainer = UIDoc.rootVisualElement.Q<VisualElement>("ButtonContainer");
        scroll = UIDoc.rootVisualElement.Q<ScrollView>("Scroll");
        popOut.RegisterCallback<ClickEvent>(OnClick);
        data = DebugManager.GetInstance();
        _loadTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - _loadTime > THREASH)
        {
            _loadTime = Time.time;
            UpdateText();
        }
    }

    private void UpdateText()
    {
        debugInformation.text = data.GetMessage();

        foreach(string b in data.Buttons.Keys)
        {
            Button check = buttonContainer.Q<Button>(b);
            if (check != null) continue;

            Button but = new Button();
            but.AddToClassList("PlayButton");
            but.text = b;
            but.name = b;
            but.clickable.clicked += data.Buttons[b];
            buttonContainer.Add(but);
        }
    }

    public void ClearButtons()
    {
        buttonContainer.Clear();
    }

    void OnClick(ClickEvent evt)
    {
        StartCoroutine(ToggleInformation());
    }

    IEnumerator ToggleInformation()
    {
        DisplayStyle data = isOut ? DisplayStyle.None : DisplayStyle.Flex;
        scroll.style.display = data;
        yield return null;
        isOut = !isOut;
    }
}
