using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


public class ChangeTabImage : MonoBehaviour
{
		public Button calendarButton;
		public Button locationButton;
		Image darkCalendarButton;
		Image darkLocationButton;

    // Start is called before the first frame update
    void Start()
    {
        var uiDoc = gameObject.GetComponent<UIDocument>();
			calendarButton = uiDoc.rootVisualElement.Q<Button>("Cal");
				calendarButton.clickable.clicked += onClickCalendar;

				locationButton = uiDoc.rootVisualElement.Q<Button>("Loc");
				locationButton.clickable.clicked += onClickLocation;
    }

		void onClickCalendar()
		{
			locationButton.style.backgroundImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/POLARIS/UI tabs/locationdark.png");
			calendarButton.style.backgroundImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/POLARIS/UI tabs/calendar.png");

			// locationButton.GetComponent<Image>().image = darkLocationButton;
			// calendarButton.GetComponent<Image>().image = calendarButton;
			print("Hello calendar!");
		}

		void onClickLocation()
		{
			locationButton.style.backgroundImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/POLARIS/UI tabs/location.png");
			calendarButton.style.backgroundImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/POLARIS/UI tabs/calendardark.png");
			print("Hello location!");
		}
}
