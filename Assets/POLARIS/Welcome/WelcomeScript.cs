using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WelcomeScript : MonoBehaviour
{
    public void OnLoginButtonClick()
    {
        SceneManager.LoadScene("Login"); // Replace "LoginScene" with the actual name of your login scene.
    }
    
    public void OnRegisterButtonClick()
    {
        SceneManager.LoadScene("Register"); // Replace "LoginScene" with the actual name of your login scene.
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
