using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using POLARIS.Managers;
using UnityEngine.SceneManagement;

public class Login_Animation : MonoBehaviour
{
    public GameObject LoadingUI;
    public bool ExpandOnFinish = true;
    private NonManagerEndpoint NME;

    private void Start()
    {
        LoadingUI = Login_Loading.instance?.gameObject;
        GetComponent<Animator>().SetBool("Expand", ExpandOnFinish);
        NME = GetComponent<NonManagerEndpoint>();
    }
    public void TransitionToUI()
    {
        if (LoadingUI != null)
        {
            LoadingUI.GetComponent<Login_Loading>().TurnOn();
        }
        gameObject.SetActive(false);
    }

    public void ActivateBlocker()
    {
        if (LoadingUI != null)
        {
            LoadingUI.transform.GetChild(0).gameObject.SetActive(true);
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.enabled = false;
        }
    }

    public void DeactivateUI()
    {
        if(LoadingUI != null) LoadingUI.SetActive(false);
    }

    public void ToggleUI()
    {
        if (LoadingUI != null) LoadingUI.SetActive(!LoadingUI.activeSelf);
    }

    public void ProperReset()
    {
        NME.CurrentState = EndpointState.NotStarted;
        //disable raycast blocker
        if (LoadingUI != null)
        {
            LoadingUI.transform.GetChild(0).gameObject.SetActive(false);
        }

        //disable user from being able to interact with anything
        EventSystem eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();
        if (eventSystem != null)
        {
            eventSystem.enabled = true;
        }
    }

    public void FinishedAnimation()
    {
        if(NME.CurrentState == EndpointState.NotVerified)
        {
            //TransitionManager.getInstance().Fade("Verify");
            SceneManager.LoadScene("Verify");
        }
        //if loginging in (and the animations associated) are still in progress, don't reset
        else if (NME.CurrentState != EndpointState.InProgress && NME.CurrentState != EndpointState.Failed)
        {
            ProperReset();
        }
    }
}
