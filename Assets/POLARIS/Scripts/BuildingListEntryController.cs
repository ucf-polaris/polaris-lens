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
    EventManager eventManager;
    //since it's all the same extended view, don't keep cloning a reference to the same extended view
    public static locationExtendedView extendedView;
    public static eventExtendedView otherView;
    
    private Geocoder geo;
    private UcfRouteManager _routeManager;
    private GameObject MenuUI;

    private void OutputFunction(ClickEvent evt)
    {
        //handle informational fields
        extendedView.TitleText.text = locationData.BuildingName;
        extendedView.AddressText.text = locationData.BuildingAddress;
        extendedView.DescriptionText.text = string.IsNullOrEmpty(locationData.BuildingDesc) ? "None" : locationData.BuildingDesc;

        //handle favorites
        extendedView.FavoritesIcon.UnregisterCallback<ClickEvent>(OnFavoritesClick);
        extendedView.FavoritesIcon.RegisterCallback<ClickEvent>(OnFavoritesClick);

        if (userManager.isFavorite(locationData))
        {
            extendedView.FavoritesIcon.RemoveFromClassList("isNotFavorited");
            extendedView.FavoritesIcon.AddToClassList("isFavorited");
        }
        else
        {
            extendedView.FavoritesIcon.RemoveFromClassList("isFavorited");
            extendedView.FavoritesIcon.AddToClassList("isNotFavorited");
        }

        //handle visited
        toggleVisited();

        //handle navigation
        extendedView.NavButton.UnregisterCallback<ClickEvent>(OnNavClick);
        extendedView.NavButton.RegisterCallback<ClickEvent>(OnNavClick);

        //handle events list
        int len = locationData.BuildingEvents != null ? locationData.BuildingEvents.Length : 0;
        extendedView.EventHeaderText.text = "Events (" + len + ")";
        extendedView.EventList.Clear();

        //if not null or empty, populate list
        if(otherView != null && locationData.BuildingEvents != null && locationData.BuildingEvents.Length != 0)
        {
            var events = eventManager.dataList.Where(evt => locationData.BuildingEvents.Any(s => s.Equals(evt.EventID)));
            foreach (var e in events)
            {
                Label label = new Label(e.Name);
                label.AddToClassList("EventText");
                label.RegisterCallback<ClickEvent, EventData>(OutputFunctionsForEvents, e);

                extendedView.EventList.Add(label);
            }
        }
        else
        {
            Label noneLabel = new Label("None");
            noneLabel.AddToClassList("EventText");
            extendedView.EventList.Add(noneLabel);
        }

        //extend location view, put down event view
        extendedView.Extended = true;
        otherView.Extended = false;
    }

    private void toggleVisited()
    {
        if (locationData.IsVisited)
        {
            extendedView.VisitedIcon.RemoveFromClassList("notVisited");
            extendedView.VisitedIcon.AddToClassList("Visited");
        }
        else
        {
            extendedView.VisitedIcon.RemoveFromClassList("Visited");
            extendedView.VisitedIcon.AddToClassList("notVisited");
        }
    }

    public void OutputFunctionsForEvents(ClickEvent evn, EventData evt)
    {
        //error checking
        if (extendedView == null) return;

        //set variables
        otherView.DescriptionText.text = HtmlParser.RichParse(evt.Description);
        otherView.image.style.backgroundImage = evt.rawImage;
        otherView.LocationText.text = evt.ListedLocation;
        otherView.StartDateText.text = evt.DateTime.ToString("f") + " to";
        otherView.EndDateText.text = evt.EndsOn.ToString("f");
        otherView.TitleText.text = evt.Name;
        otherView.HostText.text = evt.Host;

        otherView.ExtendedView.verticalScroller.value = otherView.ExtendedView.verticalScroller.lowValue;

        otherView.Extended = true;
    }

    private void OnFavoritesClick(ClickEvent evt)
    {
        
        LocationData location = locationManager.GetFromName(NameLabel.text);
        //not favorite -> favorite
        if (!userManager.isFavorite(location))
        {
            FavoriteElement.RemoveFromClassList("isNotFavorited");
            FavoriteElement.AddToClassList("isFavorited");

            //update extended view
            if(extendedView.FavoritesIcon != null)
            {
                extendedView.FavoritesIcon.RemoveFromClassList("isNotFavorited");
                extendedView.FavoritesIcon.AddToClassList("isFavorited");
            }

            userManager.UpdateFavorites(true, location);
        }
        //favorite -> not favorite
        else
        {
            FavoriteElement.RemoveFromClassList("isFavorited");
            FavoriteElement.AddToClassList("isNotFavorited");

            if(extendedView.FavoritesIcon != null)
            {
                extendedView.FavoritesIcon.RemoveFromClassList("isFavorited");
                extendedView.FavoritesIcon.AddToClassList("isNotFavorited");
            }

            userManager.UpdateFavorites(false, location);
        }
        evt.StopPropagation();
    }

    public void SetVisualElement(VisualElement visualElement)
    {
        userManager = UserManager.getInstance();
        locationManager = LocationManager.getInstance();
        eventManager = EventManager.getInstance();

        PanelEntity = visualElement.Q<VisualElement>(className: "panelEntity");
        PanelEntity.UnregisterCallback<ClickEvent>(OutputFunction);
        PanelEntity.RegisterCallback<ClickEvent>(OutputFunction);

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
            var distanceToBuilding = DistanceInMiBetweenEarthCoordinates(new double2(GetUserCurrentLocation._latitude, GetUserCurrentLocation._longitude), new double2(buildingData.BuildingLat, buildingData.BuildingLong));
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

        this.locationData = buildingData;
    }
    
    private double DistanceInMiBetweenEarthCoordinates(double2 pointA, double2 pointB) 
    {
        const int earthRadiusKm = 6371;
        const double KmToMi = 0.621371;

        var distLat = DegreesToRadians(pointB.x - pointA.x);
        var distLon = DegreesToRadians(pointB.y - pointA.y);

        var latA = DegreesToRadians(pointA.x);
        var latB = DegreesToRadians(pointB.x);

        var a = Math.Sin(distLat / 2) * Math.Sin(distLat / 2) +
                Math.Sin(distLon / 2) * Math.Sin(distLon / 2) * Math.Cos(latA) * Math.Cos(latB); 
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)); 
        return earthRadiusKm * c * KmToMi;
    }
        
    private static double DegreesToRadians(double degrees) 
    {
        return degrees * Math.PI / 180;
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

