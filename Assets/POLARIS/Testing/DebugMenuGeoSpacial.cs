using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using POLARIS.GeospatialScene;
using UnityEngine.UIElements;

public class DebugMenuGeoSpacial : MonoBehaviour
{
    PanelManager panelManager;
    VisualElement container;
    VisualElement popOut;
    VisualElement cont;

    bool isOut = false;
    // Start is called before the first frame update
    void Start()
    {
        panelManager = GameObject.Find("PanelManager").GetComponent<PanelManager>();
        /*if (!Debug.isDebugBuild && !Application.isEditor)
        {
            gameObject.SetActive(false);
        }*/
        var UIDoc = GetComponent<UIDocument>();
        container = UIDoc.rootVisualElement.Q<VisualElement>("TextContainer");
        cont = UIDoc.rootVisualElement.Q<VisualElement>("Container");
        popOut = UIDoc.rootVisualElement.Q<VisualElement>("PopOut");
        popOut.RegisterCallback<ClickEvent>(OnClick);

    }

    // Update is called once per frame
    void Update()
    {
        container.Clear();
        Label l = new Label("LoadNearby: ");
        l.AddToClassList("textSequence");
        container.Add(l);

        foreach (var pairing in panelManager.panel_Test)
        {
            Label label = new Label(pairing.Key + ": " + pairing.Value.ToString());
            label.AddToClassList("textSequence");
            container.Add(label);
        }

        Label m = new Label("FetchNearby: ");
        m.AddToClassList("textSequence");
        container.Add(m);

        foreach (var building in panelManager.location_test)
        {
            Label label = new Label(building.BuildingName);
            label.AddToClassList("textSequence");
            container.Add(label);
        }
    }

    void OnClick(ClickEvent evt)
    {
        isOut = !isOut;
        cont.style.left = Length.Percent(isOut ? 0 : -33);
    }
}
