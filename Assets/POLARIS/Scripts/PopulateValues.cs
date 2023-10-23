using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class PopulateValues : MonoBehaviour
{
    public enum Fields{
        email,
        username,
        realname
    }

    private UserManager instance;

    [Serializable]
    public class PopulateFields
    {
        public Fields type;
        public TMP_InputField input;
        private string originalInput;

        public void setInput(string s)
        {
            originalInput = s;
            input.text = s;
        }

        public string changeInput()
        {
            originalInput = input.text;
            return input.text;
        }

        public string GetOriginalInput()
        {
            return originalInput;
        }

        public bool isOriginalInput()
        {
            return input.text == originalInput;
        }
    }

    public PopulateFields[] fieldList;
    public Animator buttonAnim;
    public bool isConfirmOut = false;

    // Start is called before the first frame update
    void Start()
    {
        instance = UserManager.getInstance();
        foreach (var entry in fieldList)
        {
            switch (entry.type)
            {
                case Fields.email:
                    entry.setInput(instance.GetEmail());
                    break;
                case Fields.username:
                    entry.setInput(instance.GetUserName());
                    break;
                case Fields.realname:
                    entry.setInput(instance.GetRealName());
                    break;
            }
        }
    }

    private bool checkIfFieldsMatch()
    {
        bool flag = true;
        foreach (var entry in fieldList)
        {
            if (!entry.isOriginalInput())
            {
                flag = false;
                break;
            }
                
        }
        return flag;
    }

    public void AnimDetermine()
    {
        if (!checkIfFieldsMatch()){
            AnimDown();
        }
        else
        {
            AnimUp();
        }
    }

    private void AnimDown()
    {
        if (isConfirmOut) return;
        buttonAnim.Play("MoveDown", -1, 0f);
        isConfirmOut = true;
    }

    private void AnimUp()
    {
        if (!isConfirmOut) return;
        buttonAnim.Play("MoveUp", -1, 0f);
        isConfirmOut = false;
    }

    public void ConfirmValues()
    {
        //check on if fields are exactly the same as they originally were (should never happen but is here for safety)
        if (checkIfFieldsMatch()) return;
        //construct preliminary request in dictionary (converted to json later)
        IDictionary<string, string> req = new Dictionary<string, string>();
        foreach (var entry in fieldList)
        {
            //if entry hasn't changed
            if (entry.isOriginalInput())
            {
                continue;
            }
            //confirm input
            entry.changeInput();
            switch (entry.type)
            {
                //get from all input fields (if multiple of one field one will overwrite rest)
                case Fields.email:
                    instance.SetEmail(entry.input.text);
                    req["email"] = entry.input.text;
                    break;
                case Fields.username:
                    instance.SetUserName(entry.input.text);
                    req["username"] = entry.input.text;
                    break;
                case Fields.realname:
                    instance.SetRealName(entry.input.text);
                    req["name"] = entry.input.text;
                    break;
            }
        }

        //not implemented
        instance.BackendCall(req);
        AnimDetermine();
    }

    
}
