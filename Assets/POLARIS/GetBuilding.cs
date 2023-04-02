using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static System.Char;

namespace POLARIS
{
    public class GetBuilding : MonoBehaviour
    {
        private Camera _mainCamera;
        private Label _uiDocLabel;

        private void Start()
        {
            _mainCamera = Camera.main;
            _uiDocLabel = gameObject.GetComponent<UIDocument>().rootVisualElement.Q<Label>("BuildingTopLabel");
        }
        
        private void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

            if (!Physics.Raycast(ray, out var hit)) return;

            print("My object is clicked by mouse " + hit.transform.name);
            _uiDocLabel.text = ToTitleCase(hit.transform.name[4..]);
            StartCoroutine(ToggleLabelHeight());
        }

        private IEnumerator ToggleLabelHeight()
        {
            _uiDocLabel.ToggleInClassList("RaisedLabel");
            _uiDocLabel.ToggleInClassList("BuildingTopLabel");
            yield return new WaitForSeconds(2.0f);
            _uiDocLabel.ToggleInClassList("RaisedLabel");
            _uiDocLabel.ToggleInClassList("BuildingTopLabel");
        }

        private static string ToTitleCase(string stringToConvert)
        {
            return new string(ToTitleCaseEnumerable(stringToConvert).ToArray());
        }
        private static IEnumerable<char> ToTitleCaseEnumerable(string stringToConvert)
        {
            var newWord = true;
            foreach (var c in stringToConvert)
            {
                if(newWord) { yield return ToUpper(c); newWord = false; }
                else yield return ToLower(c);
                if(c==' ') newWord = true;
            }
        }
    }
    
    
}