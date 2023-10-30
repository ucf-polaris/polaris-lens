using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using POLARIS.Managers;
using UnityEngine.SceneManagement;

public class NewPassword : MonoBehaviour
{
    public TMP_InputField[] textBoxes;
    public TMP_InputField textBox;
    public TMP_InputField textBoxConfirm;
    public TMP_Text errorMessageText;
    public Button btn;
    private UserManager instance;
    private string Token;
    private string RefreshToken;
    private string UserID;
    private string prevScene;
    public GameObject next;
    // Start is called before the first frame update
    void Start()
    {
        textBoxes = GetComponentsInChildren<TMP_InputField>();
        if (textBoxes.Length != 2)
        {
            Debug.LogError("Woah there! There aren't 2 text boxes here somehow...yikes!");
        }
        else
        {
            textBox = textBoxes[0];
            textBoxConfirm = textBoxes[1];
        }
        btn = GetComponentInChildren<Button>();
        instance = UserManager.getInstance();
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
        // Grab the new password and the confirmed password
        string newPass = textBox.text; // = text box text
        string newPassConfirm = textBoxConfirm.text; // = text box text

        // First check if the passwords match
        if (newPass != newPassConfirm)
        {
            // passwords didn't match, post up an error message
            Debug.Log("PASSWORDS DIDN'T MATCH");
            errorMessageText.text = "Passwords do not match, please try again.";
            errorMessageText.color = Color.red;
            return;
        }
        else
        {
            errorMessageText.text = ""; // just in case
        }

        // Next, build the request dictionary for the reset password endpoint
        IDictionary<string, string> request = new Dictionary<string, string>();
        request["UserID"] = UserID;
        request["new_password"] = newPass;

        // Make a request to UserManager.UpdatePassword()
        // this sends the new password to be updated in the user object
        StartCoroutine(instance.UpdatePassword(request, (response) =>
        {
            Debug.Log("Received Response: " + response);
            errorMessageText.text = "Password successfully updated!\n Returning back...";
            errorMessageText.color = Color.green;

            StartCoroutine(waiter());
        }, (error) =>
        {
            Debug.Log("Error: " + error);
            errorMessageText.text = "An error occurred when resetting your password, please try again later.";
            errorMessageText.color = Color.red;
            // display error message
            return;
        }));
    }

    IEnumerator waiter()
    {
        yield return new WaitForSeconds(5);
        SceneManager.LoadScene(prevScene);
    }
}
