using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using POLARIS.Managers;
using POLARIS.MainScene;
using UnityEngine.UIElements;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

public class Login_Loading : Welcome_Loading
{
    VisualElement backdrop;
    BaseManager.CallStatus loadStatus = BaseManager.CallStatus.NotStarted;
    public static Login_Loading instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
        DontDestroyOnLoad(gameObject);
        userManager = UserManager.getInstance();
        var UiDoc = GetComponent<UIDocument>();
        UiDoc.enabled = true;
        fullElement = UiDoc.rootVisualElement.Q<VisualElement>("Background");
        backdrop = UiDoc.rootVisualElement.Q<VisualElement>("Backdrop");

        window = new LoadingWindow(UiDoc.rootVisualElement.Q<VisualElement>("LoadingScreen"), false);
        window.errorLabel.text = "Logging In...";
        Initialize();
        userManager.GetSucceed += OnFoundSession;
        fullElement.style.display = DisplayStyle.None;
    }

    void Initialize()
    {
        window.loadingLabel.style.visibility = Visibility.Hidden;
        window.errorLabel.style.opacity = 0;
        window.errorLabel.style.visibility = Visibility.Visible;
        window.errorLabel.style.translate = new Translate(0, Length.Percent(-50));
        backdrop.RemoveFromClassList("navyColor");
        backdrop.AddToClassList("magentaColor");
    }

    new private void OnDisable()
    {
        if(instance == this) instance = null;
    }

    public void TurnOn()
    {
        fullElement.style.display = DisplayStyle.Flex;
        StartCoroutine(StartLoading());
    }

    private IEnumerator StartLoading()
    {
        loadStatus = BaseManager.CallStatus.InProgress;
        //fade in the text with UI Toolkit
        window.errorLabel.style.opacity = 100;
        window.errorLabel.style.translate = new Translate(0, 0);
        yield return new WaitForSeconds(0.5f);
        //start animation loop
        StartLoop(window);
        yield return new WaitForSeconds(1f);

        //start loading
        AsyncOperation async = SceneManager.LoadSceneAsync("MainScene");
        async.allowSceneActivation = false;

        while (!async.isDone)
        {
            //if progress is almost done
            if (async.progress >= 0.9f)
            {
                //play the disappear animation
                async.allowSceneActivation = true;
                yield return new WaitForSeconds(1f);
            }
            yield return null;
        }

        //play disappear animation
        loadStatus = BaseManager.CallStatus.Succeeded;
        yield return StartCoroutine(DisappearAnimation(window));
        //make everything fade out
        fullElement.style.opacity = 0;
        yield return new WaitForSeconds(0.75f);
        Destroy(gameObject);

        yield return async;
    }

    protected IEnumerator ErrorText(LoadingWindow window)
    {
        var msg = "Logging In";
        window.errorLabel.text = msg;
        string[] names = { msg, msg + ".", msg + "..", msg + "..." };
        int index = 0;
        while (CheckFunction(new BaseManager.CallStatus[] { BaseManager.CallStatus.Failed, BaseManager.CallStatus.NotStarted, BaseManager.CallStatus.InProgress }, window))
        {
            yield return new WaitForSeconds(0.5f);
            window.errorLabel.text = names[index];
            index += 1;
            index %= names.Length;
        }

        window.loadingText = null;
    }

    override protected void StartLoop(LoadingWindow window)
    {
        //if already done, don't load this
        if (CheckFunction(new BaseManager.CallStatus[] { BaseManager.CallStatus.Succeeded }, window))
        {
            OnStop(window);
            return;
        }

        //startup text load
        if (window.loadingText != null) StopCoroutine(window.loadingText);
        window.loadingText = ErrorText(window);
        StartCoroutine(window.loadingText);

        if (window.loopingAnimation != null) StopCoroutine(window.loopingAnimation);
        window.loopingAnimation = LoopingAnimation(window);
        StartCoroutine(window.loopingAnimation);
    }

    override protected IEnumerator showErrorMessage(LoadingWindow window)
    {
        yield break;
    }

    override protected bool CheckFunction(BaseManager.CallStatus[] acceptList, LoadingWindow window)
    {
        return acceptList.Contains(loadStatus);
    }
}
