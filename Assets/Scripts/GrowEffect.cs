using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GrowEffect : MonoBehaviour
{
    [SerializeField]
    private List<Sprite>    sprites;
    [SerializeField]
    private Gradient        colors;
    [SerializeField]
    private Vector2         spawnRect;
    [SerializeField]
    private float           maxAlpha = 0.25f;
    [SerializeField]
    private Vector2         maxRotationSpeed = new Vector2(-90.0f, 90.0f);

    SpriteRenderer  spriteRenderer;
    float           _rotationSpeed;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.color = colors.Evaluate(Random.Range(0.0f, 1.0f)).ChangeAlpha(0.0f);
        spriteRenderer.sprite = sprites.Random();

        transform.localScale = Vector3.zero;

        RunAnimation();
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = transform.rotation * Quaternion.Euler(0, 0, _rotationSpeed * Time.deltaTime);
    }

    void RunAnimation()
    {
        StartCoroutine(RunAnimationCR());
    }

    IEnumerator RunAnimationCR()
    {
        yield return new WaitForSeconds(Random.Range(0.0f, 20.0f));

        _rotationSpeed = maxRotationSpeed.Random();

        spriteRenderer.color = colors.Evaluate(Random.Range(0.0f, 1.0f)).ChangeAlpha(0.0f);
        spriteRenderer.sprite = sprites.Random();

        Vector3 newPos = spawnRect.RandomXY() - spawnRect * 0.5f;
        newPos.z = Random.Range(-0.1f, 0.1f);
        transform.position = newPos;
        transform.localScale = Vector3.zero;

        spriteRenderer.FadeTo(spriteRenderer.color.ChangeAlpha(maxAlpha), 5.0f).
            Done(() =>
            {
                spriteRenderer.FadeTo(spriteRenderer.color.ChangeAlpha(0.0f), 5.0f).Done(() => RunAnimation());
            });

        transform.ScaleTo(4.0f * Vector3.one, 10.0f);
    }
}
