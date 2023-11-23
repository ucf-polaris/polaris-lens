using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace POLARIS.Managers
{
    public class TransitionManager : MonoBehaviour
    {
        private static TransitionManager Instance;
        private IEnumerator StartTransition;
        private IEnumerator EndTransition;
        private string TransitionTo;
        private VisualElement body;
        private float[] timing = { 1f, 1f };
        private float[] afterDelay = {1f, 1f};

        private float currentTiming;
        private float currentDelay;

        public VisualElement RaycastBlocker;

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

                var UIDoc = GetComponent<UIDocument>();
                UIDoc.enabled = true;
                body = UIDoc.rootVisualElement.Q<VisualElement>("body");
                body.AddToClassList("Default");

                RaycastBlocker = UIDoc.rootVisualElement.Q<VisualElement>("RaycastBlock");
            }
        }

        static public TransitionManager getInstance()
        {
            return Instance;
        }

        public void StartPlay(string Transition, Transitions start=Transitions.None, Transitions end=Transitions.None, float StartTiming = 1f, float StartDelay = 1f, float EndTiming = 1f, float EndDelay = 1f)
        {
            this.afterDelay[0] = StartDelay;
            this.afterDelay[1] = EndDelay;

            this.timing[0] = StartTiming;
            this.timing[1] = EndTiming;

            TransitionTo = Transition;

            StartTransition = ResolveTransition(start);
            EndTransition = ResolveTransition(end);
            StartCoroutine(PlayTransition());
        }

        private void ResetStyle()
        {
            body.style.opacity = StyleKeyword.Null;
            body.style.translate = StyleKeyword.Null;
        }

        private IEnumerator ResolveTransition(Transitions t)
        {
            switch (t)
            {
                case Transitions.FadeIn:
                    return FadeIn();
                case Transitions.FadeOut:
                    return FadeOut();
                case Transitions.FromBottomIn:
                    return FromBottomIn();
                case Transitions.FromBottomOut:
                    return FromBottomOut();
                case Transitions.FromTopIn:
                    return FromTopIn();
                case Transitions.FromTopOut:
                    return FromTopOut();
                default:
                    return null;
            }
        }

        public IEnumerator PlayTransition()
        {
            if (string.IsNullOrEmpty(TransitionTo)) yield break;
            RaycastBlocker.style.display = DisplayStyle.Flex;
            body.RemoveFromClassList("Default");

            AsyncOperation async = SceneManager.LoadSceneAsync(TransitionTo);
            async.allowSceneActivation = false;

            currentTiming = timing[0];
            currentDelay = afterDelay[0];
            if (StartTransition != null) yield return StartCoroutine(StartTransition);

            while (!async.isDone)
            {
                if (async.progress >= 0.9f)
                {
                    async.allowSceneActivation = true;
                }
                yield return null;
            }

            currentTiming = timing[1];
            currentDelay = afterDelay[1];
            if (EndTransition != null) yield return StartCoroutine(EndTransition);

            ResetStyle();
            body.AddToClassList("Default");

            RaycastBlocker.style.display = DisplayStyle.None;

            yield return async;
        }

        #region Transitions
        private IEnumerator FadeIn()
        {

            float alpha = 0f;
            for (var t = 0.0f; t < 1.0f; t += Time.deltaTime / currentTiming)
            {
                float opacity = Mathf.Lerp(alpha, 1f, t);
                body.style.opacity = opacity;
                yield return null;
            }

            yield return new WaitForSeconds(currentDelay);
        }

        private IEnumerator FadeOut()
        {

            float alpha = 1f;
            for (var t = 0.0f; t < 1.0f; t += Time.deltaTime / currentTiming)
            {
                float opacity = Mathf.Lerp(alpha, 0f, t);
                body.style.opacity = opacity;
                yield return null;
            }

            yield return new WaitForSeconds(currentDelay);
        }

        private IEnumerator FromBottomIn()
        {
            float original = 100f;
            for (var t = 0.0f; t < 1.0f; t += Time.deltaTime / currentTiming)
            {
                float translate = Mathf.Lerp(original, 0f, t);
                body.style.translate = new Translate(0, Length.Percent(translate));
                yield return null;
            }
            yield return new WaitForSeconds(currentDelay);
        }

        private IEnumerator FromBottomOut()
        {
            float original = 0f;
            for (var t = 0.0f; t < 1.0f; t += Time.deltaTime / currentTiming)
            {
                float translate = Mathf.Lerp(original, 100f, t);
                body.style.translate = new Translate(0, Length.Percent(translate));
                yield return null;
            }

            yield return new WaitForSeconds(currentDelay);
        }

        private IEnumerator FromTopIn()
        {

            float original = -100f;
            for (var t = 0.0f; t < 1.0f; t += Time.deltaTime / currentTiming)
            {
                float translate = Mathf.Lerp(original, 0f, t);
                body.style.translate = new Translate(0, Length.Percent(translate));
                yield return null;
            }

            yield return new WaitForSeconds(currentDelay);
        }

        private IEnumerator FromTopOut()
        {
            float original = 0f;
            for (var t = 0.0f; t < 1.0f; t += Time.deltaTime / currentTiming)
            {
                float translate = Mathf.Lerp(original, -100f, t);
                body.style.translate = new Translate(0, Length.Percent(translate));
                yield return null;
            }

            yield return new WaitForSeconds(currentDelay);
        }
        #endregion
    }

    public enum Transitions
    {
        None,
        FadeIn,
        FadeOut,
        FromBottomIn,
        FromBottomOut,
        FromTopIn,
        FromTopOut
    }
}

