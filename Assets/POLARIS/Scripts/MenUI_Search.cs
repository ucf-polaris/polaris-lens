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

        //UI Toolkit elements
        private TextField _searchField;
        private Label header;

        // Start is called before the first frame update
        void Start()
        {
            //manager managing
            userManager = UserManager.getInstance();
            eventManager = EventManager.getInstance();
            locationManager = LocationManager.getInstance();

            panels = GetComponent<MenUI_Panels>();
            tab = GetComponent<ChangeTabImage>();

            //change this to event system
            //currentTab = ChangeTabImage._lastPressed;
            tab.ChangeTab += OnChangeTab;

            //ui document information
            var uiDoc = GetComponent<UIDocument>();
            var rootVisual = uiDoc.rootVisualElement;
            header = rootVisual.Q<Label>("Identifier");

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
                OnChangeDropdown(MenUI_Dropdown.currentChoice);
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
                panels.UpdateEventSearchUI(events);
            }
        }

        private void HandleLocationFilter(string newText, bool flag)
        {
            if (showSuggestions && newText == "" && MenUI_Dropdown.currentChoice == "SUGGESTED")
            {
                header.text = "Suggested Locations";
                List<LocationData> buildings = new List<LocationData>();
                foreach (string buildingName in userManager.data.Suggested.Split("~"))
                {
                    LocationData building = GetBuildingFromName(buildingName);
                    if (building != null) buildings.Add(building);
                }
                panels.UpdateBuildingSearchUI(buildings);
            }
            else
            {
                LocationManager.LocationFilter filter = LocationManager.LocationFilter.None;
                //put in logic to shift between drop down options here
                switch (MenUI_Dropdown.currentChoice)
                {
                    case "VISITED":
                        header.text = "Visited Locations";
                        filter = LocationManager.LocationFilter.Visited;
                        break;
                    case "NOT VISITED":
                        header.text = "Not Visited Locations";
                        filter = LocationManager.LocationFilter.NotVisited;
                        break;
                    case "FAVORITES":
                        header.text = "Favorited Locations";
                        filter = LocationManager.LocationFilter.Favorites;
                        break;
                    case "CLOSEST":
                        header.text = "Closest Locations";
                        filter = LocationManager.LocationFilter.Closest;
                        break;
                    case "EVENTS":
                        header.text = "Most Events per Locations";
                        filter = LocationManager.LocationFilter.Events;
                        break;
                    default:
                        header.text = "Locations";
                        break;
                }
                List<LocationData> buildings = locationManager.GetBuildingsFromSearch(newText, newText.Length > 0 && newText[0] == '~', filter);
                panels.UpdateBuildingSearchUI(buildings, !flag);
            }
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
                if (showSuggestions && newText == "")
                {
                    header.text = "Suggested Events";
                }
                else
                {
                    header.text = "Events";
                }
                List<EventData> events = eventManager.GetEventsFromSearch(newText, newText.Length > 0 && newText[0] == '~');
                panels.UpdateEventSearchUI(events, !flag);
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

        public void OnChangeDropdown(string choice)
        {
            shouldTriggerOnChangeEvent = false;
            //clear search view
            if (_searchField.value != "Search for locations" && _searchField.value != "Search for events") _searchField.value = "Search for locations";
            HandleLocationFilter("", false);
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
