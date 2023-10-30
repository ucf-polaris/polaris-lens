using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using POLARIS.Managers;

public class EmailCode : MonoBehaviour
{
    public TMP_InputField textBox;
    public TMP_Text errorMessageText;
    public Button btn;
    private UserManager instance;
    private string Token;
    private string RefreshToken;
    private string UserID;
    public GameObject next;
    // Start is called before the first frame update
    void Start()
    {
        textBox = GetComponentInChildren<TMP_InputField>();
        btn = GetComponentInChildren<Button>();
        instance = UserManager.getInstance();
    }

    public void OnSendCodeClick()
    {
        // when button is pressed, extract string from textBox
        string usrEmail = textBox.text;

        // build request dictionary including the entered email
        IDictionary<string, string> request = new Dictionary<string, string>();
        request["email"] = usrEmail;

        // Start a coroutine for calling the reset password endpoint
        // this gives us back the UserID of the user with this email and the token
        StartCoroutine(instance.ResetPassword(request, (response) => {
            Debug.Log("Received response: " + response);
            // extract the fields from the response
            UserID = (string)response["UserID"];
            Token = (string)response["tokens"]["token"];

            // set the user id and token in the playerprefs
            instance.data.UserID1 = UserID;
            instance.codeData.Token = Token;

            // switch to the next gameobject
            gameObject.SetActive(false);
            next.SetActive(true);
        }, (error) => {
            Debug.Log("Error: " + error);
            errorMessageText.text = "An error occurred, please try again later";
            errorMessageText.color = Color.red;
            return; // put up an error message first
        }));
    }
}
