using UnityEngine;
using UnityEngine.UIElements;

public class ChangeTabImage : MonoBehaviour
{
	private Button _calendarButton;
	private Button _locationButton;
	
	private Texture2D _location;
	private Texture2D _locationDark;
	private Texture2D _calendar;
	private Texture2D _calendarDark;

	// Start is called before the first frame update
    private void Start()
    {
        var uiDoc = gameObject.GetComponent<UIDocument>();
        
		_calendarButton = uiDoc.rootVisualElement.Q<Button>("Cal");
		_calendarButton.clickable.clicked += OnClickCalendar;

		_locationButton = uiDoc.rootVisualElement.Q<Button>("Loc");
		_locationButton.clickable.clicked += OnClickLocation;
		
		_location = Resources.Load<Texture2D>("Polaris/UI tabs/location");
		_locationDark = Resources.Load<Texture2D>("Polaris/UI tabs/locationdark");
		_calendar = Resources.Load<Texture2D>("Polaris/UI tabs/calendar");
		_calendarDark = Resources.Load<Texture2D>("Polaris/UI tabs/calendardark");
    }

    private void OnClickCalendar()
		{
			_locationButton.style.backgroundImage = _locationDark;
			_calendarButton.style.backgroundImage = _calendar;
			print("Hello calendar!");
		}

    private void OnClickLocation()
		{
			_locationButton.style.backgroundImage = _location;
			_calendarButton.style.backgroundImage = _calendarDark;
			print("Hello location!");
		}
}
