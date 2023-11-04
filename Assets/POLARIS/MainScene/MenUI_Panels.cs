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
        public Geocoder geo;

        //misc. variables
        private string currentTab;
        private bool _waitingForResponse = false;
        private Label header;
        [SerializeField] private bool showSuggestions = true;

        //Managers
        private UserManager userManager;
        private EventManager eventManager;
        private LocationManager locationManager;

        //UI Toolkit elements
        private TextField _searchField;
        private Button _searchButton;
        private Button _clearButton;

        //UI Toolkit Objects
        [SerializeField]
        VisualTreeAsset LocationListEntryTemplate;

        [SerializeField]
        VisualTreeAsset EventListEntryTemplate;

        public ListController listController;
        public extendedScrollView ExtendedScrollView;

        public static bool userOnListView;
        // Start is called before the first frame update
        private void Start()
        {
            //Initialize variables
            userManager = UserManager.getInstance();
            eventManager = EventManager.getInstance();
            locationManager = LocationManager.getInstance();
            currentTab = ChangeTabImage._lastPressed;
            listController = new ListController();

            var uiDoc = GetComponent<UIDocument>();
            var rootVisual = uiDoc.rootVisualElement;
            header = rootVisual.Q<Label>("Identifier");
            ExtendedScrollView = new extendedScrollView(rootVisual.Q<VisualElement>("ExtendedScrollContainer"));
            //passing back variables
            listController.Initialize(rootVisual, EventListEntryTemplate, LocationListEntryTemplate, ListController.SwitchType.locations);
            EventListEntryController.extendedView = ExtendedScrollView;
            BuildingListEntryController.extendedView = ExtendedScrollView;

            //set up the search bar
            _searchField = rootVisual.Q<TextField>("SearchBar");
            _searchField.RegisterValueChangedCallback(OnSearchValueChanged);
            
            var tab = GetComponent<ChangeTabImage>();
            _searchField.RegisterCallback<FocusEvent>(tab.RaiseMenu);
            
            _searchField.selectAllOnFocus = true;
            SetPlaceholderText(_searchField, currentTab == "location" ? "Search for locations" : "Search for events");

            StartCoroutine(FillSearch());
        }

        private IEnumerator FillSearch()
        {
            if (currentTab == "location")
            {
                while (locationManager.dataList.Count == 0) yield return null;
                List<LocationData> buildings = new List<LocationData>();
                if (showSuggestions)
                {
                    header.text = "Suggested Locations";
                    buildings = new List<LocationData>();
                    foreach (string buildingName in userManager.data.Suggested.Split("~"))
                    {
                        LocationData building = GetBuildingFromName(buildingName);
                        if (building != null) buildings.Add(building);
                    }
                }
                else
                {
                    header.text = "Locations";
                    buildings = locationManager.dataList;
                }
                UpdateBuildingSearchUI(buildings);
            }
            else
            {
                while (eventManager.dataList.Count == 0) yield return null;
                if (showSuggestions)
                {
                    header.text = "Suggested Events";
                }
                else
                {
                    header.text = "Events";
                }
                List<EventData> events = eventManager.dataList;
                UpdateEventSearchUI(events);
            }
        }
        
        private LocationData GetBuildingFromName(string buildingName)
        {
            foreach (LocationData building in locationManager.dataList)
            {
                if (string.Equals(buildingName, building.BuildingName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return building;
                }

                if (building.BuildingAllias == null) continue;
                foreach (string alias in building.BuildingAllias)
                {
                    if (String.Equals(buildingName, alias,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return building;
                    }
                }
            }

            return null;
        }

        // Update is called once per frame
        private void Update()
        {
            // Can move around map at top on event/location list view, but not when clicking on specific event
            // Tried doing same thing for ExtendedScrollView, but didn't work :(
            userOnListView = listController.EntryList.panel.focusController.focusedElement == listController.EntryList || ExtendedScrollView.Extended;
            
            if (ChangeTabImage.justRaised)
            {
                StartCoroutine(FillSearch());
                ChangeTabImage.justRaised = false;
            }
            
            if (ChangeTabImage._lastPressed == currentTab) return;

            // Just swapped tabs
            currentTab = ChangeTabImage._lastPressed;
            ClearSearchResults(true);
            _searchField.value = "";
            SetPlaceholderText(_searchField, ChangeTabImage._lastPressed == "location" ? "Search for locations" : "Search for events");
        }
        
        private void OnSearchValueChanged(ChangeEvent<string> evt)
        {
            if (ChangeTabImage.justRaised) ChangeTabImage.justRaised = false;
            string newText = evt.newValue;

            //from something to empty string
            if (newText.EndsWith("\n"))
            {
                Deselect();
                _searchField.value = newText.TrimEnd('\n');
                return;
            }

            //when you replace placeholder by focusing on text box
            bool flag = (evt.previousValue == "Search for locations" || evt.previousValue == "Search for events") && evt.newValue == "";
            if (currentTab == "location")
            {
                if (showSuggestions && newText == "")
                {
                    header.text = "Suggested Locations";
                    List<LocationData> buildings = new List<LocationData>();
                    foreach (string buildingName in userManager.data.Suggested.Split("~"))
                    {
                        LocationData building = GetBuildingFromName(buildingName);
                        if (building != null) buildings.Add(building);
                    }
                    UpdateBuildingSearchUI(buildings);
                }
                else
                {
                    header.text = "Locations";
                    List<LocationData> buildings = locationManager.GetBuildingsFromSearch(newText, newText.Length > 0 && newText[0] == '~');
                    UpdateBuildingSearchUI(buildings, !flag);
                }
            }
            else
            {
                if (showSuggestions && newText == "")
                {
                    header.text = "Suggested Events";
                }
                else
                {
                    header.text = "Events";
                }
                List<EventData> events = eventManager.GetEventsFromSearch(newText, newText.Length > 0 && newText[0] == '~');
                UpdateEventSearchUI(events, !flag);
            }
        }

        private void UpdateBuildingSearchUI(List<LocationData> buildings, bool shouldReset = true)
        {
            listController.Update(buildings);
            if (shouldReset)
            {
                ReturnGoToTop();
                ExtendedScrollView.Extended = false;
            }
        }

        private void UpdateEventSearchUI(List<EventData> events, bool shouldReset = true)
        {
            listController.Update(events);
            if (shouldReset)
            {
                ReturnGoToTop();
                ExtendedScrollView.Extended = false;
            }
        }
        

        private void Deselect()
        {
            // Deselect the text input field that was used to call this function. 
            // It is required so that the camera controller can be enabled/disabled when the input field is deselected/selected 
            var eventSystem = EventSystem.current;
            if (!eventSystem.alreadySelecting)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        private void FillInputField(string searchText)
        {
            _searchField.value = searchText;
        }

        private void ClearSearchResults(bool swap)
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

        public static void SetPlaceholderText(TextField textField, string placeholder)
        {
            string placeholderClass = TextField.ussClassName + "__placeholder";

            onFocusOut();
            textField.RegisterCallback<FocusInEvent>(_ => onFocusIn());
            textField.RegisterCallback<FocusOutEvent>(_ => onFocusOut());

            void onFocusIn()
            {
                if (textField.ClassListContains(placeholderClass))
                {
                    textField.value = string.Empty;
                    textField.RemoveFromClassList(placeholderClass);
                }
            }

            void onFocusOut()
            {
                if (string.IsNullOrEmpty(textField.text))
                {
                    textField.SetValueWithoutNotify(placeholder);
                    textField.AddToClassList(placeholderClass);
                }
            }
        }

        public void ReturnGoToTop()
        {
            ScrollView SV = listController.GetScrollView();
            SV.verticalScroller.value = SV.verticalScroller.lowValue;
            SV.scrollDecelerationRate = 0.0f;
        }
    }
    
    [Serializable]
    public class extendedScrollView
    {
        public ScrollView ExtendedView;
        public VisualElement ExtendedContainerView;
        private bool extended;
        public Label DescriptionText;
        public Label LocationText;
        public Label StartDateText;
        public Label EndDateText;
        public Label TitleText;
        public VisualElement image;

        public extendedScrollView(VisualElement container)
        {
            //ExtendScrollContainer
            ExtendedContainerView = container;
            ExtendedView = container.Q<ScrollView>("ExtendedScrollView");

            DescriptionText = container.Q<Label>("DescriptionText");
            LocationText = container.Q<Label>("LocationText");
            StartDateText = container.Q<Label>("StartDateText");
            EndDateText = container.Q<Label>("EndDateText");
            TitleText = container.Q<Label>("TitleText");
            image = container.Q<VisualElement>("ImagePop");

            //back click button
            container.Q<VisualElement>("BackClick").RegisterCallback<ClickEvent>(OnBackClick);
        }

        //also close when...
        //1. Search something new
        //2. Switch tab
        //3. Collapse Menu
        //only really concerned with what happens within this scene. Any scene swaps will reset everything
        private void OnBackClick(ClickEvent evt)
        {
            Extended = false;
        }

        public bool Extended { get => extended; set { extended = value; ExtendedContainerView.style.top = Length.Percent(value ? 10f : 110f); } }
    }
}
