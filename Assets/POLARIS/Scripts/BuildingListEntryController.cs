using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BuildingListEntryController
{
    Label NameLabel;
    Label DistanceLabel;
    Label AddressLabel;
    Label EventLabel;
    VisualElement FavoriteElement;
    VisualElement NavigationElement;

    public void SetVisualElement(VisualElement visualElement)
    {
        NameLabel = visualElement.Q<Label>(className: "panelTextLocation");
        DistanceLabel = visualElement.Q<Label>(className: "panelTextDistance");
        AddressLabel = visualElement.Q<Label>(className: "panelTextAddress");
        EventLabel = visualElement.Q<Label>(className: "panelTextEvents");
        FavoriteElement = visualElement.Q<Label>(className: "panelFavoritesIcon");
        NavigationElement = visualElement.Q<Label>(className: "panelNavigationIcon");
        /*
    .panelShadow
    .panel 
    .panelEntity
    .panelImage 
    .panelBlocker
    .panelTextArea
    .panelTextDistance
    .panelTextEvents
    .panelTextAddress
    .panelLocationGroup
    .panelTextLocation
    .panelNavigationIcon
    .panelFavoritesIcon 
    .spacing 
        */
    }

    public void SetBuildingData(Building buildingData)
    {
        NameLabel.text = buildingData.BuildingName;
        DistanceLabel.text = "N miles";
        AddressLabel.text = buildingData.BuildingAddress;
        EventLabel.text = buildingData.BuildingEvents.Length.ToString() + " Events";
        //FavoriteElement clickable event
        //NavigationElement clickable event
    }
}

