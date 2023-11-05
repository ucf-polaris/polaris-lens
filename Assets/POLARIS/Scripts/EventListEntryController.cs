using System.Collections;
using System.Collections.Generic;
using POLARIS;
using UnityEngine;
using UnityEngine.UIElements;
using POLARIS.Managers;
using POLARIS.MainScene;

public class EventListEntryController : ListEntryController
{
    Label NameLabel;
    Label DescriptionLabel;
    Label TimeLocationLabel;
    VisualElement image;
    VisualElement PanelEntity;
    //since it's all the same extended view, don't keep cloning a reference to the same extended view
    public static extendedScrollView extendedView;
    string location = "";
    string startDate = "";
    string endDate = "";
    string fullDescription = "";
    string fullTitle = "";

    private UcfRouteManager _routeManager;
    private EventData _eventData;

    public void OutputFunction(ClickEvent evt)
    {
        //error checking
        if (extendedView == null) return;

        //set variables
        extendedView.DescriptionText.text = fullDescription;
        extendedView.image.style.backgroundImage = image.style.backgroundImage;
        extendedView.LocationText.text = location;
        extendedView.StartDateText.text = startDate;
        extendedView.EndDateText.text = endDate;
        extendedView.TitleText.text = fullTitle;

        extendedView.ExtendedView.verticalScroller.value = extendedView.ExtendedView.verticalScroller.lowValue;
        
        extendedView.NavButton.clickable.clicked += OnNavClick;

        extendedView.Extended = true;
    }

    public void SetVisualElement(VisualElement visualElement)
    {
        PanelEntity = visualElement.Q<VisualElement>(className: "panelEntity");
        PanelEntity.UnregisterCallback<ClickEvent>(OutputFunction);
        PanelEntity.RegisterCallback<ClickEvent>(OutputFunction);

        NameLabel = visualElement.Q<Label>("EventName");
        DescriptionLabel = visualElement.Q<Label>("Description");
        TimeLocationLabel = visualElement.Q<Label>("TimeLocation");
        image = visualElement.Q<VisualElement>(className: "panelImage") ;

        if (Camera.main != null)
        {
            _routeManager = Camera.main.transform.parent.gameObject
                                  .GetComponentInChildren<UcfRouteManager>();
        }

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
    .panelTextLocations
    .panelNavigationIcon
    .panelFavoritesIcon 
    .spacing 
        */
    }

    private void OnNavClick()
    {
        _routeManager.RouteToEvent(_eventData);
    }

    public void SetEventData(EventData eventData)
    {
        _eventData = eventData;
        
        fullTitle = eventData.Name;
        NameLabel.text = cullText(eventData.Name, 35);

        fullDescription = HtmlParser.RichParse(eventData.Description);
        DescriptionLabel.text = cullText(fullDescription, 180);

        location = eventData.ListedLocation;
        startDate = eventData.DateTime.ToString("f") + " to";
        endDate = eventData.EndsOn.ToString("f");

        var splitDate = startDate.Split(",");
        string useDate = splitDate[1] + splitDate[2];
        TimeLocationLabel.text = cullText(useDate.Trim() + " - " + eventData.ListedLocation, 60);
        
        image.style.backgroundImage = eventData.rawImage;
    }

    private string cullText(string s, int length)
    {
        //get rid of all new lines
        s = s.Replace("\n", "");

        //get rid of any tabs?
        s = s.Replace("\t", "");

        if (s.Length <= length)
            return s;

        //get rid of excess
        return TruncateLongString(s, length) + "...";
    }
}

