using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using POLARIS.Managers;
using UnityEngine.SceneManagement;

public class ResetPasswordBack : MonoBehaviour
{
    TransitionManager TransitionInstance;
    private void Start()
    {
        TransitionInstance = TransitionManager.getInstance();
    }
    public void GoBack()
    {
        var nextScene = string.IsNullOrEmpty(UserManager.getInstance().data.CurrScene) ? "Login" : UserManager.getInstance().data.CurrScene;
        if(TransitionInstance != null) TransitionInstance.StartPlay(nextScene, Transitions.FromTopIn, Transitions.FromBottomOut, 0.4f, 0f, 0.5f, 0f);
    }
}
