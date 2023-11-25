using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;

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

        public CallStatus CheckPermanenceStatus = CallStatus.NotStarted;
        public event EventHandler GetSucceed;
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
            private string suggested = "Student Union~John C. Hitt Library~Engineering Building II";
            private DateTime lastLogin;

            #region Setters and Getters
            public string Username { get => username; set { username = value; PlayerPrefs.SetString("username", value); } }
            public string UserID1 { get => UserID; set { UserID = value; PlayerPrefs.SetString("UserID", value); } }
            public string Email { get => email; set { email = value; PlayerPrefs.SetString("email", value); } }
            public string Realname { get => realname; set { realname = value; PlayerPrefs.SetString("realName", value); } }
            public string Token { get => token; set { token = value; PlayerPrefs.SetString("AuthToken", value); } }
            public string RefreshToken { get => refreshToken; set { refreshToken = value; PlayerPrefs.SetString("RefreshToken", value); } }
            public string CurrScene { get => currScene; set { currScene = value; PlayerPrefs.SetString("currScene", value); } }
            public string Suggested { get => suggested; set { suggested = value; PlayerPrefs.SetString("suggested", value); } }
            public DateTime LastLogin { get => lastLogin; set { lastLogin = value; PlayerPrefs.SetString("lastLogin", value.ToString()); } }
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
            }
        }

        private void OnGetSucceed()
        {
            if (GetSucceed != null)
            {
                GetSucceed(this, EventArgs.Empty);
            }
        }

        static public bool isNotNull()
        {
            return Instance != null && Instance.data != null;
        }

        public void CheckPermanence()
        {
            IDictionary<string, string> request = new Dictionary<string, string>();
            request.Add("email", data.Email);
            if(PlayerPrefs.HasKey("email") && PlayerPrefs.HasKey("AuthToken") && PlayerPrefs.HasKey("RefreshToken"))
                StartCoroutine(Get(request));
        }
        
        //On log out destroy player prefs
        public void Logout(bool saveData)
        {
            PlayerPrefs.DeleteKey("email");
            PlayerPrefs.DeleteKey("UserID");
            PlayerPrefs.DeleteKey("RefreshToken");
            PlayerPrefs.DeleteKey("AuthToken");
            PlayerPrefs.DeleteKey("realName");
            PlayerPrefs.DeleteKey("username");
            PlayerPrefs.DeleteKey("lastLogin");
            PlayerPrefs.DeleteKey("suggested");
            PlayerPrefs.DeleteKey("currScene");
            // update favorite and visited in the database with current snapshot
            // of the lists before clearing the lists and their caches.
            if(saveData) StartCoroutine(UpdateFavoriteAndVisitedInDB(data, true, true, true));

            data = new UserData();
        }

        //should only be called if cache exists
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
            /*if (!File.Exists(FavoritesFilePath) && !File.Exists(VisitedFilePath))
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
            }*/

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

        public IEnumerator UpdateFavoriteAndVisitedInDB(UserData data, bool favorite, bool visited, bool clear=false)
        {
            if (!favorite && !visited) yield break;

            string Token = data.Token;
            string RefreshToken = data.RefreshToken;
            JObject payload =
                new(
                    new JProperty("UserID", data.UserID1)
                );

            //add to payload and limit how much data can be written (if file path doesn't exist then no changes were made)
            if (favorite && File.Exists(FavoritesFilePath))
            {
                if (data.favorite != null && data.favorite.Count <= 130 && !data.favorite.Any(x => x.Length > 70) && data.favorite.Count > 0)
                    payload.Add(new JProperty("favorite", data.favorite ));
                else if(data.favorite.Count == 0 || data.favorite == null)
                    payload.Add(new JProperty("favorite", null));

            }
            if (visited && File.Exists(VisitedFilePath))
            {
                if (data.visited != null && data.visited.Count <= 130 && !data.visited.Any(x => x.Length > 70) && data.visited.Count > 0)
                    payload.Add(new JProperty("visited", data.visited));
                else if (data.visited.Count == 0 || data.visited == null)
                    payload.Add(new JProperty("visited", null));
            }

            if (payload.Count < 2) yield break;

            //update user
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
            if (clear)
            {
                ClearFavorites();
                ClearVisited();
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

        public List<string> LoadFavorites(UserData data)
        {
            if (File.Exists(FavoritesFilePath))
            {
                string json = File.ReadAllText(FavoritesFilePath);
                StoreInFile obj = JsonConvert.DeserializeObject<StoreInFile>(json);
                return obj.listy;
            }
            return new List<string>();
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

        public List<string> LoadVisited(UserData data)
        {
            if (File.Exists(VisitedFilePath))
            {
                string json = File.ReadAllText(VisitedFilePath);
                StoreInFile obj = JsonConvert.DeserializeObject<StoreInFile>(json);
                return obj.listy;
            }
            return new List<string>();
        }

        public void ClearVisited()
        {
            if (File.Exists(VisitedFilePath))
            {
                File.Delete(VisitedFilePath);
            }
            if (data.visited != null) data.visited.Clear();
        }

        //LoadPlayerPrefs(data);

        override public IEnumerator UpdateFields(IDictionary<string, string> request)
        {
            string reqBody = JsonConvert.SerializeObject(request);
            var www = UnityWebRequest.Put(updateCodeURL, reqBody);
            www.SetRequestHeader("authorizationToken", "{\"token\":\"" + data.Token + "\", \"refreshToken\":\"" + data.RefreshToken + "\"}");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                if (www.error.Contains("Forbidden"))
                {
                    Logout(false);
                    TransitionManager tm = TransitionManager.getInstance();
                    if (tm != null) tm.StartPlay("Login", Transitions.FromTopIn, Transitions.FromTopOut, 0.5f, 0f, 0.5f, 0f);
                    else SceneManager.LoadScene("Login");
                }
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

                //update token
                data.Token = jsonResponse["tokens"]["token"] != null ? jsonResponse["tokens"]["token"].Value<string>() : data.Token;
            }
            currentCall = null;
        }
        override protected IEnumerator Get(IDictionary<string, string> request)
        {
            CheckPermanenceStatus = CallStatus.InProgress;
            string token = data.Token;
            string refreshToken = data.RefreshToken;

            //get payload
            JObject payload = new();
            if (request.ContainsKey("email"))
            {
                payload = new(
                    new JProperty("email", request["email"])
                );
            }

            //make request
            UnityWebRequest www = UnityWebRequest.Post(UserGetURL, payload.ToString(), "application/json");
            www.SetRequestHeader("authorizationToken", "{\"token\":\"" + token + "\",\"refreshToken\":\"" + refreshToken + "\"}");
            yield return www.SendWebRequest();

            //if unauthorized, call failed
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                CheckPermanenceStatus = CallStatus.Failed;
                Logout(false);
            }
            //if error but succeeded, tampering was done
            else if (www.downloadHandler.text.Contains("ERROR"))
            {
                Debug.Log(www.downloadHandler.text);
                CheckPermanenceStatus = CallStatus.Failed;
                Logout(false);
            }
            //if succeed, update everything but favorites and visited (only update these if the associated files don't exist)
            else
            {
                Debug.Log("Form upload complete!");
                Debug.Log("Status Code: " + www.responseCode);
                Debug.Log(www.result);
                JObject jsonResponse = JObject.Parse(www.downloadHandler.text);
                Debug.Log("Response: " + jsonResponse);
                CheckPermanenceStatus = CallStatus.Succeeded;

                //set fields
                data.Realname = jsonResponse["users"][0]["name"] != null ? jsonResponse["users"][0]["name"].ToObject<string>() : "";
                data.Email = jsonResponse["users"][0]["email"] != null ? jsonResponse["users"][0]["email"].ToObject<string>() : "";
                data.UserID1 = jsonResponse["users"][0]["UserID"] != null ? jsonResponse["users"][0]["UserID"].ToObject<string>() : "";
                data.Username = jsonResponse["users"][0]["username"] != null ? jsonResponse["users"][0]["username"].ToObject<string>() : "";

                DealWithVisitedAndFavorites(jsonResponse);

                OnGetSucceed();
            }
        }

        private void DealWithVisitedAndFavorites(JObject jsonResponse)
        {
            //flags are to see if you need to update database
            bool visitedFlag = false;
            bool favoritesFlag = false;
            //if visited file exists then load from that, if it doesn't then load from database (will always assume cache is the most up to date)
            if (File.Exists(VisitedFilePath))
            {
                var newData = LoadVisited(data);
                var obtainedData = jsonResponse["users"][0]["visited"].ToObject<List<string>>();

                //if what was returned from get is empty (if what's in cache is empty don't update, otherwise update; means they're both empty if both are null)
                if (obtainedData == null)
                    visitedFlag = newData != null;
                //if both lists match, don't update
                else
                    visitedFlag = newData == null ? true : !Enumerable.SequenceEqual(obtainedData.OrderBy(fElement => fElement), newData.OrderBy(sElement => sElement));

                data.visited = newData == null ? new List<string>() : newData;
                //check if amount of elements is over 100 or the amount of characters is over 70
                if (data.visited.Count > 130 || data.visited.Any(x => x.Length > 70))
                {
                    data.visited = jsonResponse["users"][0]["visited"] != null ? jsonResponse["users"][0]["visited"].ToObject<List<string>>() : new List<string>();
                    SaveVisited();
                    visitedFlag = false;
                }
            }
            else
            {
                data.visited = jsonResponse["users"][0]["visited"] != null ? jsonResponse["users"][0]["visited"].ToObject<List<string>>() : new List<string>();
                SaveVisited();
            }

            //if favorites file exists then load from that, if it doesn't then load from database (will always assume cache is the most up to date)
            if (File.Exists(FavoritesFilePath))
            {
                var newData = LoadFavorites(data);
                var obtainedData = jsonResponse["users"][0]["favorite"].ToObject<List<string>>();

                //if what was returned from get is empty (if what's in cache is empty don't update, otherwise update; means they're both empty if both are null)
                if (obtainedData == null)
                    favoritesFlag = newData != null;
                //if both lists match, don't update
                else
                    favoritesFlag = newData == null ? true : !Enumerable.SequenceEqual(obtainedData.OrderBy(fElement => fElement), newData.OrderBy(sElement => sElement));

                data.favorite = newData == null ? new List<string>() : newData;

                //check if amount of elements is over 100 or the amount of characters is over 70
                if (data.favorite.Count > 130 || data.favorite.Any(x => x.Length > 70))
                {
                    data.favorite = jsonResponse["users"][0]["favorite"] != null ? jsonResponse["users"][0]["favorite"].ToObject<List<string>>() : new List<string>();
                    SaveFavorites();
                    favoritesFlag = false;
                }
            }
            else
            {
                data.favorite = jsonResponse["users"][0]["favorite"] != null ? jsonResponse["users"][0]["favorite"].ToObject<List<string>>() : new List<string>();
                SaveFavorites();
            }

            if (File.Exists(FavoritesFilePath) || File.Exists(VisitedFilePath))
            {
                StartCoroutine(UpdateFavoriteAndVisitedInDB(data, favoritesFlag, visitedFlag));
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
