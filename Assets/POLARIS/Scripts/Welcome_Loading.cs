using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using POLARIS.MainScene;
using POLARIS.Managers;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Linq;
using System;

public class Welcome_Loading : LoadingBase
{
    public static bool firstTime = true;
    public LoadingWindow window;
    protected UserManager userManager;
    protected VisualElement fullElement;
    public BaseManager.CallStatus BuildingsLoaded = BaseManager.CallStatus.NotStarted;
    string msg = "Loading Buildings";

    void Start()
    {
        userManager = UserManager.getInstance();
        var UiDoc = GetComponent<UIDocument>();
        UiDoc.enabled = true;
        fullElement = UiDoc.rootVisualElement.Q<VisualElement>("Background");
        LoadingMessage = "Verifying Session";

        window = new LoadingWindow(UiDoc.rootVisualElement.Q<VisualElement>("LoadingScreen"), false);
        userManager.GetSucceed += OnFoundSession;

        Debug.Log("in welcome");

        if (!firstTime)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
            firstTime = false;
            StartLoop(window);
        }
    }

    protected void OnDisable()
    {
        userManager.GetSucceed -= OnFoundSession;
    }

    protected void OnFoundSession(object o, EventArgs e)
    {
        ShowFeedbackMessage(window, "Session Found.");
        StartCoroutine(FoundEvent(window));
    }

    override protected void OnStop(LoadingWindow window)
    {
        StopLoop(window);
    }

    //if the cached user data is valid
    private IEnumerator FoundEvent(LoadingWindow window)
    {
        //load session
        AsyncOperation async = SceneManager.LoadSceneAsync("MainScene");

        // this line prevents the scene from instant activation
        async.allowSceneActivation = false;

        yield return new WaitForSeconds(1f);
        ShowFeedbackMessage(window, "Loading");

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

        //show loading buildings
        ShowFeedbackMessage(window, "Loading Buildings");
        if (window.loadingText != null) StopCoroutine(window.loadingText);
        window.loadingText = errorTextWelc(window);
        StartCoroutine(window.loadingText);

        //wait till buildings are loaded
        yield return new WaitUntil(() => BuildingsLoaded == BaseManager.CallStatus.Succeeded || BuildingsLoaded == BaseManager.CallStatus.Failed);

        //wait for the disappear animation to finish
        yield return StartCoroutine(DisappearAnimation(window));

        //make everything fade out
        fullElement.style.opacity = 0;
        yield return new WaitForSeconds(0.75f);
        Destroy(gameObject);

        yield return async;
    }

    protected IEnumerator errorTextWelc(LoadingWindow window)
    {
        window.errorLabel.text = msg;
        string[] names = { msg, msg + ".", msg + "..", msg + "..." };
        int index = 0;
        while (CheckFunction(new BaseManager.CallStatus[] { BaseManager.CallStatus.Failed, BaseManager.CallStatus.NotStarted, BaseManager.CallStatus.InProgress }, window, true))
        {
            yield return new WaitForSeconds(0.5f);
            window.errorLabel.text = names[index];
            index += 1;
            index %= names.Length;
        }

        window.loadingText = null;
    }

    private void ShowFeedbackMessage(LoadingWindow window, string msg)
    {
        window.loadingLabel.style.visibility = Visibility.Hidden;
        window.errorLabel.style.visibility = Visibility.Visible;
        window.errorLabel.text = msg;
    }

    //play animation when screen leaves then disable screen
    override protected IEnumerator showErrorMessage(LoadingWindow window)
    {
        //make feedback text appear
        ShowFeedbackMessage(window, "Redirecting...");
        yield return StartCoroutine(DisappearAnimation(window));

        //make everything fade out
        fullElement.style.opacity = 0;
        yield return new WaitForSeconds(0.75f);

        Destroy(gameObject);
        yield return null;
    }
    //play animation to cover screen
    protected IEnumerator DisappearAnimation(LoadingWindow window)
    {
        StopLoop(window);

        //make load disappear
        yield return new WaitForSeconds(0.5f);
        window.errorLabel.style.translate = new Translate(0, Length.Percent(50));
        window.errorLabel.style.opacity = 0;
        yield return new WaitForSeconds(0.5f);

        //make logo disappear
        window.animationContainer.style.scale = new Scale(new Vector2(0, 0));
        yield return new WaitForSeconds(1f);

        //make background bigger
        window.container.style.scale = new Scale(new Vector2(1.5f, 5.95f));
        yield return new WaitForSeconds(0.75f);
    }

    override protected bool CheckFunction(BaseManager.CallStatus[] acceptList, LoadingWindow window, bool or=false)
    {
        BaseManager.CallStatus status = userManager.CheckPermanenceStatus;
        if (acceptList.Contains(status) && acceptList.Contains(BuildingsLoaded) && !or) return true;
        else if ((acceptList.Contains(status) || acceptList.Contains(BuildingsLoaded)) && or) return true;
        return false;
    }
}
