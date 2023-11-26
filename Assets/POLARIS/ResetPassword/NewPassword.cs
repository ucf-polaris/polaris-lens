using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using POLARIS.Managers;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class NewPassword : NonManagerEndpoint
{
    public TMP_InputField[] textBoxes;
    public TMP_InputField textBox;
    public TMP_InputField textBoxConfirm;
    public TMP_Text errorMessageText;
    private string Token;
    private string RefreshToken;
    private string UserID;
    private string prevScene;
    public Animator parentAnimator;

    // Start is called before the first frame update
    new void Start()
    {
        base.Start();

        Token = instance.data.Token;
        UserID = instance.data.UserID1;
        prevScene = instance.data.CurrScene;

        if (string.IsNullOrEmpty(prevScene))
        {
            prevScene = "Login";
        }
    }

    public void OnChangePassClick()
    {
        CurrentState = EndpointState.InProgress;
        StartCoroutine(ResetPassword());
    }

    private void EvaluatePassword(string newPass)
    {
        // Next, build the request dictionary for the reset password endpoint
        IDictionary<string, string> request = new Dictionary<string, string>();
        request["UserID"] = UserID;
        request["new_password"] = Hashing.HashPassword(newPass);

        // Make a request to UserManager.UpdatePassword()
        // this sends the new password to be updated in the user object
        StartCoroutine(instance.UpdatePassword(request, (response) =>
        {
            Debug.Log("Received Response: " + response);
            CurrentState = EndpointState.Succeed;
            StartCoroutine(waiter());
        }, (error) =>
        {
            Debug.Log("Error: " + error);

            CurrentState = EndpointState.Failed;
            StartCoroutine(OnFail(error));
            // display error message
            return;
        }));
    }
    IEnumerator ResetPassword()
    {
        // Grab the new password and the confirmed password
        string newPass = textBox.text; // = text box text
        string newPassConfirm = textBoxConfirm.text; // = text box text

        yield return new WaitUntil(() => ani.GetInteger("State") != 0);
        // First check if the passwords match
        if (newPass != newPassConfirm)
        {
            // passwords didn't match, post up an error message
            Debug.Log("PASSWORDS DIDN'T MATCH");
            CurrentState = EndpointState.Failed;
            StartCoroutine(OnDifferentPassword());
            yield break;
        }
        else
        {
            errorMessageText.text = ""; // just in case
            EvaluatePassword(newPass);
        }
    }
    IEnumerator OnFail(string error)
    {
        yield return new WaitUntil(() => ani.GetInteger("State") == 0);
        errorMessageText.text = error != "" && error.Length <= 40 ? error : "An error occurred, please try again later";
        errorMessageText.color = Color.red;
    }
    IEnumerator OnDifferentPassword()
    {
        yield return new WaitUntil(() => ani.GetInteger("State") == 0);
        errorMessageText.text = "Passwords do not match, please try again.";
        errorMessageText.color = Color.red;
        EventSystem eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();
        if (eventSystem != null)
        {
            eventSystem.enabled = true;
        }
    }
    IEnumerator waiter()
    {
        yield return new WaitUntil(() => ani.GetInteger("State") == 0);
        errorMessageText.text = "Password successfully updated!\n Returning back...";
        errorMessageText.color = Color.green;
        yield return new WaitForSeconds(2f);
        TransitionManager.getInstance().StartPlay(prevScene, Transitions.FadeIn, Transitions.FadeOut, 0.5f, 0f, 0.5f, 0f);
    }
}
