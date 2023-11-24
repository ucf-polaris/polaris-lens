using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using POLARIS.Managers;

public class NonManagerEndpoint : MonoBehaviour
{
    protected Animator ani;
    protected UserManager instance;

    protected EndpointState _currentState = EndpointState.NotStarted;
    public EndpointState CurrentState { get => _currentState; set { if (ani != null) ani.SetInteger("State", (int)value); _currentState = value; } }

    // Start is called before the first frame update
    public void Start()
    {
        ani = GetComponent<Animator>();
        instance = UserManager.getInstance();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public enum EndpointState
{
    NotStarted,
    InProgress,
    Failed,
    Succeed,
    NotVerified
}
