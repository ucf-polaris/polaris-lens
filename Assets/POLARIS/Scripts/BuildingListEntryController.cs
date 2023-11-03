using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using POLARIS.MainScene;
using POLARIS.Managers;

public class BuildingListEntryController
{
    Label NameLabel;
    Label DistanceLabel;
    Label AddressLabel;
    Label EventLabel;
    public VisualElement image;
    VisualElement FavoriteElement;
    VisualElement NavigationElement;
    VisualElement PanelEntity;

    UserManager userManager;
    LocationManager locationManager;
    //since it's all the same extended view, don't keep cloning a reference to the same extended view
    public static extendedScrollView extendedView;

    private void OutputFunction(ClickEvent evt)
    {
        Debug.Log(NameLabel.text);
    }

    private void OnFavoritesClick(ClickEvent evt)
    {
        LocationData location = locationManager.GetFromName(NameLabel.text);
        //not favorite -> favorite
        if (!userManager.isFavorite(location))
        {
            FavoriteElement.RemoveFromClassList("isNotFavorited");
            FavoriteElement.AddToClassList("isFavorited");
            userManager.UpdateFavorites(true, location);
        }
        //favorite -> not favorite
        else
        {
            FavoriteElement.RemoveFromClassList("isFavorited");
            FavoriteElement.AddToClassList("isNotFavorited");
            userManager.UpdateFavorites(false, location);
        }
    }

    public void SetVisualElement(VisualElement visualElement)
    {
        userManager = UserManager.getInstance();
        locationManager = LocationManager.getInstance();
        PanelEntity = visualElement.Q<VisualElement>(className: "panelEntity");
        PanelEntity.UnregisterCallback<ClickEvent>(OutputFunction);
        PanelEntity.RegisterCallback<ClickEvent>(OutputFunction);

        NameLabel = visualElement.Q<Label>(className: "panelTextLocation");
        DistanceLabel = visualElement.Q<Label>(className: "panelTextDistance");
        AddressLabel = visualElement.Q<Label>(className: "panelTextAddress");
        EventLabel = visualElement.Q<Label>(className: "panelTextEvents");

        FavoriteElement = visualElement.Q<VisualElement>(className: "panelFavoritesIcon");
        PanelEntity.UnregisterCallback<ClickEvent>(OnFavoritesClick);
        PanelEntity.RegisterCallback<ClickEvent>(OnFavoritesClick);

        NavigationElement = visualElement.Q<VisualElement>(className: "panelNavigationIcon");
        image = visualElement.Q<VisualElement>(className: "panelImage");
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

    public void SetBuildingData(LocationData buildingData)
    {
        NameLabel.text = buildingData.BuildingName;
        DistanceLabel.text = "N miles";
        AddressLabel.text = buildingData.BuildingAddress == "none" ? "" : buildingData.BuildingAddress + " - ";
        EventLabel.text = (buildingData.BuildingEvents != null ? buildingData.BuildingEvents.Length.ToString() : "0") + " Events";

        LocationData location = locationManager.GetFromName(NameLabel.text);

        //set default style based on boolean
        if (userManager.isFavorite(location))
        {
            FavoriteElement.RemoveFromClassList("isNotFavorited");
            FavoriteElement.AddToClassList("isFavorited");
        }
        else
        {
            FavoriteElement.RemoveFromClassList("isFavorited");
            FavoriteElement.AddToClassList("isNotFavorited");
        }
    }
}

