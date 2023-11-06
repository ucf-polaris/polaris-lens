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
    public static eventExtendedView extendedView;

    
    private EventData _eventData;
    
    private void OnPanelClick(ClickEvent evt)
    {
        extendedView.ExtendMenu(_eventData, true);
    }

    public void SetVisualElement(VisualElement visualElement)
    {
        PanelEntity = visualElement.Q<VisualElement>(className: "panelEntity");
        PanelEntity.UnregisterCallback<ClickEvent>(OnPanelClick);
        PanelEntity.RegisterCallback<ClickEvent>(OnPanelClick);

        NameLabel = visualElement.Q<Label>("EventName");
        DescriptionLabel = visualElement.Q<Label>("Description");
        TimeLocationLabel = visualElement.Q<Label>("TimeLocation");
        image = visualElement.Q<VisualElement>(className: "panelImage") ;
    }

    public void SetEventData(EventData eventData)
    {
        _eventData = eventData;
        
        NameLabel.text = cullText(eventData.Name, 35);

        DescriptionLabel.text = cullText(HtmlParser.RichParse(_eventData.Description), 180);

        var splitDate = _eventData.DateTime.ToString("f").Split(",");
        string useDate = splitDate[1] + splitDate[2];
        TimeLocationLabel.text = cullText(useDate.Trim() + " - " + _eventData.ListedLocation, 60);
        
        image.style.backgroundImage = _eventData.rawImage;
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

