using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;

namespace POLARIS.Managers{
    public class UserManager : BaseManager
    {
        private static UserManager Instance = null;

        public UserData data;
        private IEnumerator currentCall;
        private const string updateCodeURL = "https://v21x6ajyg9.execute-api.us-east-2.amazonaws.com/dev/user/update";
        
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
                data = new UserData();
                LoadPlayerPrefs(data);
                Debug.Log(data.UserID1);
            } 
        }

        [Serializable]
        public class UserData
        {
            [SerializeField]
            private string username;
            [SerializeField]
            private string UserID;
            [SerializeField]
            private string email;
            [SerializeField]
            private string realname;
            [SerializeField]
            private string token;
            [SerializeField]
            private string refreshToken;
            [SerializeField]
            public List<string> favorite;
            [SerializeField]
            public List<string> schedule;
            [SerializeField]
            public List<string> visited;

            #region Setters and Getters
            public string Username { get => username; set { username = value; PlayerPrefs.SetString("username", value); } }
            public string UserID1 { get => UserID; set { UserID = value; PlayerPrefs.SetString("UserID", value); } }
            public string Email { get => email; set { email = value; PlayerPrefs.SetString("email", value); } }
            public string Realname { get => realname; set { realname = value; PlayerPrefs.SetString("realName", value); } }
            public string Token { get => token; set { token = value; PlayerPrefs.SetString("AuthToken", value); } }
            public string RefreshToken { get => refreshToken; set { refreshToken = value; PlayerPrefs.SetString("RefreshToken", value); } }
            #endregion
        }

        static public UserManager getInstance()
        {
            return Instance;
        }

        static public bool isNotNull()
        {
            return Instance != null && Instance.data != null;
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

        public bool LoadPlayerPrefs(UserData data)
        {
            if (data == null) return false;

            data.Email = PlayerPrefs.GetString("email");
            data.UserID1 = PlayerPrefs.GetString("UserID");
            data.RefreshToken = PlayerPrefs.GetString("RefreshToken");
            data.Token = PlayerPrefs.GetString("AuthToken");
            data.Realname = PlayerPrefs.GetString("realName");
            data.Username = PlayerPrefs.GetString("username");

            return data.UserID1 != "" && data.Token != "";
        }

        public void UpdateBackendCall(IDictionary<string, string> request)
        {
            //make backend call to update here (or implement system to avoid spams to backend)
            request["UserID"] = data.UserID1;
            if(currentCall == null)
            {
                currentCall = UpdateFields(request);
                StartCoroutine(currentCall);
            }
        }

        override public IEnumerator UpdateFields(IDictionary<string, string> request)
        {
            string reqBody = JsonConvert.SerializeObject(request);
            var www = UnityWebRequest.Put(updateCodeURL, reqBody);
            www.SetRequestHeader("authorizationToken", "{\"token\":\"" + data.Token + "\", \"refreshToken\":\"" + data.RefreshToken + "\"}");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else if (www.downloadHandler.text.Contains("ERROR"))
            {
                Debug.Log(www.downloadHandler.text);
            }
            else
            {
                Debug.Log("Form upload complete!");
                Debug.Log("Status Code: " + www.responseCode);
                Debug.Log(www.result);
                Debug.Log("Response: " + www.downloadHandler.text);

                var jsonResponse = JObject.Parse(www.downloadHandler.text);

                //update the fields here
                data.Email = jsonResponse["email"] != null ? jsonResponse["email"].Value<string>() : data.Email;
                data.Username = jsonResponse["username"] != null ? jsonResponse["username"].Value<string>() : data.Username;
                data.Realname = jsonResponse["name"] != null ? jsonResponse["name"].Value<string>() : data.Realname;
                data.schedule = jsonResponse["schedule"] != null ? jsonResponse["schedule"].Value<List<string>>() : data.schedule;
                data.favorite = jsonResponse["favorite"] != null ? jsonResponse["favorite"].Value<List<string>>() : data.favorite;
                data.visited = jsonResponse["visited"] != null ? jsonResponse["visited"].Value<List<string>>() : data.visited;
            }
            currentCall = null;
        }
        override protected IEnumerator Get(IDictionary<string, string> request)
        {
            yield return null;
            Debug.LogWarning("Could implement this, or not");
        }
        //could implement this as login though
        override protected IEnumerator Scan(IDictionary<string, string> request)
        {
            yield return null;
            Debug.LogWarning("This should not be called or implemented");
        }

    }
    
}
