using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using POLARIS.Managers;

public class LoginPermanence : MonoBehaviour
{
    UserManager userManager;
    // Start is called before the first frame update
    void Start()
    {
        userManager = UserManager.getInstance();
        userManager.CheckPermanence();
    }
}
