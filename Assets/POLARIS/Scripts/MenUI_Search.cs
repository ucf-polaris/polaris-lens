using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using POLARIS.Managers;

namespace POLARIS.MainScene
{
    public class MenUI_Search : MonoBehaviour
    {
        [SerializeField] private bool showSuggestions = true;
        private bool shouldTriggerOnChangeEvent = true;

        //Managers
        private UserManager userManager;
        private EventManager eventManager;
        private LocationManager locationManager;

        private MenUI_Panels panels;
        private ChangeTabImage tab;
        private MenUI_Dropdown drop;

        //UI Toolkit elements
        private TextField _searchField;
        private Label header;
        private Label resultsHeader;

        // Start is called before the first frame update
        void Start()
        {
            //manager managing
            userManager = UserManager.getInstance();
            eventManager = EventManager.getInstance();
            locationManager = LocationManager.getInstance();

            panels = GetComponent<MenUI_Panels>();
            tab = GetComponent<ChangeTabImage>();
            drop = GetComponent<MenUI_Dropdown>();

            //change this to event system
            //currentTab = ChangeTabImage._lastPressed;
            tab.ChangeTab += OnChangeTab;

            //ui document information
            var uiDoc = GetComponent<UIDocument>();
            var rootVisual = uiDoc.rootVisualElement;
            header = rootVisual.Q<Label>("Identifier");
            resultsHeader = rootVisual.Q<Label>("ResultsLabel");

            //set up the search bar
            _searchField = rootVisual.Q<TextField>("SearchBar");
            _searchField.RegisterValueChangedCallback(OnSearchValueChanged);
            _searchField.RegisterCallback<FocusEvent>(tab.RaiseMenu);
            _searchField.selectAllOnFocus = true;
            SetPlaceholderText(_searchField, tab.MyLastPressed == "location" ? "Search for locations" : "Search for events");

            StartCoroutine(FillSearch());
        }

        private void Update()
        {
            if (ChangeTabImage.justRaised)
            {
                StartCoroutine(FillSearch());
                ChangeTabImage.justRaised = false;
            }
        }

        public void OnChangeTab(object sender, EventArgs e)
        {
            panels.ClearSearchResults(true);
            _searchField.value = "";
            SetPlaceholderText(_searchField, tab.MyLastPressed == "location" ? "Search for locations" : "Search for events");
        }

        private IEnumerator FillSearch()
        {
            if (tab.MyLastPressed == "location")
            {
                while (locationManager.dataList.Count == 0) yield return null;
                //call to set default values (especially when you swap tabs)
                OnChangeDropdownLocation(drop.GetDropDown(drop.Locations).value);
            }
            else
            {
                resultsHeader.style.display = DisplayStyle.Flex;
                while (eventManager.dataList.Count == 0) yield return null;
                OnChangeDropDownEvent(drop.GetDropDown(drop.Events).value);
            }
        }

        private List<LocationData> SearchSuggestedLocations()
        {
            resultsHeader.style.display = DisplayStyle.None;
            List<LocationData> buildings = new List<LocationData>();
            if (userManager.data.Suggested != null)
            {
                foreach (string buildingName in userManager.data.Suggested.Split("~"))
                {
                    LocationData building = GetBuildingFromName(buildingName);
                    if (building != null) buildings.Add(building);
                }
            }

            return buildings;
        }

        private void HandleLocationFilter(string newText, bool flag)
        {
            var dropMenu = drop.GetDropDown(drop.Locations);
            if (showSuggestions && newText == "" && dropMenu.value == "SUGGESTED")
            {
                List<LocationData> buildings = SearchSuggestedLocations();
                panels.UpdateBuildingSearchUI(buildings);
            }
            else
            {
                resultsHeader.style.display = DisplayStyle.Flex;
                LocationFilter filter = LocationFilter.None;

                //put in logic to shift between drop down options here
                switch (dropMenu.value)
                {
                    case "VISITED":
                        filter = LocationFilter.Visited;
                        break;
                    case "NOT VISITED":
                        filter = LocationFilter.NotVisited;
                        break;
                    case "FAVORITES":
                        filter = LocationFilter.Favorites;
                        break;
                    case "CLOSEST":
                        filter = LocationFilter.Closest;
                        break;
                    case "EVENTS":
                        filter = LocationFilter.Events;
                        break;
                    default:
                        break;
                }
                List<LocationData> buildings = locationManager.GetBuildingsFromSearch(newText, newText.Length > 0 && newText[0] == '~', filter);
                panels.UpdateBuildingSearchUI(buildings, !flag);
            }
        }

        private void HandleEventFilter(string newText, bool flag)
        {
            var dropMenu = drop.GetDropDown(drop.Events);

            resultsHeader.style.display = DisplayStyle.Flex;
            EventFilters filter = EventFilters.None;

            //put in logic to shift between drop down options here
            switch (dropMenu.value)
            {
                case "ASCEND DATE":
                    filter = EventFilters.DateClosest;
                    break;
                case "DESCEND DATE":
                    filter = EventFilters.DateFarthest;
                    break;
                case "DISTANCE":
                    filter = EventFilters.Distance;
                    break;
                case "UPCOMING":
                    filter = EventFilters.Upcoming;
                    break;
                default:
                    break;
            }
            List<EventData> events = eventManager.GetEventsFromSearch(newText, newText.Length > 0 && newText[0] == '~', filter);
            panels.UpdateEventSearchUI(events, !flag);
        }

        private void OnSearchValueChanged(ChangeEvent<string> evt)
        {
            if (shouldTriggerOnChangeEvent == false)
            {
                shouldTriggerOnChangeEvent = true;
                return;
            }

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
            if (tab.MyLastPressed == "location")
            {
                HandleLocationFilter(newText, flag);
            }
            else
            {
                resultsHeader.style.display = DisplayStyle.Flex;
                HandleEventFilter(newText, flag);
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

        public void OnChangeDropdownLocation(string choice)
        {
            shouldTriggerOnChangeEvent = false;
            //clear search view
            if (_searchField.value != "Search for locations" && _searchField.value != "Search for events") _searchField.value = "Search for locations";
            HandleLocationFilter("", false);
        }

        public void OnChangeDropDownEvent(string choice)
        {
            shouldTriggerOnChangeEvent = false;
            //clear search view
            if (_searchField.value != "Search for locations" && _searchField.value != "Search for events") _searchField.value = "Search for locations";
            HandleEventFilter("", false);
        }

        #region HelperFunctions
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
        #endregion
    }

}
