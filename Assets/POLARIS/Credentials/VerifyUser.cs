using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using TMPro;

public class VerificationCodeScript : MonoBehaviour
{
    public TMP_InputField verificationCodeInput;
    public string verifyCodeURL = "https://ea01n0gzv0.execute-api.us-east-2.amazonaws.com/Stage/user/registrationcode";
    // string _token = RegistrationScript.AuthToken;
    public void Verify()
    {
        StartCoroutine(SendVerificationRequest(verificationCodeInput.text));
    }

    IEnumerator SendVerificationRequest(string verifCode)
    {
        
        // Retrieve the authToken from PlayerPrefs
        string authToken = PlayerPrefs.GetString("AuthToken");

        // Use authToken for verification or any other purpose
        Debug.Log("Received Auth Token: " + authToken);
        

        
        JObject payload =
            new JObject(
                new JProperty("token", authToken) //given from register response token : blahblhah
            );
        JObject code =
            new JObject(
                new JProperty("code", verifCode)
            );
        
        UnityWebRequest www = UnityWebRequest.Post(verifyCodeURL,code.ToString(),  "application/json");

        
        //UnityWebRequest www = UnityWebRequest.PostWwwForm(verifyCodeURL,code.ToString(),  "application/json");
        // www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes("{}")); // Empty JSON payload
        // Add the authorization token to the header
        // www.SetRequestHeader("authorizationToken", payload.ToString());
        // www.SetRequestHeader("token", code);
        Debug.Log("payload " + payload.ToString());
        www.SetRequestHeader("authorizationToken",payload.ToString());
        
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