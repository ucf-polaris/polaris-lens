using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using POLARIS.Managers;
using System.Linq;

namespace POLARIS.MainScene
{
    public class MenUI_Loading : LoadingBase
    {
        LocationManager locationManager;
        EventManager eventManager;

        [SerializeField]
        LoadingWindow locationLoading;
        LoadingWindow eventsLoading;

        ChangeTabImage tab;
        // Start is called before the first frame update
        void Start()
        {
            tab = GetComponent<ChangeTabImage>();
            locationManager = LocationManager.getInstance();
            eventManager = EventManager.getInstance();

            //when scan succeed stop all loading
            locationManager.ScanSucceed += StopLocationLoading;
            eventManager.ScanSucceed += StopEventLoading;

            //when change tabs change which loading screen is displayed
            tab.ChangeTab += OnSwapTab;

            var UiDoc = GetComponent<UIDocument>();
            locationLoading = new LoadingWindow(UiDoc.rootVisualElement.Q<VisualElement>("LocationsLoading"), true);
            eventsLoading = new LoadingWindow(UiDoc.rootVisualElement.Q<VisualElement>("EventsLoading"), false);

            OnSwapTab(null, null);

            StartLoop(locationLoading);
            StartLoop(eventsLoading);
        }

        private void OnDisable()
        {
            if(LocationManager.getInstance() != null) locationManager.ScanSucceed -= StopLocationLoading;
            if(EventManager.getInstance() != null) eventManager.ScanSucceed -= StopEventLoading;
        }

        private void Update()
        {
            
        }
        
        override protected void OnStop(LoadingWindow window)
        {
            window.container.style.display = DisplayStyle.None;
        }

        public void OnSwapTab(object o, EventArgs e)
        {
            if (tab.MyLastPressed == "location") 
            {
                if(locationManager.ScanStatus != BaseManager.CallStatus.Succeeded) locationLoading.container.style.display = DisplayStyle.Flex;
                eventsLoading.container.style.display = DisplayStyle.None;
            }
            else
            {
                locationLoading.container.style.display = DisplayStyle.None;
                if (eventManager.ScanStatus != BaseManager.CallStatus.Succeeded) eventsLoading.container.style.display = DisplayStyle.Flex;
            }
        }
        public void StopLocationLoading(object o, EventArgs e)
        {
            if (locationLoading.loopingAnimation != null) 
            {
                StopCoroutine(locationLoading.loopingAnimation);
                locationLoading.loopingAnimation = null;
            }

            if (locationLoading.loadingText != null) 
            {
                StopCoroutine(locationLoading.loadingText);
                locationLoading.loadingText = null;
            }

            if (locationLoading.showError != null)
            {
                StopCoroutine(locationLoading.showError);
                locationLoading.showError = null;
            }
            locationLoading.container.style.display = DisplayStyle.None;
        }

        public void StopEventLoading(object o, EventArgs e)
        {
            if (eventsLoading.loopingAnimation != null)
            {
                StopCoroutine(eventsLoading.loopingAnimation);
                eventsLoading.loopingAnimation = null;
            }

            if (eventsLoading.loadingText != null)
            {
                StopCoroutine(eventsLoading.loadingText);
                eventsLoading.loadingText = null;
            }

            if (eventsLoading.showError != null)
            {
                StopCoroutine(eventsLoading.showError);
                eventsLoading.showError = null;
            }
            eventsLoading.container.style.display = DisplayStyle.None;
        }

        //show error message for 3 seconds then start another loop
        override protected IEnumerator showErrorMessage(LoadingWindow window)
        {
            Debug.Log("Is called: LoadingText");
            window.loadingLabel.style.visibility = Visibility.Hidden;
            window.errorLabel.style.visibility = Visibility.Visible;
            yield return new WaitForSeconds(3f);
            window.loadingLabel.style.visibility = Visibility.Visible;
            window.errorLabel.style.visibility = Visibility.Hidden;

            //retry call
            if (window.isLocation)
                locationManager.CallScan();
            else
                eventManager.CallScan();

            window.showError = null;
        }

        private BaseManager.CallStatus CheckStatus(bool isLocation)
        {
            if (isLocation) return locationManager.ScanStatus;
            return eventManager.ScanStatus;
        }
        override protected bool CheckFunction(BaseManager.CallStatus[] acceptList, LoadingWindow window, bool or = false)
        {
            BaseManager.CallStatus status = CheckStatus(window.isLocation);
            if (acceptList.Contains(status)) return true;
            return false;
        }
    }

    
}

