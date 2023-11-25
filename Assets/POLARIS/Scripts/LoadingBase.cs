using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using POLARIS.Managers;
using System.Linq;
using System;

namespace POLARIS.MainScene
{
    public abstract class LoadingBase : MonoBehaviour
    {
        protected string LoadingMessage = "Loading";
        protected bool OrOnFail = false;
        protected virtual IEnumerator showErrorMessage(LoadingWindow window)
        {
            yield break;
        }

        //checks if current state (defined by class) is in accept list
        protected virtual bool CheckFunction(BaseManager.CallStatus[] acceptList, LoadingWindow window, bool or=false)
        {
            return true;
        }

        protected virtual void StartLoop(LoadingWindow window)
        {
            //if already done, don't load this
            if (CheckFunction(new BaseManager.CallStatus[] { BaseManager.CallStatus.Succeeded }, window))
            {
                OnStop(window);
                return;
            }

            //startup text load
            if (window.loadingText != null) StopCoroutine(window.loadingText);
            window.loadingText = LoadingText(window);
            StartCoroutine(window.loadingText);

            if (window.loopingAnimation != null) StopCoroutine(window.loopingAnimation);
            window.loopingAnimation = LoopingAnimation(window);
            StartCoroutine(window.loopingAnimation);
        }

        //enumerator to play animation
        protected IEnumerator LoopingAnimation(LoadingWindow window)
        {
            //deep copy of list to reverse
            List<LoadingPiece> rev = window.Pieces.Select(piece => piece).ToList();
            rev.Reverse();

            bool isIn = true;

            //while endpoint(s) is not success
            while (CheckFunction(new BaseManager.CallStatus[] { BaseManager.CallStatus.Failed, BaseManager.CallStatus.NotStarted, BaseManager.CallStatus.InProgress}, window, true))
            {
                //if you've failed the call or the call hasn't started (it reset), show error
                if ((CheckFunction(new BaseManager.CallStatus[] { BaseManager.CallStatus.Failed, BaseManager.CallStatus.NotStarted}, window, OrOnFail) && window.showError == null))
                {
                    window.showError = showErrorMessage(window);
                    StartCoroutine(window.showError);
                }

                yield return new WaitForSeconds(0.5f);

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
            if (CheckFunction(new BaseManager.CallStatus[] { BaseManager.CallStatus.Succeeded}, window))
            {
                OnStop(window);
            }

            window.loopingAnimation = null;
        }

        protected virtual void OnStop(LoadingWindow window)
        {
            Debug.Log("IN BASE");
            StopLoop(window);
        }

        //move the elipses while scan is going on
        protected IEnumerator LoadingText(LoadingWindow window)
        {
            window.loadingLabel.text = LoadingMessage;
            string[] names = { LoadingMessage, LoadingMessage + ".", LoadingMessage + "..", LoadingMessage + "..." };
            int index = 0;
            while (CheckFunction(new BaseManager.CallStatus[] { BaseManager.CallStatus.Failed, BaseManager.CallStatus.NotStarted, BaseManager.CallStatus.InProgress }, window))
            {
                yield return new WaitForSeconds(0.5f);
                window.loadingLabel.text = names[index];
                index += 1;
                index %= names.Length;
            }

            window.loadingText = null;
        }

        protected void StopLoop(LoadingWindow window)
        {
            //if failed, stop looping animation and loading text animation
            if (window.loopingAnimation != null)
            {
                StopCoroutine(window.loopingAnimation);
                window.loopingAnimation = null;
            }

            if (window.loadingText != null)
            {
                StopCoroutine(window.loadingText);
                window.loadingText = null;
            }
        }
    }

    public enum Dimension
    {
        Width,
        Height
    }

    [Serializable]
    public class LoadingWindow
    {
        public VisualElement container;
        public VisualElement animationContainer;
        public Label loadingLabel;
        public Label errorLabel;
        public IEnumerator loopingAnimation;
        public IEnumerator loadingText;
        public IEnumerator showError;
        public IEnumerator outwardAnimation;
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
            Pieces.Add(new LoadingPiece(container.Q<VisualElement>("7"), Dimension.Width, 72, 0.5f));

            loadingLabel = container.Q<Label>("LoadingLabel");
            errorLabel = container.Q<Label>("ErrorLabel");
            animationContainer = container.Q<VisualElement>("LoadingAnimation");
        }
    }
    [Serializable]
    public class LoadingPiece
    {
        public VisualElement piece;
        public Dimension dim;
        public float originalSize;
        public float animationDuration;

        public LoadingPiece(VisualElement piece, Dimension dim, float originalSize, float animationDuration = 0.4f)
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
            if (dim == Dimension.Height)
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
