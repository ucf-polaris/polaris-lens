using System;
using System.Collections.Generic;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using Esri.HPFramework;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class PopulateEntires : MonoBehaviour
{
    private ListView listView;
    private List<string> items;
    // Start is called before the first frame update
    void Start()
    {
        items = new List<string>();
        Func<VisualElement> makeItem = () => new Label();
        Action<VisualElement, int> bindLocationItem = (VisualElement element, int index) => {
            (element as Label).text = items[index];
            (element as Label).RegisterCallback<ClickEvent>(_ => OutputTest());
            element.name = "ListedElement";
        };

        
        for(int i = 0; i < 10; i++)
        {
            items.Add("hello");
        }

        var rootVisual = GetComponent<UIDocument>().rootVisualElement;
        listView = rootVisual.Q<ListView>("SearchResultBigLabel");
        listView.makeItem = makeItem;
        listView.bindItem = bindLocationItem;
        listView.itemsSource = items;
        //listView.fixedItemHeight = 800;
        listView.Rebuild();
    }

    void OutputTest()
    {
        Debug.Log("WORKING!!!");
    }

    // Update is called once per frame
    void Update()
    {
        listView.Rebuild();
    }
}
