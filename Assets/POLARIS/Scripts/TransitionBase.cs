using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class TransitionBase : MonoBehaviour
{
    //object that is displayed between the transition
    public GameObject passoverTemplate;
    protected GameObject passoverObject;
    protected string TransitionTo;
    public bool destroyPassoverObject;
    protected GameObject ActiveGameObject;

    protected abstract IEnumerator StartTransition();
    public IEnumerator PlayTransition()
    {
        AsyncOperation async = SceneManager.LoadSceneAsync(TransitionTo);
        async.allowSceneActivation = false;
        yield return StartCoroutine(StartTransition());
        

        //place the passoverObject (prefab) as the transition screen
        if(passoverObject == null) passoverObject = Instantiate(passoverObject, gameObject.transform);

        while (!async.isDone)
        {
            if(async.progress >= 0.9f)
            {
                async.allowSceneActivation = true;
            }
            yield return null;
        }

        //destroy passover object (you don't deal with it in the transition). Otherwise you deal with it in the transition.
        if (passoverObject != null && destroyPassoverObject)
        {
            Destroy(passoverObject);
            passoverObject = null;
        }

        yield return StartCoroutine(EndTransition());

        //finally destroy and deal with passover if not destroyed by end transition
        if (passoverObject != null)
        {
            Destroy(passoverObject);
            passoverObject = null;
        }

        yield return async;
    }

    public abstract void Initialize(string obj, params string[] listParams);

    protected abstract IEnumerator EndTransition();
}
