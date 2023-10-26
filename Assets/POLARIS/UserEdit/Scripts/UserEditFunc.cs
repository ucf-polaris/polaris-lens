using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using POLARIS.Managers;
using UnityEngine.UIElements;

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
    }
    private UserManager instance;
    [SerializeField]
    private string[] buttonNameList;
    private IDictionary<string, Press> buttonList;
    private UIDocument UiDoc;
    private string[] fieldNames;
    private IDictionary<string, FullTextField> fieldsMap;
    private Button confirmButton;

    private void Start()
    {
        //define the needed variables
        instance = UserManager.getInstance();
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

        //1. Update Placeholder correctly (once)
        //2. Update confirm button correctly (once)
        //3. Handle confirm button (once)
        //4. Handle logout button (once)
        //5. Handle reset password button (once)@

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
            fieldsMap["Email"].CurrentValue = (instance.data.Email);
            fieldsMap["Username"].CurrentValue = (instance.data.Username);
            fieldsMap["Name"].CurrentValue = (instance.data.Realname);
        }
        else
        {
            EmptyFields();
        }
    }

    private void OnChangedField(ChangeEvent<string> evt)
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
        if (string.IsNullOrEmpty(textField.text))
            onFocusOut();
        else
            placeholder.text = string.Empty;
            
        textField.RegisterCallback<FocusInEvent>(_ => onFocusIn());
        textField.RegisterCallback<FocusOutEvent>(_ => onFocusOut());

        void onFocusIn()
        {
            if (textField.ClassListContains(placeholderClass))
            {
                placeholder.text = string.Empty;
                textField.RemoveFromClassList(placeholderClass);
            }
        }

        void onFocusOut()
        {
            if (string.IsNullOrEmpty(textField.text))
            {
                placeholder.text = text;
                textField.AddToClassList(placeholderClass);
            }
        }
    }

    public void OnConfirmClick(ClickEvent evt)
    {

    }
}
