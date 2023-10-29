using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using POLARIS.Managers;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
public class UserEditFunc : MonoBehaviour
{
    public class FullTextField
    {
        public TextField field;
        public Label placeholder;
        private string currentValue;

        public FullTextField(TextField field, Label placeholder)
        {
            this.field = field;
            this.placeholder = placeholder;
            this.CurrentValue = "";
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

    private void Start()
    {
        //define the needed variables
        UserInstance = UserManager.getInstance();
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

        resetPasswordPress = new Press(UiDoc, "ResetPassowrd");
        //resetPasswordPress.AddEvent(OnConfirmClick);
        //1. Update Placeholder correctly (once)
        //2. Update confirm button correctly (once)
        //3. Handle confirm button (once)
        //4. Handle logout button (once)
        //5. Handle reset password button (once)

        //initialize text input fields
        fieldsMap = new Dictionary<string, FullTextField>();
        foreach (string name in fieldNames)
        {
            //define variables
            var field = UiDoc.rootVisualElement.Q<VisualElement>(name + "Field");
            TextField userInput = field.Q<TextField>("UserInput");
            Label placeholder = field.Q<Label>("Placeholder");

            //register update on change for confirm
            userInput.RegisterValueChangedCallback(OnChangedField);

            //register update on change for placeholder
            SetPlaceholderText(userInput, placeholder, name);

            //add to list
            fieldsMap.Add(name, new FullTextField(userInput, placeholder));
        }
        Initialize();
    }
    public void Initialize()
    {
        PopulateTextFields();
    }

    public void EmptyFields()
    {
        //empty out fields
        foreach (string name in fieldNames)
        {
            fieldsMap[name].CurrentValue = ("");
        }
    }

    private void PopulateTextFields()
    {
        //populate if UserManager not null
        if (UserManager.isNotNull())
        {
            fieldsMap["Email"].CurrentValue = (UserInstance.data.Email);
            fieldsMap["Username"].CurrentValue = (UserInstance.data.Username);
            fieldsMap["Name"].CurrentValue = (UserInstance.data.Realname);
            //set initial value
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
        }
    }

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

    public IEnumerator CallBackend(IDictionary<string, string> req)
    {
        yield return StartCoroutine(UserInstance.UpdateFields(req));
        //correct confirm
        if (UserInstance.data.Email == fieldsMap["Email"].field.value)
            fieldsMap["Email"].matchValues();

        if (UserInstance.data.Username == fieldsMap["Username"].field.value)
        {
            Debug.Log(fieldsMap["Username"].field.value);
            Debug.Log(UserInstance.data.Username);
            fieldsMap["Username"].matchValues();
            Debug.Log("yes");
        }
            
        if (UserInstance.data.Realname == fieldsMap["Name"].field.value)
            fieldsMap["Name"].matchValues();

        renderConfirmButton();
        Debug.Log("Made it");
        callingFunction = null;
    }

    public void OnConfirmClick()
    {
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

        if(callingFunction == null)
        {
            request["UserID"] = UserInstance.data.UserID1;
            callingFunction = CallBackend(request);
            StartCoroutine(callingFunction);
        }
    }

    public void OnLogOutClick()
    {
        UserInstance.Logout();
        SceneManager.LoadScene("Login");
    }
}
