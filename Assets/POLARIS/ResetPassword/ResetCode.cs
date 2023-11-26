using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using POLARIS.Managers;
using UnityEngine.EventSystems;

public class ResetCode : NonManagerEndpoint
{
    public TMP_InputField textBox;
    public TMP_Text errorMessageText;
    public Animator parentAnimator;

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
        CurrentState = EndpointState.InProgress;
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
            CurrentState = EndpointState.Succeed;
            StartCoroutine(OnSuccess());
        }, (error) => {
            Debug.Log("Error: " + error);
            
            CurrentState = EndpointState.Failed;
            StartCoroutine(OnFail(error));
            return;
        }));
    }

    IEnumerator OnSuccess()
    {
        yield return new WaitUntil(() => ani.GetInteger("State") == 0);
        parentAnimator.Play("ToReset");
    }

    IEnumerator OnFail(string error)
    {
        yield return new WaitUntil(() => ani.GetInteger("State") == 0);
        errorMessageText.text = error != "" && error.Length <= 40 ? error : "An error occurred, please try again later";
        errorMessageText.color = Color.red;
    }
}
