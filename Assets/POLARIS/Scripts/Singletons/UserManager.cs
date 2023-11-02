using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.IO;

namespace POLARIS.Managers{
    public class UserManager : BaseManager
    {
        private static UserManager Instance = null;

        public UserData data;
        public UserCodeData codeData;
        private IEnumerator currentCall;
        private const string updateCodeURL = "https://api.ucfpolaris.com/user/update";
        private const string BaseApiURL = "https://api.ucfpolaris.com";
        private const string UserGetURL = BaseApiURL + "/user/get";
        private const string FavoritesFileName = "favorites.json";
        private static readonly string FavoritesFilePath = Path.Combine(Application.persistentDataPath, FavoritesFileName);
        public bool Testing = false;

        void Awake()
        {
            Initialize();
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
            public HashSet<string> favorite;
            [SerializeField]
            public List<string> schedule;
            [SerializeField]
            public List<string> visited;
            [SerializeField]
            private string currScene;

            #region Setters and Getters
            public string Username { get => username; set { username = value; PlayerPrefs.SetString("username", value); } }
            public string UserID1 { get => UserID; set { UserID = value; PlayerPrefs.SetString("UserID", value); } }
            public string Email { get => email; set { email = value; PlayerPrefs.SetString("email", value); } }
            public string Realname { get => realname; set { realname = value; PlayerPrefs.SetString("realName", value); } }
            public string Token { get => token; set { token = value; PlayerPrefs.SetString("AuthToken", value); } }
            public string RefreshToken { get => refreshToken; set { refreshToken = value; PlayerPrefs.SetString("RefreshToken", value); } }
            public string CurrScene { get => currScene; set { currScene = value; PlayerPrefs.SetString("currScene", value); } }
            #endregion
        }

        [Serializable]
        public class UserCodeData
        {
            [SerializeField]
            private string userID;
            [SerializeField]
            private string token;

            #region Setters and Getters
            public string UserID { get => userID; set { userID = value; PlayerPrefs.SetString("UserID", value); } }
            public string Token { get => token; set { token = value; PlayerPrefs.SetString("CodeAuthToken", value); } }
            #endregion
        }

        static public UserManager getInstance()
        {
            return Instance;
        }

        public void Initialize()
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
            ClearFavorites();
            data = new UserData();

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
            LoadFavorites(data);

            return data.UserID1 != "" && data.Token != "";
        }

        public void SaveFavorites()
        {
            string json = JsonUtility.ToJson(new List<string>(data.favorite));
            File.WriteAllText(FavoritesFilePath, json);
        }

        public void LoadFavorites(UserData data)
        {
            if (File.Exists(FavoritesFilePath))
            {
                string json = File.ReadAllText(FavoritesFilePath);
                List<string> favList = JsonUtility.FromJson<List<string>>(json);
                data.favorite = new HashSet<string>(favList);
            }
        }

        public void ClearFavorites()
        {
            if (File.Exists(FavoritesFilePath))
            {
                File.Delete(FavoritesFilePath);
            }
            data.favorite.Clear();
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
                data.favorite = jsonResponse["favorite"] != null ? jsonResponse["favorite"].Value<HashSet<string>>() : data.favorite;
                data.visited = jsonResponse["visited"] != null ? jsonResponse["visited"].Value<List<string>>() : data.visited;
            }
            currentCall = null;
        }
        override protected IEnumerator Get(IDictionary<string, string> request)
        {
            string Token = data.Token;
            string RefreshToken = data.RefreshToken;

            JObject payload =
                new(
                    new JProperty("email", request["email"])
                );
            UnityWebRequest www = UnityWebRequest.Post(UserGetURL, payload.ToString(), "application/json");
            www.SetRequestHeader("authorizationToken", "{\"token\":\"" + Token + "\",\"refreshToken\":\"" + RefreshToken + "\"}");
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
                JObject jsonResponse = JObject.Parse(www.downloadHandler.text);
                Debug.Log("Response: " + jsonResponse);
            }
        }
        public IEnumerator ResetPasswordCode(IDictionary<string, string> request, Action<JObject> onSuccess, Action<string> onError)
        {
            JObject payload =
                new(
                    new JProperty("code", request["code"]),
                    new JProperty("UserID", request["UserID"])
                );
            UnityWebRequest www = UnityWebRequest.Post(BaseApiURL + "/user/passwordresetcode", payload.ToString(), "application/json");
            www.SetRequestHeader("authorizationToken", "{\"token\":\"" + request["token"] + "\"}");
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                onError?.Invoke(www.error);
            }
            else if (www.downloadHandler.text.Contains("ERROR"))
            {
                Debug.Log(www.downloadHandler.text);
                onError?.Invoke(www.error);
            }
            else
            {
                Debug.Log("Form upload complete!");
                Debug.Log("Status Code: " + www.responseCode);
                Debug.Log(www.result);
                JObject jsonResponse = JObject.Parse(www.downloadHandler.text);
                Debug.Log("Response: " + jsonResponse);

                onSuccess?.Invoke(jsonResponse);
            }
        }
        public IEnumerator ResetPassword(IDictionary<string, string> request, Action<JObject> onSuccess, Action<string> onError)
        {
            JObject payload =
                new(
                    new JProperty("email", request["email"])
                );
            UnityWebRequest www = UnityWebRequest.Post(BaseApiURL + "/user/passwordreset", payload.ToString(), "application/json");
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                onError?.Invoke(www.error);
            }
            else if (www.downloadHandler.text.Contains("ERROR"))
            {
                Debug.Log(www.downloadHandler.text);
                onError?.Invoke(www.error);
            }
            else
            {
                Debug.Log("Form upload complete!");
                Debug.Log("Status Code: " + www.responseCode);
                Debug.Log(www.result);
                JObject jsonResponse = JObject.Parse(www.downloadHandler.text);
                Debug.Log("Response: " + jsonResponse);

                onSuccess?.Invoke(jsonResponse);
            }
        }
        public IEnumerator UpdatePassword(IDictionary<string, string> request, Action<JObject> onSuccess, Action<string> onError)
        {
            string Token = data.Token;
            string RefreshToken = data.RefreshToken;
            Debug.Log(Token);
            JObject payload =
                new(
                    new JProperty("password", request["new_password"]),
                    new JProperty("UserID", request["UserID"])
                );
            UnityWebRequest www = UnityWebRequest.Put(BaseApiURL + "/user/update", payload.ToString());
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("authorizationToken", "{\"token\":\"" + Token + "\",\"refreshToken\":\"" + RefreshToken + "\"}");
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                onError?.Invoke(www.error);
            }
            else if (www.downloadHandler.text.Contains("ERROR"))
            {
                Debug.Log(www.downloadHandler.text);
                onError?.Invoke(www.error);
            }
            else
            {
                Debug.Log("Form upload complete!");
                Debug.Log("Status Code: " + www.responseCode);
                Debug.Log(www.result);
                JObject jsonResponse = JObject.Parse(www.downloadHandler.text);
                Debug.Log("Response: " + jsonResponse);

                onSuccess?.Invoke(jsonResponse);
            }
        }

        public bool isFavorite(LocationData building)
        {
            return data.favorite.Contains(building.BuildingName);
        }

        public void UpdateFavorites(bool mode, string buildingName)
        {
            if (mode && !data.favorite.Contains(buildingName))
                data.favorite.Add(buildingName);
            else if (!mode && data.favorite.Contains(buildingName))
                data.favorite.Remove(buildingName);
            else
                Debug.Log(mode ? "Building is already favorited, why favorite again?" : "Building isn't even favorited, why defavorite?!?");
            SaveFavorites();
        }


        //could implement this as login though
        override protected IEnumerator Scan(IDictionary<string, string> request)
        {
            yield return null;
            Debug.LogWarning("This should not be called or implemented");
        }

    }
    
}
