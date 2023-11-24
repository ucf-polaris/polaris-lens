using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using POLARIS.Managers;

public class EmailCode : NonManagerEndpoint
{
    public TMP_InputField textBox;
    public TMP_Text errorMessageText;
    private string Token;
    private string RefreshToken;
    private string UserID;
    public GameObject next;

    public void OnSendCodeClick()
    {
        // when button is pressed, extract string from textBox
        string usrEmail = textBox.text;

        // build request dictionary including the entered email
        IDictionary<string, string> request = new Dictionary<string, string>();
        request["email"] = usrEmail;

        // Start a coroutine for calling the reset password endpoint
        // this gives us back the UserID of the user with this email and the token
        CurrentState = EndpointState.InProgress;
        StartCoroutine(instance.ResetPassword(request, (response) => {
            Debug.Log("Received response: " + response);
            // extract the fields from the response
            UserID = (string)response["UserID"];
            Token = (string)response["tokens"]["token"];

            // set the user id and token in the playerprefs
            instance.data.UserID1 = UserID;
            instance.codeData.Token = Token;
            Debug.Log(instance.codeData.Token);

            CurrentState = EndpointState.Succeed;
            StartCoroutine(OnSuccess());

        }, (error) => {
            StartCoroutine(PopUpErrorMessage(error));
            CurrentState = EndpointState.Failed;
            return; // put up an error message first
        }));
    }

    IEnumerator PopUpErrorMessage(string error)
    {
        yield return new WaitUntil(() => ani.GetInteger("State") == 0);
        Debug.Log("Error: " + error);
        errorMessageText.text = "An error occurred, please try again later";
        errorMessageText.color = Color.red;
    }

    IEnumerator OnSuccess()
    {
        yield return new WaitUntil(() => ani.GetInteger("State") == 0);
        // switch to the next gameobject
        gameObject.transform.parent.gameObject.SetActive(false);
        next.SetActive(true);
    }
}
