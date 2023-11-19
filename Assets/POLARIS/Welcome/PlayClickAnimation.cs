using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayClickAnimation : MonoBehaviour
{
    Animator ani;
    int touchCount = 0;
    private void Start()
    {
        ani = GetComponent<Animator>(); 
    }
    public void ClickLogo()
    {
        bool flag = ani.GetBool("isPlaying");
        if (flag) return;

        ani.SetBool("isPlaying", true);
        if(touchCount < 4)
        {
            ani.Play("Bounce");
            touchCount++;
        }
        else
        {
            ani.Play("Rocket");
            touchCount = 0;
        }
        
    }

    public void AnimationDone()
    {
        ani.SetBool("isPlaying", false);
    }
}
