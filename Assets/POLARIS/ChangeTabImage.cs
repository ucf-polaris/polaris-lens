using UnityEngine;
using UnityEngine.UIElements;
using POLARIS.MainScene;
using System;

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
	private Label _header;
	private string _myLastPressed = "location";
	public string MyLastPressed { get => _myLastPressed; set { _myLastPressed = value; OnEvent(); } }

	public static bool _menuOpen = false;
	public static bool justRaised = false;

	private MenUI_Panels panelFuncts;
	public event EventHandler ChangeTab;

	// Start is called before the first frame update
    private void Start()
    {
		panelFuncts = gameObject.GetComponent<MenUI_Panels>();
        var uiDoc = gameObject.GetComponent<UIDocument>();

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
		_header = uiDoc.rootVisualElement.Q<Label>("Identifier");
		_arrow.RegisterCallback<ClickEvent>(OnClickArrow);
    }
	private void OnEvent()
	{
		Debug.Log("yes");
		if (ChangeTab != null)
		{
			ChangeTab(this, EventArgs.Empty);
		}
	}

	private void OnClickCalendar()
	{
		_header.text = "Events";
		_locationButton.style.backgroundImage = _locationDark;
		_calendarButton.style.backgroundImage = _calendar;
		print("Hello calendar!");
		MoveUI("calendar");
	}

    private void OnClickLocation()
	{
		_header.text = "Locations";
		_locationButton.style.backgroundImage = _location;
		_calendarButton.style.backgroundImage = _calendarDark;
		print("Hello location!");
		MoveUI("location");
	}

    private void OnClickArrow(ClickEvent evt)
    {
	    justRaised = !_menuOpen;
	    _menuOpen = !_menuOpen;
	    _spacer.style.height = Length.Percent(_menuOpen ?  15 : 82.5f);
	    _arrow.style.rotate = new Rotate(_menuOpen ? 180 : 0);
		panelFuncts.CollapseBothExtendedViews();
		panelFuncts.ReturnGoToTop();
	}

    private void MoveUI(string pressed)
    {
	    justRaised = (!_menuOpen && !(_menuOpen && MyLastPressed == pressed)); 
	    _menuOpen = !(_menuOpen && MyLastPressed == pressed);
		MyLastPressed = pressed;

	    _spacer.style.height = Length.Percent(_menuOpen ?  15 : 82.5f);
	    _arrow.style.rotate = new Rotate(_menuOpen ? 180 : 0);
		panelFuncts.CollapseBothExtendedViews();
		panelFuncts.ReturnGoToTop();
	}

    public void RaiseMenu(FocusEvent e)
    {
	    justRaised = true;
	    _menuOpen = true;
	    _spacer.style.height = Length.Percent(15);
	    _arrow.style.rotate = new Rotate(180);
	}

	public void CollapseMenu(FocusEvent e)
	{
		justRaised = false;
		_menuOpen = false;
		_spacer.style.height = Length.Percent(82.5f);
		_arrow.style.rotate = new Rotate(0);
		panelFuncts.CollapseBothExtendedViews();
		panelFuncts.ReturnGoToTop();
	}
}


