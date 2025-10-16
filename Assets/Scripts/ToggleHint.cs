using System.Xml;
using UnityEngine;
using UC;

public class ToggleHint : MonoBehaviour
{
    RectTransform   rectTransform;
    bool            open = false;

    Tweener.BaseInterpolator tweenAnimation;

    void Start()
    {
        rectTransform = transform as RectTransform;
    }

    public void Toggle()
    {
        if (tweenAnimation != null)
        {
            if (!tweenAnimation.isFinished) return;
        }

        if (open)
        {
            tweenAnimation = rectTransform.Move(new Vector3(180.0f, 0.0f, 0.0f), 0.25f).EaseFunction(Ease.Sqrt);
        }
        else
        {
            tweenAnimation = rectTransform.Move(new Vector3(-180.0f, 0.0f, 0.0f), 0.25f).EaseFunction(Ease.Sqrt);
        }
        open = !open;
    }
}
