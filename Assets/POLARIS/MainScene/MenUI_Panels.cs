using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using POLARIS.Managers;

namespace POLARIS.MainScene {
    public class MenUI_Panels : MonoBehaviour
    {
        public Geocoder geo;

        //data objects
        private List<Building> _buildingSearchList = new();
        private List<EventData> _eventSearchList = new();

        //misc. variables
        private string currentTab;
        private bool _waitingForResponse = false;

        //Managers
        private EventManager eventManager;

        //UI Toolkit elements
        private TextField _searchField;
        private Button _searchButton;
        private Button _clearButton;

        //UI Toolkit Objects
        [SerializeField]
        VisualTreeAsset LocationListEntryTemplate;

        [SerializeField]
        VisualTreeAsset EventListEntryTemplate;

        private ListController listController;
        // Start is called before the first frame update
        private void Start()
        {
            //Initialize variables

            eventManager = EventManager.getInstance();
            currentTab = ChangeTabImage._lastPressed;
            listController = new ListController();

            var uiDoc = GetComponent<UIDocument>();
            var rootVisual = uiDoc.rootVisualElement;
            listController.Initialize(rootVisual, EventListEntryTemplate, LocationListEntryTemplate, ListController.SwitchType.locations);

            //set up the search bar
            _searchField = rootVisual.Q<TextField>("SearchBar");
            _searchField.RegisterValueChangedCallback(OnSearchValueChanged);
            
            var tab = GetComponent<ChangeTabImage>();
            _searchField.RegisterCallback<FocusEvent>(tab.RaiseMenu);
            
            _searchField.selectAllOnFocus = true;
            SetPlaceholderText(_searchField, currentTab == "location" ? "Search for locations" : "Search for events");
        }

        // Update is called once per frame
        private void Update()
        {
            if (ChangeTabImage._lastPressed == currentTab) return;
            
            currentTab = ChangeTabImage._lastPressed;
            _searchField.value = "";
            ClearSearchResults(true);
            SetPlaceholderText(_searchField, ChangeTabImage._lastPressed == "location" ? "Search for locations" : "Search for events");
        }
        private void OnSearchValueChanged(ChangeEvent<string> evt)
        {
            string newText = evt.newValue;

            if (newText.EndsWith("\n"))
            {
                Deselect();
                _searchField.value = newText.TrimEnd('\n');
                return;
            }

            if (!string.IsNullOrWhiteSpace(newText))
            {
                if (currentTab == "location")
                {
                    List<Building> buildings = GetBuildingsFromSearch(newText, newText[0] == '~');
                    UpdateBuildingSearchUI(buildings);
                }
                else
                {
                    List<EventData> events = eventManager.GetEventsFromSearch(newText, newText[0] == '~');
                    UpdateEventSearchUI(events);
                }
            }
            else
            {
                ClearSearchResults(false);
            }
        }


        private bool FuzzyMatch(string source, string target, int tolerance)
        {
            return LongestCommonSubsequence(source, target).Length >= target.Length - tolerance;
        }

        private List<Building> GetBuildingsFromSearch(string query, bool fuzzySearch)
        {
            const int TOLERANCE = 1;

            List<Building> buildings = new List<Building>();
            foreach (Building building in Locations.LocationList)
            {
                if (building.BuildingName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (fuzzySearch && FuzzyMatch(building.BuildingName, query, TOLERANCE)))
                {
                    buildings.Add(building);
                    continue;
                }

                if (building.BuildingAllias == null) continue;
                foreach (string alias in building.BuildingAllias)
                {
                    if (alias.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (fuzzySearch && FuzzyMatch(alias, query, TOLERANCE)))
                    {
                        buildings.Add(building);
                        break;
                    }
                }
            }

            return buildings;
        }

        private void UpdateBuildingSearchUI(List<Building> buildings)
        {
            listController.Update(buildings);
        }

        private void UpdateEventSearchUI(List<EventData> events)
        {
            listController.Update(events);
        }

        private void OnBuildingSearchClick(Building selectedBuilding)
        {
            if (_waitingForResponse) return;
            _waitingForResponse = true;
            Deselect();
            Debug.Log(
                $"Name: {selectedBuilding.BuildingName ?? ""}\n" +
                $"Aliases: {((selectedBuilding.BuildingAllias != null) ? string.Join(", ", selectedBuilding.BuildingAllias) : "")}\n" +
                $"Abbreviations: {((selectedBuilding.BuildingAbbreviation != null) ? string.Join(", ", selectedBuilding.BuildingAbbreviation) : "")}\n" +
                $"Description: {selectedBuilding.BuildingDesc ?? ""}\n" +
                $"Longitude: {selectedBuilding.BuildingLong}\n" +
                $"Latitude: {selectedBuilding.BuildingLat}\n" +
                $"Address: {selectedBuilding.BuildingAddress ?? ""}\n" +
                $"Events: {((selectedBuilding.BuildingEvents != null) ? string.Join(", ", selectedBuilding.BuildingEvents) : "")}\n");
            geo.SetStuff(selectedBuilding.BuildingLong, selectedBuilding.BuildingLat, selectedBuilding.BuildingAddress);
            // ClearSearchResults();
            _waitingForResponse = false;
        }

        private void OnEventSearchClick(EventData selectedEvent)
        {
            if (_waitingForResponse) return;
            _waitingForResponse = true;
            Deselect();
            Debug.Log(
                $"Name: {selectedEvent.Name ?? ""}\n" +
                $"Description: {selectedEvent.Description ?? ""}\n" +
                $"Longitude: {selectedEvent.Location.BuildingLong}\n" +
                $"Latitude: {selectedEvent.Location.BuildingLat}\n" +
                $"Host: {selectedEvent.Host ?? ""}\n" +
                $"Start Time: {selectedEvent.DateTime}\n" +
                $"End Time: {selectedEvent.EndsOn}\n" +
                $"Image Path: {selectedEvent.Image ?? ""}\n" +
                $"Location: {selectedEvent.ListedLocation ?? ""}\n");
            // ClearSearchResults();
            _waitingForResponse = false;
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
                listController.Update(new List<EventData>());
            else if (listController.sw == type1)
                listController.Update(new List<Building>());
            
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

        private string LongestCommonSubsequence(string source, string target)
        {
            int[,] C = LongestCommonSubsequenceLengthTable(source, target);

            return Backtrack(C, source, target, source.Length, target.Length);
        }

        private int[,] LongestCommonSubsequenceLengthTable(string source, string target)
        {
            int[,] C = new int[source.Length + 1, target.Length + 1];

            for (int i = 0; i < source.Length + 1; i++) { C[i, 0] = 0; }
            for (int j = 0; j < target.Length + 1; j++) { C[0, j] = 0; }

            for (int i = 1; i < source.Length + 1; i++)
            {
                for (int j = 1; j < target.Length + 1; j++)
                {
                    if (source[i - 1].Equals(target[j - 1]))
                    {
                        C[i, j] = C[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        C[i, j] = Math.Max(C[i, j - 1], C[i - 1, j]);
                    }
                }
            }

            return C;
        }

        private string Backtrack(int[,] C, string source, string target, int i, int j)
        {
            if (i == 0 || j == 0)
            {
                return "";
            }
            else if (source[i - 1].Equals(target[j - 1]))
            {
                return Backtrack(C, source, target, i - 1, j - 1) + source[i - 1];
            }
            else
            {
                if (C[i, j - 1] > C[i - 1, j])
                {
                    return Backtrack(C, source, target, i, j - 1);
                }
                else
                {
                    return Backtrack(C, source, target, i - 1, j);
                }
            }
        }
    }
}
