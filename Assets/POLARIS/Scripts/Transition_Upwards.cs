using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Transition_Upwards : TransitionBase
{
    VisualElement body;
    public override void Initialize(string obj, params string[] listParams)
    {
        if (passoverObject != null) return;
        passoverObject = Instantiate(passoverTemplate, gameObject.transform);
        var UIDoc = passoverObject.GetComponent<UIDocument>();
        body = UIDoc.rootVisualElement.Q<VisualElement>("body");
        if (listParams.Length > 0) TransitionTo = listParams[0];
    }

    protected override IEnumerator StartTransition()
    {
        yield return new WaitForSeconds(0.1f);
        body.style.translate = new Translate(0, 0);
        yield return new WaitForSeconds(1f);
    }
    protected override IEnumerator EndTransition()
    {
        body.style.opacity = 0;
        yield return new WaitForSeconds(1f);
    }

}
