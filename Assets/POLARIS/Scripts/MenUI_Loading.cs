using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using POLARIS.Managers;
using System.Linq;

namespace POLARIS.MainScene
{
    public class MenUI_Loading : MonoBehaviour
    {
        LocationManager locationManager;
        EventManager eventManager;
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
        private void Update()
        {
            
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
        //play animation
        private void StartLoop(LoadingWindow window)
        {
            //if already done, don't load this
            if (CheckStatus(window.isLocation) == BaseManager.CallStatus.Succeeded)
            {
                window.container.style.display = DisplayStyle.None;
                return;
            }

            if (window.loopingAnimation != null) StopCoroutine(window.loopingAnimation);
            window.loopingAnimation = LoopingAnimation(window);
            StartCoroutine(window.loopingAnimation);

            //startup text load
            if (window.loadingText != null) StopCoroutine(window.loadingText);
            window.loadingText = LoadingText(window);
            StartCoroutine(window.loadingText);
        }

        //move the elipses while scan is going on
        private IEnumerator LoadingText(LoadingWindow window)
        {
            Debug.Log("Is called: LoadingText");
            window.loadingLabel.text = "Loading";
            string[] names = { "Loading", "Loading.", "Loading..", "Loading..." };
            int index = 0;
            while (CheckStatus(window.isLocation) != BaseManager.CallStatus.Succeeded)
            {
                yield return new WaitForSeconds(0.5f);
                window.loadingLabel.text = names[index];
                index += 1;
                index %= names.Length;
            }

            window.loadingText = null;
        }

        //enumerator to play animation
        private IEnumerator LoopingAnimation(LoadingWindow window)
        {
            List<LoadingPiece> rev = window.Pieces.Select(book => book).ToList();
            rev.Reverse();
            bool isIn = true;
            Debug.Log("Is called: LoopingAnimation");

            //while endpoint is still being called (our dynamoDB endpoints)
            while (CheckStatus(window.isLocation) != BaseManager.CallStatus.Succeeded)
            {
                if ((CheckStatus(window.isLocation) == BaseManager.CallStatus.Failed || CheckStatus(window.isLocation) == BaseManager.CallStatus.NotStarted) && window.showError == null)
                {
                    window.showError = showErrorMessage(window);
                    StartCoroutine(window.showError);
                }

                yield return new WaitForSeconds(2f);

                if (isIn)
                {
                    //pop out of existance
                    foreach (var pie in rev)
                    {
                        pie.Out();
                        yield return new WaitForSeconds(pie.animationDuration);
                    }

                    isIn = false;
                }
                else
                {
                    //pop into existance
                    foreach (var pie in window.Pieces)
                    {
                        pie.In();
                        yield return new WaitForSeconds(pie.animationDuration);
                    }
                    isIn = true;
                }
            }

            if (window.loadingText != null) StopCoroutine(window.loadingText);

            //on success
            if (CheckStatus(window.isLocation) == BaseManager.CallStatus.Succeeded)
            {
                window.container.style.display = DisplayStyle.None;
            }

            window.loopingAnimation = null;
        }

        //show error message for 3 seconds then start another loop
        private IEnumerator showErrorMessage(LoadingWindow window)
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
    }

    public enum Dimension
    {
        Width,
        Height
    }

    class LoadingWindow
    {
        public VisualElement container;
        public Label loadingLabel;
        public Label errorLabel;
        public IEnumerator loopingAnimation;
        public IEnumerator loadingText;
        public IEnumerator showError;
        public bool isLocation;
        public bool isDone;

        public List<LoadingPiece> Pieces = new List<LoadingPiece>();     

        public LoadingWindow(VisualElement container, bool isLocation)
        {
            this.isLocation = isLocation;
            this.container = container;
            container.style.display = DisplayStyle.Flex;

            //gather all 7 pieces
            Pieces.Add(new LoadingPiece(container.Q<VisualElement>("Center"), Dimension.Height, 251, 0.5f));
            Pieces.Add(new LoadingPiece(container.Q<VisualElement>("1"), Dimension.Width, 72));
            Pieces.Add(new LoadingPiece(container.Q<VisualElement>("2"), Dimension.Width, 149, 0.8f));
            Pieces.Add(new LoadingPiece(container.Q<VisualElement>("3"), Dimension.Height, 81));
            Pieces.Add(new LoadingPiece(container.Q<VisualElement>("4"), Dimension.Height, 109, 0.8f));
            Pieces.Add(new LoadingPiece(container.Q<VisualElement>("5"), Dimension.Height, 81));
            Pieces.Add(new LoadingPiece(container.Q<VisualElement>("6"), Dimension.Width, 149, 0.8f));
            Pieces.Add(new LoadingPiece(container.Q<VisualElement>("7"), Dimension.Width, 72));

            loadingLabel = container.Q<Label>("LoadingLabel");
            errorLabel = container.Q<Label>("ErrorLabel");
        }
    }

    class LoadingPiece
    {
        public VisualElement piece;
        public Dimension dim;
        public float originalSize;
        public float animationDuration;

        public LoadingPiece(VisualElement piece, Dimension dim, float originalSize, float animationDuration=0.4f)
        {
            this.animationDuration = animationDuration;
            this.dim = dim;
            this.piece = piece;
            //had to hard code getting the original size. piece.style.width was always returning 0
            this.originalSize = originalSize;
            //dim = piece.style.transitionProperty.value[0].ToString() == "width"
        }

        public void Out()
        {
            if(dim == Dimension.Height)
                piece.style.height = new Length(0);
            else
                piece.style.width = new Length(0);
        }

        public void In()
        {
            if (dim == Dimension.Height)
                piece.style.height = new Length(originalSize);
            else
                piece.style.width = new Length(originalSize);
        }
    }
}

