using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using UnityEngine.SceneManagement;

public class DebugManager : MonoBehaviour
{
    private static DebugManager instance;
    private static int counter = 0;
    private IDictionary<string, string> Message = new Dictionary<string, string>();
    public IDictionary<string, Action> Buttons = new Dictionary<string, Action>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
            Debug.Log("Debug Manager Created");
            SceneManager.activeSceneChanged += ChangedActiveScene;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void ChangedActiveScene(Scene current, Scene next)
    {
        string currentName = next.name;
        Debug.Log(currentName);

        if (currentName == null)
        {
            // Scene1 has been removed
            currentName = "Replaced";
        }

        DebugMenu menu = gameObject.GetComponent<DebugMenu>();
        if (menu != null) menu.ClearButtons();
        ClearButton();
        Message.Clear();
    }

    private void OnDisable()
    {
        if(instance == this) instance = null;
        SceneManager.activeSceneChanged -= ChangedActiveScene;
    }

    public static DebugManager GetInstance()
    {
        return instance;
    }

    public void AddToMessage(string title, string info)
    {
        Message[title] = info;
    }

    public void AddToButton(string title, Action info)
    {
        Buttons[title] = info;
    }

    public void RemoveFromMessage(string title)
    {
        Message.Remove(title);
    }

    public void RemoveFromButton(string title)
    {
        Buttons.Remove(title);
    }

    public void ClearButton()
    {
        Buttons.Clear();
    }

    public string GetMessage()
    {
        string ret = "";
        foreach(string key in Message.Keys)
        {
            ret += key + ": " + Message[key] + "\n";
        }
        return ret;
    }

    public string AssignName(string s)
    {
        int n = counter;
        counter++;
        return s + " " + n.ToString();
    }

}
