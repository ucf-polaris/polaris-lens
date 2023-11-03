using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class NestedScrollView : MonoBehaviour
{
    private ScrollView parent;
    private ScrollView child;
    // Start is called before the first frame update
    void Start()
    {
        UIDocument uidoc = GetComponent<UIDocument>();
        parent = uidoc.rootVisualElement.Q<ScrollView>("ParentScroll");
        child = uidoc.rootVisualElement.Q<ScrollView>("NestedScroll");

        parent.RegisterCallback<PointerDownEvent>(OnClick);
        child.RegisterCallback<PointerDownEvent>(OnClick);
    }

    void OnClick(PointerDownEvent evt)
    {
        if (evt.currentTarget == parent)
            Debug.Log("Parent");
        else if(evt.currentTarget == child)
            Debug.Log("Child");
    }
}
