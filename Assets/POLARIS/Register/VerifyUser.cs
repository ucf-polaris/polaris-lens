using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using TMPro;
using POLARIS.Managers;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

// [Serializable]
// public class AuthorizationTokenHeader
// {
//     public string token;
// }
public class VerificationCodeScript : NonManagerEndpoint
{
    public TMP_InputField verificationCodeInput;
    public string verifyCodeURL = "https://v21x6ajyg9.execute-api.us-east-2.amazonaws.com/dev/user/registrationcode";
    // string _token = RegistrationScript.AuthToken;
    public Animator parentAnimator;

    public void Verify()
    {
        if (CurrentState == EndpointState.NotStarted)
        {
            CurrentState = EndpointState.InProgress;
            StartCoroutine(SendVerificationRequest(verificationCodeInput.text));
        }
    }

    IEnumerator SendVerificationRequest(string verifCode)
    {
        
        // Create an instance of the AuthorizationTokenHeader class
        // AuthorizationTokenHeader authTokenHeader = new AuthorizationTokenHeader();

        // authTokenHeader.token = PlayerPrefs.GetString("AuthToken");
        
        // Retrieve the authToken from PlayerPrefs
        string authToken = instance.codeData.Token;

        // Use authToken for verification or any other purpose
        Debug.Log("Received Auth Token: " + authToken);
        // verification code sent in body of request
        JObject code =
            new JObject(
                new JProperty("code", verifCode)
            );

        var www = UnityWebRequest.Post(verifyCodeURL, code.ToString(), "application/json");
        //set request header
        // Debug.Log("heres the header!!" + jsonHeader);
        
        // Create the JSON string for the header
        // string jsonHeader = "{\"token\":\"" + authToken + "\"}";
        // Debug.Log("heres the header!!" + jsonHeader);
        // www.SetRequestHeader("authorizationToken",jsonHeader);
        www.SetRequestHeader("authorizationToken", "{\"token\":\"" + authToken + "\"}");
        // yield return www.SendWebRequest()
        yield return www.SendWebRequest();
        
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
            CurrentState = EndpointState.Failed;
        }
        else if (www.downloadHandler.text.Contains("ERROR"))
        {
            Debug.Log(www.downloadHandler.text);
            CurrentState = EndpointState.Failed;
        }
        else
        {
            Debug.Log("Form upload complete!");
            Debug.Log("Status Code: " + www.responseCode);
            Debug.Log(www.result);
            Debug.Log("Response: " + www.downloadHandler.text);
            
            CurrentState = EndpointState.Succeed;
            StartCoroutine(ToLogin());
        }
    }

    IEnumerator ToLogin()
    {
        yield return new WaitUntil(() => ani.GetInteger("State") == 0);
        if(EventSystem.current != null) EventSystem.current.enabled = false;
        TransitionManager.getInstance().StartPlay("Login", Transitions.FadeIn, Transitions.FadeOut, 0.5f, 0f, 0.5f, 0f);
    }
}