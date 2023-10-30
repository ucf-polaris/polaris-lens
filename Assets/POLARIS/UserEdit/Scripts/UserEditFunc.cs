using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using POLARIS.Managers;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using System.Text.RegularExpressions;

public class UserEditFunc : MonoBehaviour
{
    public class FullTextField
    {
        public TextField field;
        public Label placeholder;
        public Label errorMessage;
        private string currentValue;
        public IEnumerator errorFadePlaying;

        public FullTextField(TextField field, Label placeholder, Label errorMessage)
        {
            this.field = field;
            this.placeholder = placeholder;
            this.CurrentValue = "";
            this.errorMessage = errorMessage;
        }

        public string CurrentValue { 
            get => currentValue; 
            set { currentValue = value; if(field != null) field.value = value; } }

        public bool valueHasChanged()
        {
            return field.value != currentValue;
        }

        public void matchValues()
        {
            currentValue = field.value;
        }
    }
    private UserManager UserInstance;
    [SerializeField]
    private string[] buttonNameList;
    private IDictionary<string, Press> buttonList;
    private UIDocument UiDoc;
    private string[] fieldNames;
    private IDictionary<string, FullTextField> fieldsMap;
    private Button confirmButton;
    private Press confirmPress;
    private Press logOutPress;
    private Press resetPasswordPress;
    private IEnumerator callingFunction;

    public bool errorTesting;
    private void Start()
    {
        //define the needed variables
        UserInstance = UserManager.getInstance();
        UserInstance.data.CurrScene = SceneManager.GetActiveScene().name;
        UiDoc = gameObject.GetComponent<UIDocument>();
        buttonNameList = new string[] { "Confirm", "ResetPassword", "LogOut" };
        fieldNames = new string[] { "Email", "Username", "Name" };

        //define the three button fields
        buttonList = new Dictionary<string, Press>();
        foreach (string name in buttonNameList)
        {
            buttonList.Add(name, new Press(UiDoc, name));
        }

        //confirm field set
        confirmButton = UiDoc.rootVisualElement.Q<Button>("Confirm");
        confirmPress = new Press(UiDoc, "Confirm");
        confirmPress.AddEvent(OnConfirmClick);

        logOutPress = new Press(UiDoc, "LogOut");
        logOutPress.AddEvent(OnLogOutClick);

        resetPasswordPress = new Press(UiDoc, "ResetPassword");
        resetPasswordPress.AddEvent(OnResetPasswordClick);

        //initialize text input fields
        fieldsMap = new Dictionary<string, FullTextField>();
        foreach (string name in fieldNames)
        {
            //define variables
            var field = UiDoc.rootVisualElement.Q<VisualElement>(name + "Field");
            TextField userInput = field.Q<TextField>("UserInput");
            Label placeholder = field.Q<Label>("Placeholder");
            Label errorMessage = field.Q<Label>("ErrorMessage");

            //register update on change for confirm
            userInput.RegisterValueChangedCallback(OnChangedField);

            //register update on change for placeholder
            SetPlaceholderText(userInput, placeholder, name);

            //add to list
            fieldsMap.Add(name, new FullTextField(userInput, placeholder, errorMessage));
        }
        Initialize();
    }
    public void Initialize()
    {
        PopulateTextFields();
    }
    #region TextFieldFunctions
    public void EmptyFields()
    {
        //empty out fields
        foreach (string name in fieldNames)
        {
            fieldsMap[name].CurrentValue = ("");
        }
    }

    //gets values from UserManager and populates text field, also initializes whether confirm button should appear
    private void PopulateTextFields()
    {
        //populate if UserManager not null
        if (UserManager.isNotNull())
        {
            //set initial values
            fieldsMap["Email"].CurrentValue = (UserInstance.data.Email);
            fieldsMap["Username"].CurrentValue = (UserInstance.data.Username);
            fieldsMap["Name"].CurrentValue = (UserInstance.data.Realname);
        }
        else
        {
            EmptyFields();
        }
    }

    private void OnChangedField(ChangeEvent<string> evt)
    {
        renderConfirmButton();
    }

    //detects if text in fields have changed (and if the confirm button should appear as result)
    private void renderConfirmButton()
    {
        bool flag = false;
        //checks if all the field names were same from inital
        foreach (FullTextField field in fieldsMap.Values)
        {
            if (field.valueHasChanged())
            {
                flag = true;
                break;
            }
        }

        //if no change has occured
        if (!flag)
        {
            confirmButton.style.height = 0;
        }
        else
        {
            confirmButton.style.height = 200;
            //stop shaking if it's shaking
            if (playing != null)
            {
                StopCoroutine(playing);
                confirmButton.style.translate = new Translate(0, 0);
                confirmButton.SetEnabled(true);
            }
        }
    }

    //set placeholder functionallity
    private static void SetPlaceholderText(TextField textField, Label placeholder, string text)
    {
        string placeholderClass = TextField.ussClassName + "__placeholder";

        //set initial value
        onChange();
        textField.RegisterCallback<ChangeEvent<string>>(_ => onChange());

        void onChange()
        {
            //check if place holder should be empty
            if (string.IsNullOrEmpty(textField.text))
                placeholder.text = text;
            else
                placeholder.text = string.Empty;
        }
    }
    public enum fieldValidationChecks
    {
        emptyError,
        whitespaceError,
        emailFormatError,
        success
    }
    private IDictionary<fieldValidationChecks, string> errorMessages = new Dictionary<fieldValidationChecks, string>()
    {
        {fieldValidationChecks.emptyError, "(field cannot be empty)"},
        {fieldValidationChecks.whitespaceError, "(field cannot contain whitespaces)" },
        {fieldValidationChecks.emailFormatError, "(email is invalid)" }
    };
    //iterates through all fields and check if all are valid on confirm click
    private bool allFieldCheck()
    {
        //for testing
        if (errorTesting)
        {
            //set errors
            foreach (string name in fieldNames)
            {
                playErrorMessage(fieldsMap[name], "(THIS IS A TEST)");
            }
            ConfirmShakeAnimation();
            return false;
        }

        //error checking
        bool errorExists = false;
        foreach (string name in fieldNames)
        {
            bool flag = false;

            //trim whitespace characters
            fieldsMap[name].field.value = fieldsMap[name].field.value.Trim();

            if (name == "Email") flag = true;
            fieldValidationChecks checkError = isFieldValid(fieldsMap[name].field.value, flag, flag);

            //output error message
            if (checkError != fieldValidationChecks.success && fieldsMap[name].valueHasChanged())
            {
                errorExists = true;
                playErrorMessage(fieldsMap[name], errorMessages[checkError]);
            }
        }

        return !errorExists;
    }
    //checking if the fields are valid
    private fieldValidationChecks isFieldValid(string text, bool email = false, bool whitespaces = false)
    {
        //check if empty
        if (text.Length == 0)
            return fieldValidationChecks.emptyError;

        //check if any whitespaces exist
        if (whitespaces && text.Any(x => Char.IsWhiteSpace(x)))
            return fieldValidationChecks.whitespaceError;

        //check if email is valid
        if (email && !IsValidEmail(text))
            return fieldValidationChecks.emailFormatError;

        return fieldValidationChecks.success;
    }

    private static bool IsValidEmail(string email)
    {
        string regex = @"^[^@\s]+@[^@\s]+\.(com|net|org|gov)$";

        return Regex.IsMatch(email, regex, RegexOptions.IgnoreCase);
    }

    #endregion

    #region ConfirmButtonFunctions
    //call update user and then choose to render confirm button based on success or failure
    public IEnumerator CallBackend(IDictionary<string, string> req)
    {
        yield return StartCoroutine(UserInstance.UpdateFields(req));

        //check whether user updated succeeded based on data in UserManager (field by field)
        if (UserInstance.data.Email == fieldsMap["Email"].field.value)
            fieldsMap["Email"].matchValues();

        if (UserInstance.data.Username == fieldsMap["Username"].field.value)
            fieldsMap["Username"].matchValues();
            
        if (UserInstance.data.Realname == fieldsMap["Name"].field.value)
            fieldsMap["Name"].matchValues();

        //render confirm button based on above results
        renderConfirmButton();
        callingFunction = null;
    }

    //what should happen when press ocnfirm
    public void OnConfirmClick()
    {
        //if fields are not valid don't send request
        if (!allFieldCheck())
        {
            ConfirmShakeAnimation();
            return;
        }
        //create request. Only put field in request if it's changed
        IDictionary<string, string> request = new Dictionary<string, string>();
        foreach (string name in fieldNames)
        {
            //if the field has changed, pack into request
            if (fieldsMap[name].valueHasChanged())
            {
                //can use to lower as the names used as keys match the request name
                request[name.ToLower()] = fieldsMap[name].field.value;
            }
        }

        //call user function if no update is currently happening
        if (callingFunction == null)
        {
            request["UserID"] = UserInstance.data.UserID1;
            callingFunction = CallBackend(request);
            StartCoroutine(callingFunction);
        }
    }
    #endregion

    #region OtherButtonFunctions
    public void OnLogOutClick()
    {
        UserInstance.Logout();
        SceneManager.LoadScene("Login");
    }

    public void OnResetPasswordClick()
    {
        SceneManager.LoadScene("ForgotPWCode");
    }
    #endregion

    #region AnimationFunctions
    IEnumerator playing;
    private void ConfirmShakeAnimation()
    {
        if(playing == null)
        {
            playing = playShakeAnimation(confirmButton);
            StartCoroutine(playing);
        }
    }

    private IEnumerator playShakeAnimation(VisualElement ui)
    {
        ui.SetEnabled(false);
        for(int i = 0;i < 2; i++)
        {
            ui.style.translate = new Translate(10, 0);
            yield return new WaitForSeconds(0.05f);
            ui.style.translate = new Translate(-10, 0);
            yield return new WaitForSeconds(0.05f);
        }
        ui.style.translate = new Translate(0, 0);
        ui.SetEnabled(true);

        playing = null;
    }

    public void playErrorMessage(FullTextField ui, string err)
    {
        //reset error message if currently in process of fading out
        if(ui.errorFadePlaying != null)
        {
            StopCoroutine(ui.errorFadePlaying);
            ui.errorFadePlaying = null;
        }
        ui.errorMessage.text = err;
        FadeIn(ui.errorMessage);
        ui.errorFadePlaying = fadeOutAnimation(ui);
        StartCoroutine(ui.errorFadePlaying);
    }

    private IEnumerator fadeOutAnimation(FullTextField ui)
    {
        yield return new WaitForSeconds(3f);
        FadeOut(ui.errorMessage);
        ui.errorFadePlaying = null;
    }
    void FadeIn(Label ui)
    {
        ui.AddToClassList("fade-in");
        ui.RemoveFromClassList("fade-out");
    }

    void FadeOut(Label ui)
    {
        ui.AddToClassList("fade-out");
        ui.RemoveFromClassList("fade-in");
    }
    #endregion
}
