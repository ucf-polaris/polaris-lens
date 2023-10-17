using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro; 
public class LoginUser : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public string loginURL = "https://qgfl17av2f.execute-api.us-east-2.amazonaws.com/Stage/user/login";
    
    public void Login()
    {
        StartCoroutine(SendLoginRequest(emailInput.text, passwordInput.text));
    }
    
    IEnumerator SendLoginRequest(string email, string password)
    {
        JObject payload =
            new JObject(
                new JProperty("email", email),
                new JProperty("password", password)
            );
        
        UnityWebRequest www = UnityWebRequest.Post(loginURL, payload.ToString(), "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Form upload complete!");
            Debug.Log("Status Code: " + www.responseCode);
            Debug.Log(www.result);
            Debug.Log("Response: " + www.downloadHandler.text);
        }
    }
    
    
    
}
