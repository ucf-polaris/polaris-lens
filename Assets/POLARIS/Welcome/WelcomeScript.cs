using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WelcomeScript : MonoBehaviour
{
    public void OnLoginButtonClick()
    {
        SceneManager.LoadScene("Login"); 
    }
    
    public void OnRegisterButtonClick()
    {
        SceneManager.LoadScene("Register"); 
    }

    public void OnForgotPasswordClick()
    {
        SceneManager.LoadScene("ForgotPWCode");
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
