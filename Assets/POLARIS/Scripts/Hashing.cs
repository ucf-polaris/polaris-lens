using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;


public class Hashing : MonoBehaviour
{
    // returns hashed password
    public static string HashPassword(string password)
    {
        // init hash object
        using (SHA256 mySHA256 = SHA256.Create())
        {
            // compute the hash
            byte[] hashValue = mySHA256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();

            foreach (byte b in hashValue)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }
    
    // // Start is called before the first frame update
    // void Start()
    // {
    //     
    // }
    //
    // // Update is called once per frame
    // void Update()
    // {
    //     
    // }
}
