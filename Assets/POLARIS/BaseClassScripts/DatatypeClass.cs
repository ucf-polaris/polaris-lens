using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DatatypeClass : MonoBehaviour
{
    //this is an undone script
    //if we need it I'll finish it -Kaeden
    public class User
    {
        private string username;
        private string UserID;
        private string email;
        private string realname;
        private string token;
        private string refreshToken;
        private List<string> favorite;
        private List<string> schedule;
        private List<string> visited;

        #region Getters
        public string GetUserName()
        {
            return username;
        }
        public string GetUserID()
        {
            return UserID;
        }
        public string GetEmail()
        {
            return email;
        }
        public string GetRealName()
        {
            return realname;
        }
        public string GetToken()
        {
            return token;
        }
        public string GetRefreshToken()
        {
            return refreshToken;
        }
        public List<string> GetFavorite()
        {
            return favorite;
        }
        public List<string> GetSchedule()
        {
            return schedule;
        }
        public List<string> GetVisited()
        {
            return visited;
        }
        #endregion
    }
}
