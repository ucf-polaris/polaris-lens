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

	private VisualElement _spacer;
	private VisualElement _arrow;
	private string _lastPressed;
	private bool _menuOpen;

	// Start is called before the first frame update
    private void Start()
    {
        var uiDoc = gameObject.GetComponent<UIDocument>();
		var test = uiDoc.rootVisualElement.Q<ScrollView>("ScrollView");

		_calendarButton = uiDoc.rootVisualElement.Q<Button>("Cal");
		_calendarButton.clickable.clicked += OnClickCalendar;

		_locationButton = uiDoc.rootVisualElement.Q<Button>("Loc");
		_locationButton.clickable.clicked += OnClickLocation;
		
		_location = Resources.Load<Texture2D>("Polaris/UI tabs/locationlight");
		_locationDark = Resources.Load<Texture2D>("Polaris/UI tabs/locationdark");
		_calendar = Resources.Load<Texture2D>("Polaris/UI tabs/calendarlight");
		_calendarDark = Resources.Load<Texture2D>("Polaris/UI tabs/calendardark");
		
		_spacer = uiDoc.rootVisualElement.Q<VisualElement>("Spacer");
		_arrow = uiDoc.rootVisualElement.Q<VisualElement>("Arrow");
		_arrow.RegisterCallback<ClickEvent>(OnClickArrow);
    }

    private void OnClickCalendar()
	{
		_locationButton.style.backgroundImage = _locationDark;
		_calendarButton.style.backgroundImage = _calendar;
		print("Hello calendar!");
		MoveUI("calendar");
	}

    private void OnClickLocation()
	{
		_locationButton.style.backgroundImage = _location;
		_calendarButton.style.backgroundImage = _calendarDark;
		print("Hello location!");
		MoveUI("location");
	}

    private void OnClickArrow(ClickEvent evt)
    {
	    _menuOpen = !_menuOpen;
	    _spacer.style.height = Length.Percent(_menuOpen ?  15 : 80);
	    _arrow.style.rotate = new Rotate(_menuOpen ? 90 : 270);
    }

    private void MoveUI(string pressed)
    {
	    _menuOpen = !(_menuOpen && _lastPressed == pressed);
	    _lastPressed = pressed;

	    _spacer.style.height = Length.Percent(_menuOpen ?  15 : 80);
	    _arrow.style.rotate = new Rotate(_menuOpen ? 90 : 270);
    }
}
