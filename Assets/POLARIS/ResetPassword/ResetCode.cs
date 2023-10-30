using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using POLARIS.Managers;

public class ResetCode : MonoBehaviour
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

    public void OnEnterCodeClick()
    {
        bool success = false;
        // collect code from what the user entered
        string usrCode = textBox.text; // text from text box

        // Build a request dictionary of <string, string> as the request
        IDictionary<string, string> request = new Dictionary<string, string>();
        request["UserID"] = instance.data.UserID1;
        request["token"] = instance.codeData.Token;
        request["code"] = usrCode;

        // Make a request to UserManager.ResetPasswordCode()
        // this sends the inputted code to see if it was correct or not
        StartCoroutine(instance.ResetPasswordCode(request, (response) => {
            Debug.Log("Received response: " + response);
            // set the success value based on what the api tells us
            success = (bool)response["success"];

            // now we have a real token, not just for code. Store for future use
            instance.data.Token = (string)response["tokens"]["token"];

            // if the code was wrong, don't switch to the next gameobject
            if (!success)
            {
                // display an error message saying code was wrong
                errorMessageText.text = "Code was incorrect, please try again";
                errorMessageText.color = Color.red;
                return;
            }
            // switch to the next game object
            gameObject.SetActive(false);
            next.SetActive(true);
        }, (error) => {
            Debug.Log("Error: " + error);
            errorMessageText.text = "An error occurred while trying to validate your code, please try again later.";
            errorMessageText.color = Color.red;
            // put up an error message saying an error occurred
            // no clue how to do this...
            return;
        }));
    }
}
