using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using POLARIS.Managers;

public class EventListEntryController : ListEntryController
{
    Label NameLabel;
    Label DescriptionLabel;
    Label TimeLocationLabel;
    VisualElement image;

    public void SetVisualElement(VisualElement visualElement)
    {
        NameLabel = visualElement.Q<Label>("EventName");
        DescriptionLabel = visualElement.Q<Label>("Description");
        TimeLocationLabel = visualElement.Q<Label>("TimeLocation");
        image = visualElement.Q<VisualElement>(className: "panelImage") ;
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

    public void SetEventData(EventData eventData)
    {
        NameLabel.text = eventData.Name;
        //cull description here
        DescriptionLabel.text = eventData.Description;
        TimeLocationLabel.text = eventData.DateTime + " - " + eventData.ListedLocation;
        image.style.backgroundImage = eventData.rawImage;
    }
}

