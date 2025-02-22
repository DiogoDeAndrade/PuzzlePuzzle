using System;
using UnityEngine;

public class PuzzleTile : MonoBehaviour
{
    [SerializeField] private Color          normalColor = Color.white;
    [SerializeField] private Color          highlightColor = Color.white;
    [SerializeField] private SpriteRenderer imageSpriteRenderer;

    public Vector2Int gridPos;

    Vector2         tileSize;
    Vector2         offset;
    Puzzle          owner;
    SpriteRenderer  baseSpriteRenderer;

    private void Start()
    {
        owner = GetComponentInParent<Puzzle>();
        baseSpriteRenderer = GetComponent<SpriteRenderer>();

        tileSize = owner.tileSize;
        offset = owner.worldOffset;
    }

    private void Update()
    {        
        if (owner.inputEnabled)
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
    }

    public void MoveTo(Vector2Int gridPos, float time)
    {
        var targetPos = GetWorldPos(gridPos);

        transform.MoveToWorld(targetPos, time, "TileMove").EaseFunction(Ease.Sqrt);

        this.gridPos = gridPos;
    }

    Vector3 GetWorldPos(Vector2Int gridPos)
    {
        return new Vector3((gridPos.x + 0.5f) * tileSize.x + offset.x, (gridPos.y + 0.5f) * tileSize.y + offset.y, 0.0f);
    }

    Rect GetWorldRect()
    {
        return new Rect(transform.position.x - tileSize.x * 0.5f, transform.position.y - tileSize.y * 0.5f, tileSize.x, tileSize.y);
    }

    public void SetImage(Sprite sprite)
    {
        if (imageSpriteRenderer == null) return;
        imageSpriteRenderer.enabled = (sprite != null);
        imageSpriteRenderer.sprite = sprite;
    }
}
