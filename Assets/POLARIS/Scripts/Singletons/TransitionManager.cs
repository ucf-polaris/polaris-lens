using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace POLARIS.Managers
{
    public class TransitionManager : MonoBehaviour
    {
        private static TransitionManager Instance;
        private TransitionBase transition;
        private GameObject ActiveGameObject;

        void Awake()
        {
            //create singleton
            if (Instance != this && Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                DontDestroyOnLoad(gameObject);
                Instance = this;
            }
        }

        static public TransitionManager getInstance()
        {
            return Instance;
        }

        public void Upwards(string transitionTo)
        {
            transition = GetComponent<Transition_Upwards>();
            transition.Initialize("", transitionTo);
            StartCoroutine(transition.PlayTransition());
        }
    }
}

