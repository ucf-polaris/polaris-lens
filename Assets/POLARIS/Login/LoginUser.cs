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
using POLARIS.Managers;
using UnityEngine.EventSystems;

public class LoginUser : MonoBehaviour
{
    private Animator ani;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    private UserManager instance;
    private string Token;
    private string UserID;
    private string RefreshToken;
    private string prevScene;
    public string loginURL = "https://api.ucfpolaris.com/user/login";

    public enum LoginState
    {
        NotStarted,
        InProgress,
        Failed,
        Succeed,
        NotVerified
    }

    private LoginState _currentState = LoginState.NotStarted;
    public LoginState CurrentState { get => _currentState; set { if(ani != null) ani.SetInteger("State", (int)value); _currentState = value; } }
    public void Start()
    {
        ani = GetComponent<Animator>();
        instance = UserManager.getInstance();
        instance.data.CurrScene = SceneManager.GetActiveScene().name;
    }
    public void Login()
    {
        if(CurrentState == LoginState.NotStarted)
        {
            CurrentState = LoginState.InProgress;
            StartCoroutine(SendLoginRequest(emailInput.text, passwordInput.text));
        }  
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
            CurrentState = LoginState.Failed;
        }
        else if (www.downloadHandler.text.Contains("ERROR"))
        {
            Debug.Log(www.downloadHandler.text);
            CurrentState = LoginState.Failed;
        }
        else
        {
            Debug.Log("Form upload complete!");
            Debug.Log("Status Code: " + www.responseCode);
            Debug.Log(www.result);
            JObject jsonResponse = JObject.Parse(www.downloadHandler.text);
            Debug.Log("Response: " + jsonResponse);
            if (jsonResponse["verified"].Value<bool>() == true)
            {
                instance.data.UserID1 = jsonResponse["UserID"].Value<string>();
                instance.data.Email = jsonResponse["email"] != null ? jsonResponse["email"].Value<string>() : "";
                instance.data.Username = jsonResponse["username"] != null ? jsonResponse["username"].Value<string>() : "";
                instance.data.Realname = jsonResponse["name"] != null ? jsonResponse["name"].Value<string>() : "";
                instance.data.schedule = jsonResponse["schedule"] != null ? jsonResponse["schedule"].Value<List<string>>() : new List<string>();
                instance.data.favorite = jsonResponse["favorite"] != null ? jsonResponse["favorite"].ToObject<List<string>>() : new List<string>();
                instance.data.visited = jsonResponse["visited"] != null ? jsonResponse["visited"].ToObject<List<string>>() : new List<string>();
                instance.data.Token = (string)jsonResponse["tokens"]["token"];
                instance.data.RefreshToken = (string)jsonResponse["tokens"]["refreshToken"];
                //keep track of when last logged in.
                instance.data.LastLogin = DateTime.UtcNow;
                //SceneManager.LoadScene("MainScene");
                CurrentState = LoginState.Succeed;
            }
            else
            {
                JObject jobj = JObject.Parse(www.downloadHandler.text);

                instance.codeData.UserID = jobj["UserID"].Value<string>();
                instance.codeData.Token = jobj["tokens"]["token"].Value<string>();
                //SceneManager.LoadScene("Verify");
                CurrentState = LoginState.NotVerified;
            }
            
        }
    }
    
}
