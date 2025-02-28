using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FireflyEffect : MonoBehaviour
{
    [SerializeField]
    private Gradient        colors;
    [SerializeField]
    private Vector2         spawnRect;
    [SerializeField]
    private float           maxAlpha = 0.25f;
    [SerializeField]
    private Vector2         maxSpeed = new Vector2(10.0f, 50.0f);

    TrailRenderer   trailRenderer;
    Vector3         deltaVector;

    void Start()
    {
        trailRenderer = GetComponentInChildren<TrailRenderer>();
        trailRenderer.emitting = false;

        RunAnimation();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += deltaVector * Time.deltaTime;
    }

    void RunAnimation()
    {
        StartCoroutine(RunAnimationCR());
    }

    IEnumerator RunAnimationCR()
    {
        yield return new WaitForSeconds(Random.Range(0.0f, 20.0f));

        trailRenderer.startColor = colors.Evaluate(Random.Range(0.0f, 1.0f)).ChangeAlpha(0.0f);
        trailRenderer.endColor = colors.Evaluate(Random.Range(0.0f, 1.0f)).ChangeAlpha(0.0f);

        deltaVector = Random.insideUnitCircle.normalized;

        Vector3 newPos = -deltaVector * spawnRect * 0.75f;
        newPos.z = Random.Range(-0.1f, 0.1f);
        transform.position = newPos;
        trailRenderer.emitting = false;

        deltaVector *= maxSpeed.Random();

        trailRenderer.FadeTo(maxAlpha, 0.0f, 1.0f).
            Done(() =>
            {
                trailRenderer.FadeTo(0.0f, 0.0f, 1.0f).DelayStart(5.0f).Done(() => RunAnimation()); 
            });

        yield return null;

        trailRenderer.emitting = true;
    }
}
