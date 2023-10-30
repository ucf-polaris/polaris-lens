using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class UiHelper
{
    public static void DelayAddToClassList(VisualElement ui, Dictionary<string, int> classMap)
    {
        foreach(var item in classMap)
        {
            ui.schedule.Execute(() => ui.AddToClassList(item.Key)).StartingIn(item.Value);
        }
    }

    public static void DelayRemoveFromClassList(VisualElement ui, Dictionary<string, int> classMap)
    {
        foreach (var item in classMap)
        {
            ui.RemoveFromClassList(item.Key);
        }
    }}
