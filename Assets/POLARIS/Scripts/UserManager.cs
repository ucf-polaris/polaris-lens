using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserManager : MonoBehaviour
{
    private static UserManager Instance = null;
    private string username;
    [SerializeField]
    private string UserID;
    [SerializeField]
    private string email;
    [SerializeField]
    private string realname;
    private string token;
    private string refreshToken;
    [HideInInspector]
    public List<string> favorite;
    [HideInInspector]
    public List<string> schedule;
    [HideInInspector]
    public List<string> visited;

    void Awake()
    {
        //create singleton
        if (Instance != this && Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
            //LoadPlayerPrefs();
        } 
    }

    public static UserManager getInstance()
    {
        return Instance;
    }

    #region Getters
    public string GetEmail()
    {
        if (email == null) return "";
        return email;
    }

    public string GetUserID()
    {
        if (UserID == null) return "";
        return UserID;
    }

    public string GetUserName()
    {
        if (username == null) return "";
        return username;
    }

    public string GetRealName()
    {
        if (realname == null) return "";
        return realname;
    }

    public string GetToken()
    {
        if (token == null) return "";
        return token;
    }

    public string GetRefreshToken()
    {
        if (refreshToken == null) return "";
        return refreshToken;
    }
    #endregion

    #region Setters
    public void SetEmail(string val)
    {
        email = val;
        PlayerPrefs.SetString("email", val);
    }

    public void SetUserID(string val)
    {
        UserID = val;
        PlayerPrefs.SetString("UserID", val);
    }

    public void SetUserName(string val)
    {
        username = val;
        PlayerPrefs.SetString("username", val);
    }

    public void SetRealName(string val)
    {
        realname = val;
        PlayerPrefs.SetString("realName", val);
    }

    public void SetToken(string val)
    {
        token = val;
        PlayerPrefs.SetString("AuthToken", val);
    }

    public void SetRefreshToken(string val)
    {
        refreshToken = val;
        PlayerPrefs.SetString("RefreshToken", val);
    }
    #endregion

    public bool LoadPlayerPrefs()
    {
        email = PlayerPrefs.GetString("email");
        UserID = PlayerPrefs.GetString("UserID");
        refreshToken = PlayerPrefs.GetString("RefreshToken");
        token = PlayerPrefs.GetString("AuthToken");
        realname = PlayerPrefs.GetString("realName");
        username = PlayerPrefs.GetString("username");

        return UserID != "" && token != "";
    }

    //On log out destroy player prefs
    public void Logout()
    {
        PlayerPrefs.DeleteKey("email");
        PlayerPrefs.DeleteKey("UserID");
        PlayerPrefs.DeleteKey("RefreshToken");
        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.DeleteKey("realName");
        PlayerPrefs.DeleteKey("username");

        /*
        PlayerPrefs.DeleteKey("favorites");
        PlayerPrefs.DeleteKey("schedule");
        PlayerPrefs.DeleteKey("visited");
        */
    }

    public void BackendCall(IDictionary<string, string> request)
    {
        //make backend call to update here (or implement system to avoid spams to backend)
    }
}
