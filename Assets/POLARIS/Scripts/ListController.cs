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

public class ListController
{
    //UMXL template used for all entries
    VisualTreeAsset EntryTemplate;

    //UI elements in main scene
    ListView EntryList;

    public void Initialize(VisualElement root, VisualTreeAsset template)
    {
        _buildingSearchList = new List<Building>();

        //template
        EntryTemplate = template;

        //get list view
        EntryList = root.Q<ListView>("SearchResultBigLabel");

        //fill list with nothing initially
        FillList();
    }

    //building data
    List<Building> _buildingSearchList;

    void FillList()
    {
        EntryList.makeItem = () =>
        {
            // instantiate new entry
            var newEntry = EntryTemplate.Instantiate();

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
            (item.userData as BuildingListEntryController).SetBuildingData(_buildingSearchList[index]);
        };

        // Set the actual item's source list/array
        EntryList.itemsSource = _buildingSearchList;
        
        //set settings for list view
        EntryList.selectionType = SelectionType.None;

        //set settings for list view's scroll
        ScrollView SV = EntryList.Q<ScrollView>();
        SV.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        SV.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;
        SV.scrollDecelerationRate = 0.25f;
        SV.elasticity = 0.01f;
    }

    public void Update(List<Building> newList)
    {
        _buildingSearchList = newList;
        EntryList.itemsSource = _buildingSearchList;

        EntryList.bindItem = (item, index) =>
        {
            (item.userData as BuildingListEntryController).SetBuildingData(_buildingSearchList[index]);
        };
        EntryList.Rebuild();
    }
}
