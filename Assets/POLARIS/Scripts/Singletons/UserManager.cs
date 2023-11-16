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
        private string FavoritesFilePath;
        private const string VisitedFileName = "visited.json";
        private string VisitedFilePath;
        public bool Testing = false;
        public bool InitFavAndVisit = false;

        void Awake()
        {
            FavoritesFilePath = Path.Combine(Application.persistentDataPath, FavoritesFileName);
            VisitedFilePath = Path.Combine(Application.persistentDataPath, VisitedFileName);
            Debug.Log(FavoritesFilePath);
            Debug.Log(VisitedFilePath);
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
            public List<string> favorite = new List<string>();
            [SerializeField]
            public List<string> schedule = new List<string>();
            [SerializeField]
            public List<string> visited = new List<string>();
            [SerializeField]
            private string currScene;
            [SerializeField]
            private string suggested;

            #region Setters and Getters
            public string Username { get => username; set { username = value; PlayerPrefs.SetString("username", value); } }
            public string UserID1 { get => UserID; set { UserID = value; PlayerPrefs.SetString("UserID", value); } }
            public string Email { get => email; set { email = value; PlayerPrefs.SetString("email", value); } }
            public string Realname { get => realname; set { realname = value; PlayerPrefs.SetString("realName", value); } }
            public string Token { get => token; set { token = value; PlayerPrefs.SetString("AuthToken", value); } }
            public string RefreshToken { get => refreshToken; set { refreshToken = value; PlayerPrefs.SetString("RefreshToken", value); } }
            public string CurrScene { get => currScene; set { currScene = value; PlayerPrefs.SetString("currScene", value); } }
            public string Suggested { get => suggested; set { suggested = value; PlayerPrefs.SetString("suggested", value); } }
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

        public static UserManager getInstance()
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
            // update favorite and visited in the database with current snapshot
            // of the lists before clearing the lists and their caches.
            StartCoroutine(UpdateFavoriteAndVisitedInDB(data));
            ClearFavorites();
            ClearVisited();
            data = new UserData();

            /*
            PlayerPrefs.DeleteKey("favorites");
            PlayerPrefs.DeleteKey("schedule");
            PlayerPrefs.DeleteKey("visited");
            */
        }

        public bool LoadPlayerPrefs(UserData data)
        {
            Debug.Log("Loading player prefs");
            if (data == null) return false;

            data.Email = PlayerPrefs.GetString("email");
            data.UserID1 = PlayerPrefs.GetString("UserID");
            data.RefreshToken = PlayerPrefs.GetString("RefreshToken");
            data.Token = PlayerPrefs.GetString("AuthToken");
            data.Realname = PlayerPrefs.GetString("realName");
            data.Username = PlayerPrefs.GetString("username");
            // Default to SU, Library, and Engineering 2 (shoutout to Komila)
            data.Suggested = PlayerPrefs.GetString("suggested", "Student Union~John C. Hitt Library~Engineering Building II");

            // condition to check if file is empty
            if (!File.Exists(FavoritesFilePath) && !File.Exists(VisitedFilePath))
            {
                Debug.Log("Cache does not exist, reinitialize...");
                InitFavoriteAndVisited(data);
            }
            // if i haven't logged out, meaning my cache hasn't been cleared, just use the cache
            // instead of requerying the database again.
            else
            {
                Debug.Log("We haven't logged out just yet, we still have a cache!");
                LoadFavorites(data);
                LoadVisited(data);
            }

            return data.UserID1 != "" && data.Token != "";
        }

        public void UpdateBackendCall(IDictionary<string, string> request)
        {
            //make backend call to update here (or implement system to avoid spams to backend)
            request["UserID"] = data.UserID1;
            if (currentCall == null)
            {
                currentCall = UpdateFields(request);
                StartCoroutine(currentCall);
            }
        }

        public void InitFavoriteAndVisited(UserData data)
        {
            Debug.Log("Initiliazing favorites and visited");
            // this is a goofy ahhh way to do it LMAO
            IDictionary<string, string> req = new Dictionary<string, string>();
            req["email"] = data.Email;
            InitFavAndVisit = true;
            StartCoroutine(Get(req));
        }

        public IEnumerator UpdateFavoriteAndVisitedInDB(UserData data)
        {
            Debug.Log("UFAVIDB");
            string Token = data.Token;
            string RefreshToken = data.RefreshToken;
            JObject payload =
                new(
                    new JProperty("UserID", data.UserID1),
                    new JProperty("favorite", data.favorite == null ? null : data.favorite),
                    new JProperty("visited", data.visited == null ? null : data.visited)
                );
            var www = UnityWebRequest.Put(updateCodeURL, payload.ToString());
            www.SetRequestHeader("authorizationToken", "{\"token\":\"" + Token + "\", \"refreshToken\":\"" + RefreshToken + "\"}");
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
            }
        }
        class StoreInFile
        {
            public List<string> listy;
            public StoreInFile(List<string> lst)
            {
                listy = lst;
            }
        }

        public void SaveFavorites()
        {
            Debug.Log("Saving favorites");
            StoreInFile s = new StoreInFile(data.favorite);
            string json = JsonConvert.SerializeObject(s);
            //string json = JsonUtility.ToJson(data.favorite.ToArray());
            Debug.Log(json);
            File.WriteAllText(FavoritesFilePath, json);
        }

        public void LoadFavorites(UserData data)
        {
            if (File.Exists(FavoritesFilePath))
            {
                string json = File.ReadAllText(FavoritesFilePath);
                StoreInFile obj = JsonConvert.DeserializeObject<StoreInFile>(json);
                data.favorite = obj.listy;
            }
        }

        public void ClearFavorites()
        {
            if (File.Exists(FavoritesFilePath))
            {
                File.Delete(FavoritesFilePath);
            }
            if(data.favorite != null) data.favorite.Clear();
        }

        public void SaveVisited()
        {
            Debug.Log("saving visited");
            StoreInFile s = new StoreInFile(data.visited);
            string json = JsonConvert.SerializeObject(s);
            //string json = JsonUtility.ToJson(data.favorite.ToArray());
            Debug.Log(json);
            File.WriteAllText(VisitedFilePath, json);
        }

        public void LoadVisited(UserData data)
        {
            if (File.Exists(VisitedFilePath))
            {
                string json = File.ReadAllText(VisitedFilePath);
                StoreInFile obj = JsonConvert.DeserializeObject<StoreInFile>(json);
                data.visited = obj.listy;
            }
        }

        public void ClearVisited()
        {
            if (File.Exists(VisitedFilePath))
            {
                File.Delete(VisitedFilePath);
            }
            if (data.visited != null) data.visited.Clear();
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
                if (InitFavAndVisit)
                {
                    Debug.Log("Init-ing");
                    Debug.Log(jsonResponse["users"][0]["favorite"]);
                    data.favorite = jsonResponse["users"][0]["favorite"] != null ? jsonResponse["users"][0]["favorite"].ToObject<List<string>>() : new List<string>();
                    data.visited = jsonResponse["users"][0]["visited"] != null ? jsonResponse["users"][0]["visited"].ToObject<List<string>>() : new List<string>();
                    SaveFavorites();
                    SaveVisited();
                    InitFavAndVisit = false;
                }
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
            if (data.favorite == null) return false;
            return data.favorite.Contains(building.BuildingName);
        }

        public void UpdateFavorites(bool add, LocationData building)
        {
            if (data.favorite == null) data.favorite = new List<string>();

            if (add && !data.favorite.Contains(building.BuildingName))
            {
                Debug.Log("Added");
                data.favorite.Add(building.BuildingName);
                building.IsFavorited = true;
            }
            else if (!add && data.favorite.Contains(building.BuildingName))
            {
                Debug.Log("Removed");
                data.favorite.Remove(building.BuildingName);
                building.IsFavorited = false;
            }
            else
                Debug.Log(add ? "Building is already favorited, why favorite again?" : "Building isn't even favorited, why defavorite?!?");
            SaveFavorites();
        }

        public bool isVisited(LocationData building)
        {
            if (data.visited == null) return false;
            return data.visited.Contains(building.BuildingName);
        }

        public void UpdateVisited(bool add, LocationData building)
        {
            if (data.visited == null) data.visited = new List<string>();
            if (add && !data.visited.Contains(building.BuildingName))
            {
                Debug.Log("Added");
                data.visited.Add(building.BuildingName);
                building.IsVisited = true;
            }
            else if (!add && data.visited.Contains(building.BuildingName))
            {
                Debug.Log("Deleted");
                data.visited.Remove(building.BuildingName);
                building.IsVisited = false;
            }
            else
                Debug.Log(add ? "Building is already visited, why visit again?" : "Building isn't even visited, why try to unvisit??!?!");
            SaveVisited();
        }


        //could implement this as login though
        override protected IEnumerator Scan(IDictionary<string, string> request)
        {
            yield return null;
            Debug.LogWarning("This should not be called or implemented");
        }

    }
    
}
