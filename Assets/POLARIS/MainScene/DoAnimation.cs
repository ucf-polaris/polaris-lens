using UnityEngine;
using UnityEngine.UI;

public class DoAnimation : MonoBehaviour
{
    public float FadeTime;
    public float AnimTime;
    
    private Animator _animator;
    private Image _image;
    
    private static readonly int Play = Animator.StringToHash("Play");
    private static readonly int End = Animator.StringToHash("End");

    private bool _animating;
    private bool _playing;
    private float _startTime;
    private float _animationTime;

    private void Start()
    {
        _image = GetComponent<Image>();
    }

    private void Update()
    {
        if (!_animating) return;

        if (Time.time - _startTime < FadeTime)
        {
            var fadePercent = (Time.time - _startTime) / FadeTime;
            _image.color = new Color(1f, 1f, 1f, fadePercent);
        }
        else if (!_playing)
        {
            PlayAnimationForReal();
            _animationTime = Time.time;
            _playing = true;
        }
        else if (Time.time - _animationTime > AnimTime)
        {
            if (Time.time - _animationTime - AnimTime < FadeTime)
            {
                var fadePercent = (Time.time - _animationTime - AnimTime) / FadeTime;
                _image.color = new Color(1f, 1f, 1f, 1 - fadePercent);
            }
            else
            {
                StopAnimation();
            }
        }
    }

    public void PlayAnimation()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
        _animating = true;
        _startTime = Time.time;
    }

    private void PlayAnimationForReal()
    {
        _animator.SetTrigger(Play);
    }

    private void StopAnimation()
    {
        _animator.SetTrigger(End);
        _animating = false;
        _playing = false;
        gameObject.SetActive(false);
    }
}
