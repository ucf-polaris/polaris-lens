using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static System.Char;

namespace POLARIS.MainScene
{
    public class GetBuilding : MonoBehaviour
    {
        private Camera _mainCamera;
        private Label _uiDocLabel;

        private float lastTapTime = 0;
        private float doubleTapThreshold = 0.3f;

        private void Start()
        {
            _mainCamera = Camera.main;
            _uiDocLabel = gameObject.GetComponent<UIDocument>().rootVisualElement.Q<Label>("BuildingTopLabel");
        }
        
        private void Update()
        {
            // Double tap to view building name
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    if (Time.time - lastTapTime <= doubleTapThreshold)
                    {
                        lastTapTime = 0;
                        
                        var ray = _mainCamera.ScreenPointToRay(touch.position);

                        if (!Physics.Raycast(ray, out var hit)) return;

                        print("My object is clicked by mouse " + hit.transform.name);
                        _uiDocLabel.text = ToTitleCase(hit.transform.name[4..]);
                        StartCoroutine(ToggleLabelHeight());
                    }
                    else
                    {
                        lastTapTime = Time.time;
                    }
                }
            }
        }

        private IEnumerator ToggleLabelHeight()
        {
            _uiDocLabel.ToggleInClassList("RaisedLabel");
            _uiDocLabel.ToggleInClassList("BuildingTopLabel");
            yield return new WaitForSeconds(2.0f);
            _uiDocLabel.ToggleInClassList("RaisedLabel");
            _uiDocLabel.ToggleInClassList("BuildingTopLabel");
        }

        public static string ToTitleCase(string stringToConvert)
        {
            return new string(ToTitleCaseEnumerable(stringToConvert).ToArray());
        }
        private static IEnumerable<char> ToTitleCaseEnumerable(string stringToConvert)
        {
            var newWord = true;
            var prevLetterI = false;
            foreach (var c in stringToConvert)
            {
                if (newWord)
                {
                    yield return ToUpper(c);
                    newWord = false;
                    prevLetterI = c == 'I';
                }
                else
                {
                    if (prevLetterI)
                    {
                        if (c == 'I') yield return ToUpper(c);
                        else yield return ToLower(c);
                    }
                    else
                    {
                        yield return ToLower(c);
                    }
                }

                if (c == ' ')
                {
                    newWord = true;
                    prevLetterI = false;
                }
            }
        }
    }
    
    
}