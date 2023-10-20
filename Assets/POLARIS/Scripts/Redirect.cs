using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Redirect : MonoBehaviour
{
    public void SceneTransition(string nextScene)
    {
        SceneManager.LoadScene(nextScene);
    }
}
