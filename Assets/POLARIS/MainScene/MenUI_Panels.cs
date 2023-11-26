using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using POLARIS.Managers;

namespace POLARIS.MainScene {
    public class MenUI_Panels : MonoBehaviour
    {
        //Managers
        private EventManager eventManager;
        private LocationManager locationManager;

        //UI Toolkit Objects
        [SerializeField]
        VisualTreeAsset LocationListEntryTemplate;

        [SerializeField]
        VisualTreeAsset EventListEntryTemplate;

        public static ListController listController;
        public eventExtendedView ExtendedEventView;
        public locationExtendedView ExtendedLocationView;

        public static bool userOnListView;
        private Label resultsHeader;
        // Start is called before the first frame update
        private void Start()
        {
            //Initialize variables
            eventManager = EventManager.getInstance();
            locationManager = LocationManager.getInstance();
            listController = new ListController();

            var uiDoc = GetComponent<UIDocument>();
            var rootVisual = uiDoc.rootVisualElement;
            resultsHeader = rootVisual.Q<Label>("ResultsLabel");

            //define extended views
            ExtendedEventView = new eventExtendedView(rootVisual.Q<VisualElement>("ExtendedEventView"));
            ExtendedLocationView = new locationExtendedView(rootVisual.Q<VisualElement>("ExtendedLocationView"));

            ExtendedEventView.OtherView = ExtendedLocationView;
            ExtendedLocationView.OtherView = ExtendedEventView;

            //passing back variables
            listController.Initialize(rootVisual, EventListEntryTemplate, LocationListEntryTemplate, ListController.SwitchType.locations);
            EventListEntryController.extendedView = ExtendedEventView;
            BuildingListEntryController.extendedView = ExtendedLocationView;

            BuildingListEntryController.otherView = ExtendedEventView;

            locationManager.UpdateNeeded += RefreshListView;
            locationManager.UpdateNeeded += RefreshLocationExtendedView;

            //when image finishes downloading, refresh
            eventManager.ImageDownloaded += RefreshListView;
            eventManager.ImageDownloaded += RefreshEventExtendedView;
            locationManager.ImageDownloaded += RefreshListView;
            locationManager.ImageDownloaded += RefreshEventExtendedView;
        }

        private void OnDisable()
        {
            if(LocationManager.getInstance() != null)
            {
                locationManager.UpdateNeeded -= RefreshListView;
                locationManager.UpdateNeeded -= RefreshLocationExtendedView;
                locationManager.ImageDownloaded -= RefreshListView;
                locationManager.ImageDownloaded -= RefreshEventExtendedView;
            }

            if (EventManager.getInstance() != null)
            {
                eventManager.ImageDownloaded -= RefreshListView;
                eventManager.ImageDownloaded -= RefreshEventExtendedView;
            }
        }

        // Update is called once per frame
        private void Update()
        {
            userOnListView = ChangeTabImage._menuOpen || ExtendedEventView.Extended || ExtendedLocationView.Extended;
        }

        private void RefreshListView(object sender, EventArgs e)
        {
            listController.EntryList.Rebuild();
        }

        //this will be called when parking information is finished (will load stuff)
        private void RefreshLocationExtendedView(object sender, EventArgs e)
        {
            //if extended location view isn't up, then don't do anything
            if (!ExtendedLocationView.Extended) return;
            ExtendedLocationView.RefreshPage(false);
        }

        private void RefreshEventExtendedView(object sender, EventArgs e)
        {
            //if extended location view isn't up, then don't do anything
            if (!ExtendedEventView.Extended) return;
            ExtendedEventView.RefreshPage(false);
        }

        public void UpdateBuildingSearchUI(List<LocationData> buildings, bool shouldReset = true)
        {
            listController.Update(buildings);
            resultsHeader.text = buildings.Count.ToString() + " Results";
            if (shouldReset)
            {
                ReturnGoToTop();
                CollapseBothExtendedViews();
            }
        }

        public void UpdateEventSearchUI(List<EventData> events, bool shouldReset = true)
        {
            listController.Update(events);
            resultsHeader.text = events.Count.ToString() + " Results";
            if (shouldReset)
            {
                ReturnGoToTop();
                CollapseBothExtendedViews();
            }
        }

        public void ClearSearchResults(bool swap)
        {
            ListController.SwitchType type1 = ListController.SwitchType.locations;
            ListController.SwitchType type2 = ListController.SwitchType.events;
            if (swap)
            {
                type1 = ListController.SwitchType.events;
                type2 = ListController.SwitchType.locations;
            }

            //clear with empty version of list type
            if (listController.sw == type2)
                listController.Update(eventManager.dataList);
                // listController.Update(new List<EventData>());
            else if (listController.sw == type1)
                listController.Update(locationManager.dataList);
                // listController.Update(new List<Building>());
        }

        public void ReturnGoToTop()
        {
            ScrollView SV = listController.GetScrollView();
            SV.verticalScroller.value = SV.verticalScroller.lowValue;
            SV.scrollDecelerationRate = 0.0f;
        }

        public void CollapseBothExtendedViews()
        {
            ExtendedEventView.Extended = false;
            ExtendedLocationView.Extended = false;
        }
    }
    //----------------------------------EVENT EXTENDED VIEW----------------------------------
    [Serializable]
    public class eventExtendedView : extendedScrollView
    {
        public Label LocationText;
        public Label StartDateText;
        public Label EndDateText;
        public Label HostText;
        private EventData _eventData;
        public locationExtendedView OtherView;

        public eventExtendedView(VisualElement container) : base(container)
        {
            LocationText = container.Q<Label>("LocationText");
            StartDateText = container.Q<Label>("StartDateText");
            EndDateText = container.Q<Label>("EndDateText");
            HostText = container.Q<Label>("HostText");

            //back click button
            container.Q<VisualElement>("BackClick").RegisterCallback<ClickEvent>(OnBackClick);
        }
        public void ExtendMenu(EventData evtData, bool closeOther)
        {
            if (evtData == null) return;
            BackToTop();
            _eventData = evtData;
            RefreshPage(closeOther);
        }
        public void RefreshPage(bool closeOther)
        {
            //error checking
            if(_eventData == null)
            {
                Debug.Log("EventData is NULL");
                return;
            }

            //set variables
            DescriptionText.text = HtmlParser.RichParse(_eventData.Description);
            image.style.backgroundImage = _eventData.rawImage;
            LocationText.text = _eventData.ListedLocation;
            StartDateText.text = _eventData.DateTime.ToString("f") + " to";
            EndDateText.text = _eventData.EndsOn.ToString("f");
            TitleText.text = _eventData.Name;
            HostText.text = _eventData.Host;

            ExtendedView.verticalScroller.value = ExtendedView.verticalScroller.lowValue;

            NavButton.UnregisterCallback<ClickEvent>(OnNavClick);
            NavButton.RegisterCallback<ClickEvent>(OnNavClick);

            Extended = true;
            if(closeOther) OtherView.Extended = false;
        }

        private void OnNavClick(ClickEvent evt)
        {
            _routeManager.RouteToEvent(_eventData);
            Extended = false;
            OtherView.Extended = false;
            if (MenuUI != null)
            {
                var tabImage = MenuUI.GetComponent<ChangeTabImage>();
                tabImage.CollapseMenu(null);
            }
        }
    }
    //----------------------------------LOCATION EXTENDED VIEW----------------------------------
    [Serializable]
    public class locationExtendedView : extendedScrollView
    {
        private VisualElement container;
        public VisualElement FavoritesIcon;
        public VisualElement VisitedIcon;
        public VisualElement EventList;
        public VisualElement ParkingContainer;
        public VisualElement ParkingList;
        public VisualElement ParkingIcon;

        public Label AddressText;
        public Label EventHeaderText;
        
        private LocationData locationData;
        private UserManager userManager;
        private EventManager eventManager;
        private LocationManager locationManager;

        private Geocoder geo;
        public eventExtendedView OtherView;

        public locationExtendedView(VisualElement container) : base(container)
        {
            this.container = container;
            EventList = container.Q<VisualElement>("EventList");
            ParkingList = container.Q<VisualElement>("ParkingList");
            ParkingContainer = container.Q<VisualElement>("ParkingContainer");
            ParkingIcon = container.Q<VisualElement>("ParkIcon");

            EventHeaderText = container.Q<Label>("EventsLabel");
            AddressText = container.Q<Label>("AddressText");
            
            VisitedIcon = container.Q<VisualElement>("VisitedIcon");
            FavoritesIcon = container.Q<VisualElement>("FavoriteIcon");
            eventManager = EventManager.getInstance();
            userManager = UserManager.getInstance();
            locationManager = LocationManager.getInstance();

            if (Camera.main != null)
            {
                geo = Camera.main.transform.parent.gameObject
                    .GetComponentInChildren<Geocoder>();
            }
        }
        public void ExtendMenu(LocationData locData, bool closeOther)
        {
            if (locData == null) return;
            BackToTop();
            locationData = locData;
            RefreshPage(closeOther);
        }
        public void RefreshPage(bool closeOther)
        {
            if(locationData == null)
            {
                return;
            }
            //handle informational fields
            TitleText.text = locationData.BuildingName;
            AddressText.text = locationData.BuildingAddress;
            DescriptionText.text = string.IsNullOrEmpty(locationData.BuildingDesc) ? "None" : locationData.BuildingDesc;

            image.style.backgroundImage = locationData.rawImage;

            //handle favorites
            FavoritesIcon.UnregisterCallback<ClickEvent>(OnFavoritesClick);
            FavoritesIcon.RegisterCallback<ClickEvent>(OnFavoritesClick);

            if (userManager.isFavorite(locationData))
            {
                FavoritesIcon.RemoveFromClassList("isNotFavorited");
                FavoritesIcon.AddToClassList("isFavorited");
            }
            else
            {
                FavoritesIcon.RemoveFromClassList("isFavorited");
                FavoritesIcon.AddToClassList("isNotFavorited");
            }

            //handle visited
            toggleVisited();

            //handle navigation
            NavButton.UnregisterCallback<ClickEvent>(OnNavClick);
            NavButton.RegisterCallback<ClickEvent>(OnNavClick);

            //handle events list
            int len = locationData.BuildingEvents != null ? locationData.BuildingEvents.Length : 0;
            EventHeaderText.text = "Events (" + len + ")";
            EventList.Clear();

            //if not null or empty, populate list
            if (OtherView != null && locationData.BuildingEvents != null && locationData.BuildingEvents.Length != 0)
            {
                //location Manager datalist
                var events = eventManager.dataList.Where(evt => locationData.BuildingEvents.Any(s => s.Equals(evt.EventID)));
                foreach (var e in events)
                {
                    Label label = new Label(e.Name);
                    label.AddToClassList("EventText");
                    label.RegisterCallback<ClickEvent, EventData>(OpenOtherView, e);

                    EventList.Add(label);
                }
            }
            else
            {
                Label noneLabel = new Label("None");
                noneLabel.AddToClassList("EventText");
                EventList.Add(noneLabel);
            }

            //parking information (for garages)
            if (LocationManager.getInstance().garageList.ContainsKey(locationData.BuildingName))
            {
                ParkingContainer.style.display = DisplayStyle.Flex;
                ParkingIcon.style.display = DisplayStyle.Flex;
                PopulateParkingData();
            }
            else
            {
                //make disappear if location is not a parking garage
                ParkingContainer.style.display = DisplayStyle.None;
                ParkingIcon.style.display = DisplayStyle.None;
            }

            //extend location view, put down event view
            Extended = true;
            if(closeOther) OtherView.Extended = false;
        }

        private void PopulateParkingData()
        {
            //get variables
            GarageData data = LocationManager.getInstance().garageList[locationData.BuildingName];

            Label name = container.Q<Label>("GarageName");
            Label available = container.Q<Label>("Available");
            Label fulllabel = container.Q<Label>("FullLabel");
            VisualElement progress = container.Q<VisualElement>("InternalProgressBar");

            //set values
            name.text = data.name;
            available.text = "Available: " + data.available.ToString();

            int percentFull = (int)(Math.Ceiling(((float)data.occupied / (float)data.total) * 100));
            fulllabel.text = percentFull.ToString() + "%";

            progress.style.width = Length.Percent(percentFull);
        }

        private void OnFavoritesClick(ClickEvent evt)
        {
            LocationData location = locationManager.GetFromName(TitleText.text);
            //not favorite -> favorite
            if (!userManager.isFavorite(location))
            {
                FavoritesIcon.RemoveFromClassList("isNotFavorited");
                FavoritesIcon.AddToClassList("isFavorited");
                userManager.UpdateFavorites(true, location);
            }
            //favorite -> not favorite
            else
            {
                FavoritesIcon.RemoveFromClassList("isFavorited");
                FavoritesIcon.AddToClassList("isNotFavorited");
                userManager.UpdateFavorites(false, location);
            }
            MenUI_Panels.listController.EntryList.Rebuild();
        }

        private void toggleVisited()
        {
            if (locationData.IsVisited)
            {
                VisitedIcon.RemoveFromClassList("notVisited");
                VisitedIcon.AddToClassList("Visited");
            }
            else
            {
                VisitedIcon.RemoveFromClassList("Visited");
                VisitedIcon.AddToClassList("notVisited");
            }
        }

        private void OnNavClick(ClickEvent evt)
        {
            geo.MoveCameraToCoordinates(locationData.BuildingLong, locationData.BuildingLat);
            _routeManager.RouteToLocation(locationData);
            Extended = false;
            OtherView.Extended = false;

            if (MenuUI != null)
            {
                var tabImage = MenuUI.GetComponent<ChangeTabImage>();
                tabImage.CollapseMenu(null);
            }
            evt.StopPropagation();
        }

        private void OpenOtherView(ClickEvent evt, EventData ev)
        {
            OtherView.ExtendMenu(ev, false);
        }
    }
    //----------------------------------BASE EXTENDED VIEW----------------------------------
    [Serializable]
    public class extendedScrollView
    {
        public ScrollView ExtendedView;
        public VisualElement ExtendedContainerView;
        public Label DescriptionText;
        public VisualElement image;
        public Label TitleText;
        private bool extended;
        public Button NavButton;
        protected UcfRouteManager _routeManager;
        protected GameObject MenuUI;
        private float ScrollDecel;

        public extendedScrollView(VisualElement container)
        {
            ExtendedContainerView = container;
            ExtendedView = container.Q<ScrollView>("ExtendedScrollView");
            ScrollDecel = ExtendedView.scrollDecelerationRate;
            ExtendedView.RegisterCallback<PointerDownEvent>(OnTouchScreen);

            image = container.Q<VisualElement>("ImagePop");
            DescriptionText = container.Q<Label>("DescriptionText");
            TitleText = container.Q<Label>("TitleText");
            NavButton = container.Q<Button>("NavButton");

            //back click button
            container.Q<VisualElement>("BackClick").RegisterCallback<ClickEvent>(OnBackClick);

            if (Camera.main != null)
            {
                _routeManager = Camera.main.transform.parent.gameObject
                                      .GetComponentInChildren<UcfRouteManager>();
            }

            MenuUI = GameObject.Find("MenuUI");
        }

        //also close when... (Events)
        //1. Search something new
        //2. Switch tab
        //3. Collapse Menu
        //only really concerned with what happens within this scene. Any scene swaps will reset everything

        //also close when... (Locations)
        //1. Search something new
        //2. Switch tab
        //3. Collapse Menu
        //4. Press Events in Events List (maybe)
        //only really concerned with what happens within this scene. Any scene swaps will reset everything
        protected void OnBackClick(ClickEvent evt)
        {
            Extended = false;
        }

        public void BackToTop()
        {
            ExtendedView.verticalScroller.value = ExtendedView.verticalScroller.lowValue;
            ExtendedView.scrollDecelerationRate = 0.0f;
        }

        private void OnTouchScreen(PointerDownEvent evt)
        {
            ExtendedView.scrollDecelerationRate = ScrollDecel;
        }

        public bool Extended { get => extended; set { extended = value; ExtendedContainerView.style.top = Length.Percent(value ? 10f : 110f); } }
    }
}
