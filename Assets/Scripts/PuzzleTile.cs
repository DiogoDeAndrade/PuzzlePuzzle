using NaughtyAttributes.Test;
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
    [SerializeField] private SpriteRenderer pipeSprite;
    [SerializeField] private Sprite[]       pipeSprites;


    public Vector2Int gridPos;

    Vector2         tileSize;
    Vector2         offset;
    Puzzle          owner;
    SpriteRenderer  baseSpriteRenderer;
    bool            immoveable = false;
    Vector2Int      originalGridPos;
    int             pipeType = -1;
    bool            full;
    int             pipeRotation = 0;

    private void Start()
    {
        owner = GetComponentInParent<Puzzle>();
        baseSpriteRenderer = GetComponent<SpriteRenderer>();
        if (immoveableSprite) immoveableSprite.enabled = immoveable;

        tileSize = owner.tileSize;
        offset = owner.worldOffset;
        originalGridPos = gridPos;

        UpdatePipe(false);
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

    // Rotates 90 degrees counter-clockwise
    public Tweener.BaseInterpolator Rotate(float time, bool counterclockwise = true)
    {
        var targetRotation = transform.rotation * Quaternion.Euler(0, 0, (counterclockwise) ? (90) : (-90));

        var ret = transform.RotateTo(targetRotation, time, "TileRotate").EaseFunction(Ease.Sqrt);

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

    public void SetPipe(int pipeType, int rotation)
    {
        this.pipeType = pipeType;
        this.pipeRotation = rotation;        
        UpdatePipe(true);
    }

    public void SetFull(bool full)
    {
        this.full = full;
        UpdatePipe(false);
    }

    public bool IsFull() => full;

    void UpdatePipe(bool updateRotation)
    {
        if (pipeSprite == null) return;
        pipeSprite.enabled = (pipeType >= 0);
        int offset = (full) ? (4) : (0);
        if (pipeType >= 0) pipeSprite.sprite = pipeSprites[pipeType + offset];

        if (updateRotation)
            pipeSprite.transform.localRotation = Quaternion.Euler(0, 0, pipeRotation * 90.0f);
    }
}
