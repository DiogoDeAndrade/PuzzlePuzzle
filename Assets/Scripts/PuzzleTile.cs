using System;
using UnityEngine;

public class PuzzleTile : MonoBehaviour
{
    [SerializeField] private Color          normalColor = Color.white;
    [SerializeField] private Color          immoveableColor = Color.white;
    [SerializeField] private Color          highlightColor = Color.white;
    [SerializeField] private SpriteRenderer imageSpriteRenderer;
    [SerializeField] private SpriteRenderer lightSprite;
    [SerializeField] private Sprite         lightOnSprite;
    [SerializeField] private Sprite         lightOffSprite;
    [SerializeField] private SpriteRenderer immoveableSprite;


    public Vector2Int gridPos;

    Vector2         tileSize;
    Vector2         offset;
    Puzzle          owner;
    SpriteRenderer  baseSpriteRenderer;
    bool            immoveable = false;
    Vector2Int      originalGridPos;

    private void Start()
    {
        owner = GetComponentInParent<Puzzle>();
        baseSpriteRenderer = GetComponent<SpriteRenderer>();
        if (immoveableSprite) immoveableSprite.enabled = immoveable;

        tileSize = owner.tileSize;
        offset = owner.worldOffset;
        originalGridPos = gridPos;
    }

    private void Update()
    {
        var currentState = owner.GetCurrentState();

        if (immoveable)
        {
            baseSpriteRenderer.color = immoveableColor;
        }
        else if ((owner.inputEnabled) && (!owner.isComplete))
        {
            var worldPos = owner.mainCamera.ScreenToWorldPoint(Input.mousePosition);
            if (GetWorldRect().Contains(worldPos))
            {
                baseSpriteRenderer.color = highlightColor;
            }
            else
            {
                baseSpriteRenderer.color = normalColor;
            }
        }
        else
        {
            baseSpriteRenderer.color = normalColor;
        }

        lightSprite.enabled = owner.isLightsOut;
        lightSprite.sprite = (currentState.isLightOn(gridPos.x, gridPos.y)) ? (lightOnSprite) : (lightOffSprite);
    }

    public Tweener.BaseInterpolator MoveTo(Vector2Int gridPos, float time)
    {
        var targetPos = GetWorldPos(gridPos);

        var ret = transform.MoveToWorld(targetPos, time, "TileMove").EaseFunction(Ease.Sqrt);

        this.gridPos = gridPos;

        return ret;
    }

    Vector3 GetWorldPos(Vector2Int gridPos)
    {
        return new Vector3((gridPos.x + 0.5f) * tileSize.x + offset.x, (gridPos.y + 0.5f) * tileSize.y + offset.y, 0.0f);
    }

    Rect GetWorldRect()
    {
        return new Rect(transform.position.x - tileSize.x * 0.5f, transform.position.y - tileSize.y * 0.5f, tileSize.x, tileSize.y);
    }

    public void SetImmoveable(bool b)
    {
        immoveable = b;
        if (immoveableSprite) immoveableSprite.enabled = immoveable;
    }

    public void SetImage(Sprite sprite)
    {
        if (imageSpriteRenderer == null) return;
        imageSpriteRenderer.enabled = (sprite != null);
        imageSpriteRenderer.sprite = sprite;
    }
}
