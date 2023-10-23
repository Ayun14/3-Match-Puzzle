using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Fade : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private float solidAlpha = 1f;
    [SerializeField] private float clearAlpha = 0f;
    [SerializeField] private float timeToFade = 1f;

    private void Start()
    {
        _image.enabled = true;
    }

    public void FadeOn()
    {
        StartCoroutine(FadeRoutine(0, 1));
    }

    public void FadeOff()
    {
        StartCoroutine(FadeRoutine(1, 0));
    }

    private IEnumerator FadeRoutine(int a, int goleA)
    {
        _image.enabled = true;
        Color startColor = _image.color;
        _image.color = new Color(startColor.r, startColor.g, startColor.b, a);

        Tween tween = _image.DOFade(goleA, 0.7f);
        yield return new WaitForSeconds(0.6f);
    }
}
