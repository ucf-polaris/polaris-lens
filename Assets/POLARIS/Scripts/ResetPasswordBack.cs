using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using POLARIS.Managers;
using UnityEngine.SceneManagement;

public class ResetPasswordBack : MonoBehaviour
{
    public void GoBack()
    {
        Debug.Log("yes");
        var nextScene = string.IsNullOrEmpty(UserManager.getInstance().data.CurrScene) ? "Login" : UserManager.getInstance().data.CurrScene;
        SceneManager.LoadScene(nextScene);
    }
}
