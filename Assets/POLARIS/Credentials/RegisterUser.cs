using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TMPro;

public class RegistrationScript : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public string registrationURL = "https://8vnj8pkog4.execute-api.us-east-2.amazonaws.com/dev2/register";

    public void Register()
    {
        StartCoroutine(SendRegistrationRequest(usernameInput.text, passwordInput.text));
    }

    IEnumerator SendRegistrationRequest(string username, string password)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

        JObject payload =
            new JObject(
                new JProperty("username", username),
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
        }
    }
}