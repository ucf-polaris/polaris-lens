using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


public class ChangeTabImage : MonoBehaviour
{
		private Button _calendarButton;
		private Button _locationButton;
		// Image _darkCalendarButton;
		// Image _darkLocationButton;

    // Start is called before the first frame update
    private void Start()
    {
        var uiDoc = gameObject.GetComponent<UIDocument>();
			_calendarButton = uiDoc.rootVisualElement.Q<Button>("Cal");
				_calendarButton.clickable.clicked += OnClickCalendar;

				_locationButton = uiDoc.rootVisualElement.Q<Button>("Loc");
				_locationButton.clickable.clicked += OnClickLocation;
    }

    private void OnClickCalendar()
		{
			_locationButton.style.backgroundImage = Resources.Load<Texture2D>("Polaris/UI tabs/locationdark");
			_calendarButton.style.backgroundImage = Resources.Load<Texture2D>("Polaris/UI tabs/calendar.png");

			// locationButton.GetComponent<Image>().image = darkLocationButton;
			// calendarButton.GetComponent<Image>().image = calendarButton;
			print("Hello calendar!");
		}

    private void OnClickLocation()
		{
			_locationButton.style.backgroundImage = Resources.Load<Texture2D>("Polaris/UI tabs/location.png");
			_calendarButton.style.backgroundImage = Resources.Load<Texture2D>("Polaris/UI tabs/calendardark.png");
			print("Hello location!");
		}
}
