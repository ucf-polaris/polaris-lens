using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using TMPro;


// [Serializable]
// public class AuthorizationTokenHeader
// {
//     public string token;
// }
public class VerificationCodeScript : MonoBehaviour
{
    public TMP_InputField verificationCodeInput;
    public string verifyCodeURL = "https://v21x6ajyg9.execute-api.us-east-2.amazonaws.com/dev/user/update";
    // string _token = RegistrationScript.AuthToken;
    public void Verify()
    {
        StartCoroutine(SendVerificationRequest(verificationCodeInput.text));
    }

    IEnumerator SendVerificationRequest(string verifCode)
    {
        
        // Create an instance of the AuthorizationTokenHeader class
        // AuthorizationTokenHeader authTokenHeader = new AuthorizationTokenHeader();

        // authTokenHeader.token = PlayerPrefs.GetString("AuthToken");
        
        // Retrieve the authToken from PlayerPrefs
        string authToken = PlayerPrefs.GetString("AuthToken");

        // Use authToken for verification or any other purpose
        Debug.Log("Received Auth Token: " + authToken);
        // Debug.Log("Received Auth Token: " + authTokenHeader.token);

        
        // JObject payload =
        //     new JObject(
        //         new JProperty("token", authToken) 
        //     );
        // JObject payload =
        //     new JObject(
        //         new JProperty("token", authToken)
        //     );
        // Debug.Log("payload!!! here!! its the authtoken!!" + payload.ToString());
        //given from register response token : blahblhah
        
        // verification code sent in body of request
        JObject code =
            new JObject(
                new JProperty("code", verifCode)
            );
        
        
        
        
        
        // Serialize the JObject to a JSON string
        string jsonBody = code.ToString(); 
        // string jsonHeader = JsonUtility.ToJson(authTokenHeader);
        
        // Convert the JSON string to bytes
        byte[] bodyData = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        // byte[] headerData = System.Text.Encoding.UTF8.GetBytes(jsonHeader);
        
        //make new unity web request object
        UnityWebRequest www = new UnityWebRequest(verifyCodeURL, "POST");
        
        //set request header
        // Debug.Log("heres the header!!" + jsonHeader);
        
        // Create the JSON string for the header
        string jsonHeader = "{\"token\":\"" + authToken + "\"}";
        Debug.Log("heres the header!!" + jsonHeader);
        www.SetRequestHeader("authorizationToken",jsonHeader);
        
        
        // Set the request body
        www.uploadHandler = new UploadHandlerRaw(bodyData);
        
        
        // UnityWebRequest www = UnityWebRequest.Post(verifyCodeURL,code.ToString(),  "application/json");

        
        //UnityWebRequest www = UnityWebRequest.PostWwwForm(verifyCodeURL,code.ToString(),  "application/json");
        
        // use this to send an empty body 
        // www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes("{}")); // Empty JSON payload
       
        // OLD Add the authorization token to the header
        // www.SetRequestHeader("authorizationToken", payload.ToString());
        // Debug.Log("payload!!! here!! its the authtoken!!" + Uri.EscapeUriString(payload.ToString()));
        
        
        
        
        yield return www.SendWebRequest();
        
        

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Network error: " + www.error);
        }
        else
        {
            Debug.Log("Status Code: " + www.responseCode);

            if (www.responseCode == 200) // Successful verification
            {
                Debug.Log("Verification successful!");
                Debug.Log("Response: " + www.downloadHandler.text);
                // Handle successful verification here, e.g., allow user access
                
            }
            else
            {
                Debug.LogWarning("Verification failed. Status Code: " + www.responseCode);
                // Handle the failed verification here, e.g., display an error message to the user
                Debug.Log("Response: " + www.downloadHandler.text);
                // Print request headers
                Debug.Log("Request Header -  " + www.GetRequestHeader("authorizationToken"));
            }
        }
    }
}