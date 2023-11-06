using System;
using System.Collections.Generic;
using System.Linq;
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
using POLARIS.Managers;
using POLARIS.MainScene;

public class ListController
{
    public enum SwitchType{
        locations,
        events
    }
    //UMXL template used for all entries
    VisualTreeAsset EventTemplate;
    VisualTreeAsset LocationTemplate;

    //UI elements in main scene
    public ListView EntryList;
    
    public SwitchType sw;

    private const float scrollDeceleration = 0.01f;

    public void Initialize(VisualElement root, VisualTreeAsset eventEntry, VisualTreeAsset locationEntry, SwitchType type)
    {
        sw = type;

        //initialize variables
        EntryList = root.Q<ListView>("SearchResultBigLabel");
        _buildingSearchList = new List<LocationData>();
        _eventSearchList = new List<EventData>();

        //template
        EventTemplate = eventEntry;
        LocationTemplate = locationEntry;

        if (sw == SwitchType.events)
            FillListEvent();
        else
            FillListBuilding();

        ConfigureListView();
    }

    //building data
    List<LocationData> _buildingSearchList;

    //event data
    List<EventData> _eventSearchList;
    
    public ScrollView GetScrollView()
    {
        return EntryList.Q<ScrollView>();
    }
    void ConfigureListView()
    {
        //set settings for list view
        EntryList.selectionType = SelectionType.None;
        //restore screen deceleration if touch
        EntryList.RegisterCallback<PointerDownEvent>(OnScreenTouch);

        //set settings for list view's scroll
        ScrollView SV = EntryList.Q<ScrollView>();
        SV.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        SV.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;
    }

    void FillListBuilding()
    {
        EntryList.makeItem = () =>
        {
            // instantiate new entry
            var newEntry = LocationTemplate.Instantiate();

            var EntryLogic = new BuildingListEntryController();

            // Assign controller script to visual element
            newEntry.userData = EntryLogic;

            // Initialize controller script
            EntryLogic.SetVisualElement(newEntry);

            // return root of new instansiated element
            return newEntry;
        };

        //bind function
        EntryList.bindItem = (item, index) =>
        {
            (item.userData as BuildingListEntryController)?.SetBuildingData(_buildingSearchList[index]);
        };

        // Set the actual item's source list/array
        EntryList.itemsSource = _buildingSearchList;
    }

    void FillListEvent()
    {
        EntryList.makeItem = () =>
        {
            // instantiate new entry
            var newEntry = EventTemplate.Instantiate();

            var EntryLogic = new EventListEntryController();

            // Assign controller script to visual element
            newEntry.userData = EntryLogic;

            // Initialize controller script
            EntryLogic.SetVisualElement(newEntry);

            // return root of new instantiated element
            return newEntry;
        };

        //bind function
        EntryList.bindItem = (item, index) =>
        {
            (item.userData as EventListEntryController)?.SetEventData(_eventSearchList[index]);
        };
        // Set the actual item's source list/array
        EntryList.itemsSource = _eventSearchList;
    }

    public void Update(List<LocationData> newList)
    {
        _buildingSearchList = newList;
        FillListBuilding();
        EntryList.Rebuild();

        sw = SwitchType.locations;
    }

    public void Update(List<EventData> newList)
    {
        _eventSearchList = newList;
        FillListEvent();
        EntryList.Rebuild();

        sw = SwitchType.events;
    }
    private void OnScreenTouch(PointerDownEvent evt)
    {
        GetScrollView().scrollDecelerationRate = scrollDeceleration;
    }
}
