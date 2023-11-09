using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using POLARIS.MainScene;
using POLARIS.Managers;
using Unity.Mathematics;
using System.Linq;
using POLARIS;

public class BuildingListEntryController
{
    Label NameLabel;
    Label DistanceLabel;
    Label AddressLabel;
    Label EventLabel;
    public VisualElement image;
    VisualElement FavoriteElement;
    Button NavigationButton;
    VisualElement PanelEntity;
    LocationData locationData;

    UserManager userManager;
    LocationManager locationManager;

    //since it's all the same extended view, don't keep cloning a reference to the same extended view
    public static locationExtendedView extendedView;
    public static eventExtendedView otherView;
    
    private Geocoder geo;
    private UcfRouteManager _routeManager;
    private GameObject MenuUI;
    private void OnPanelClick(ClickEvent evt)
    {
        extendedView.ExtendMenu(locationData, true);
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
        evt.StopPropagation();
    }

    public void SetVisualElement(VisualElement visualElement)
    {
        userManager = UserManager.getInstance();
        locationManager = LocationManager.getInstance();

        PanelEntity = visualElement.Q<VisualElement>(className: "panelEntity");
        PanelEntity.UnregisterCallback<ClickEvent>(OnPanelClick);
        PanelEntity.RegisterCallback<ClickEvent>(OnPanelClick);

        NameLabel = visualElement.Q<Label>(className: "panelTextLocation");
        DistanceLabel = visualElement.Q<Label>(className: "panelTextDistance");
        AddressLabel = visualElement.Q<Label>(className: "panelTextAddress");
        EventLabel = visualElement.Q<Label>(className: "panelTextEvents");

        FavoriteElement = visualElement.Q<VisualElement>(className: "panelFavoritesIcon");
        FavoriteElement.UnregisterCallback<ClickEvent>(OnFavoritesClick);
        FavoriteElement.RegisterCallback<ClickEvent>(OnFavoritesClick);

        NavigationButton = visualElement.Q<Button>("navigationButton");
        NavigationButton.UnregisterCallback<ClickEvent>(OnNavClick);
        NavigationButton.RegisterCallback<ClickEvent>(OnNavClick);

        image = visualElement.Q<VisualElement>(className: "panelImage");

        if (Camera.main != null)
        {
            _routeManager = Camera.main.transform.parent.gameObject
                                  .GetComponentInChildren<UcfRouteManager>();
            geo = Camera.main.transform.parent.gameObject
                .GetComponentInChildren<Geocoder>();
        }

        MenuUI = GameObject.Find("MenuUI");
    }

    public void SetBuildingData(LocationData buildingData)
    {
        NameLabel.text = buildingData.BuildingName;
        if (GetUserCurrentLocation.displayLocation)
        {
            var distanceToBuilding = locationManager.DistanceInMiBetweenEarthCoordinates(new double2(GetUserCurrentLocation._latitude, GetUserCurrentLocation._longitude), new double2(buildingData.BuildingLat, buildingData.BuildingLong));
            DistanceLabel.text = $"{distanceToBuilding:0.00} miles";
        }
        else
        {
            DistanceLabel.text = "N miles";
        }
        AddressLabel.text = buildingData.BuildingAddress == null ? "No Address Found - " : buildingData.BuildingAddress + " - ";
        EventLabel.text = (buildingData.BuildingEvents != null ? buildingData.BuildingEvents.Length : "0") + " Events";

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

        image.style.backgroundImage = buildingData.rawImage;
        this.locationData = buildingData;
    }
       

    private void OnNavClick(ClickEvent evt)
    {
        geo.MoveCameraToCoordinates(locationData.BuildingLong, locationData.BuildingLat);
        _routeManager.RouteToLocation(locationData);
        extendedView.Extended = false;
        otherView.Extended = false;

        if (MenuUI != null)
        {
            var tabImage = MenuUI.GetComponent<ChangeTabImage>();
            tabImage.CollapseMenu(null);
        }
        evt.StopPropagation();
    }
}

