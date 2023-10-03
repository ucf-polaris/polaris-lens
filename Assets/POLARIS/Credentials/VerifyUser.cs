using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using TMPro;

public class VerificationCodeScript : MonoBehaviour
{
    public TMP_InputField verificationCodeInput;
    public string verifyCodeURL = "https://ea01n0gzv0.execute-api.us-east-2.amazonaws.com/Stage/user/registrationcode";

    public void Verify()
    {
        StartCoroutine(SendVerificationRequest(verificationCodeInput.text));
    }

    IEnumerator SendVerificationRequest(string code)
    {
        JObject payload =
            new JObject(
            new JProperty("code", code)
        );

        UnityWebRequest www = UnityWebRequest.Post(verifyCodeURL, payload.ToString(), "application/json");

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
            }
        }
    }
}