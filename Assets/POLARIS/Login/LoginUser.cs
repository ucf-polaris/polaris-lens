using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro; 
using UnityEngine.SceneManagement;

public class LoginUser : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public string loginURL = "https://api.ucfpolaris.com/user/login";
    
    public void Login()
    {
        StartCoroutine(SendLoginRequest(emailInput.text, passwordInput.text));
    }
    
    
    
    IEnumerator SendLoginRequest(string email, string password)
    {
        password = Hashing.HashPassword(password);
        
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
            JObject jsonResponse = JObject.Parse(www.downloadHandler.text);
            Debug.Log("Response: " + jsonResponse);
            if (jsonResponse.ContainsKey("UserID"))
            {
                SceneManager.LoadScene("MainScene");
            }
            else Debug.Log("incorrect login");
        }
    }
    
}
