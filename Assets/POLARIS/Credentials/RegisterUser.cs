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
    public string registrationURL = "https://ea01n0gzv0.execute-api.us-east-2.amazonaws.com/Stage/user/register";

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
        }
    }
}