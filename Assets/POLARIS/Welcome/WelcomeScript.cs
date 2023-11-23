using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using POLARIS.Managers;
using System;

public class WelcomeScript : MonoBehaviour
{
    TransitionManager transitionManager;

    public void OnLoginButtonClick()
    {
        //if (transitionManager != null) transitionManager.StartPlay("Login", In.transition, Out.transition, In.Duration, In.AfterDelay, Out.Duration, Out.AfterDelay);
        if (transitionManager != null) transitionManager.StartPlay("Login", Transitions.FromBottomIn, Transitions.FadeOut, 0.4f, 0f, 0.5f, 0f);
    }
    
    public void OnRegisterButtonClick()
    {
        //if (transitionManager != null) transitionManager.StartPlay("Register", In.transition, Out.transition, In.Duration, In.AfterDelay, Out.Duration, Out.AfterDelay);
        if (transitionManager != null) transitionManager.StartPlay("Register", Transitions.FromTopIn, Transitions.FadeOut, 0.4f, 0f, 0.5f, 0f);
    }

    public void OnForgotPasswordClick()
    {
        if (transitionManager != null) transitionManager.StartPlay("ForgotPWCode", Transitions.FadeIn, Transitions.FadeOut, 0.5f, 0f, 0.5f, 0f);
    }
    
    // Start is called before the first frame update
    void Start()
    {
        transitionManager = TransitionManager.getInstance();
    }
}
