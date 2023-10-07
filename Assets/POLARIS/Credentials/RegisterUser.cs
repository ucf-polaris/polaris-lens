using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;

public class RegistrationScript : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public string registrationURL = "https://api.ucfpolaris.com/user/register";
    // Public variable to store the token
    public static string AuthToken;
    public void Register()
    {
        StartCoroutine(SendRegistrationRequest(emailInput.text, passwordInput.text));
    }

    IEnumerator SendRegistrationRequest(string email, string password)
    {
        JObject payload =
            new JObject(
                new JProperty("email", email),
                new JProperty("password", password)
            );
            
        
        

        UnityWebRequest www = UnityWebRequest.Post(registrationURL, payload.ToString(), "application/json");

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
            JObject jsonResponse = JObject.Parse(www.downloadHandler.text);
            AuthToken = jsonResponse["token"].Value<string>();
            
            // Save the authToken to PlayerPrefs for later use
            PlayerPrefs.SetString("AuthToken", AuthToken);
            
            
            Debug.Log("token saved: " + AuthToken);
        }
    }
}