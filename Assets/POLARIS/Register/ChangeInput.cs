using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
// control navigations
using UnityEngine.EventSystems;

public class ChangeInput : MonoBehaviour
{
    EventSystem system;

    public Selectable firstInput;
    public Button enterButton;
    
    // Start is called before the first frame update
    void Start()
    {
        system = EventSystem.current;
        //firstInput.Select();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && Input.GetKey(KeyCode.LeftShift))
        {
            // get next selectable component from currently selected
            Selectable previous = system.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnUp();
            if (previous != null)
            {
                previous.Select();
            }
        }
        
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            // get next selectable component from currently selected
            Selectable next = system.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
            if (next != null)
            {
                next.Select();
            }
        }
        
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            enterButton.onClick.Invoke();
            Debug.Log("Button Pressed from Keyboard");
        }
    }
}
