using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using TMPro;
using UnityEngine.U2D;

public class Puzzle : MonoBehaviour
{
    [SerializeField, Header("Puzzle Parameters")] 
    private PuzzleType      puzzleType = PuzzleType.Sliding;
    [SerializeField] 
    private Vector2Int      gridSize = new Vector2Int(2, 2);
    [SerializeField, ShowIf(nameof(isSliding))]
    private int             unmoveablePieceCount;
    [SerializeField]
    private bool            shuffle;
    [SerializeField, ShowIf(nameof(shuffle))]
    private int             shuffleAmmount = 3;
    [SerializeField]
    private bool            randomSeed;
    [SerializeField, HideIf(nameof(randomSeed))]
    private int             seed;
    [SerializeField, Header("Interaction")]
    private float           interactionCooldown = 0.5f;
    [SerializeField]
    private float           animationTime = 0.25f;
    [SerializeField, Header("Tiles")] 
    private PuzzleTile      baseTilePrefab;
    [SerializeField]
    private Texture2D       baseImage;
    [SerializeField, Header("References")]
    private SpriteRenderer  puzzleBackground;
    [SerializeField]
    private TextMeshProUGUI solutionText;
    [SerializeField]
    private Camera          _mainCamera;


    class PuzzleElement
    {        
    }

    class SolutionElement
    {
        public enum ActionType { Move, Rotate };

        public ActionType       action;
        public Vector2Int       start;
        public Vector2Int       end;
    }

    Vector2                 _tileSize;
    Vector2                 _worldOffset;
    PuzzleState             currentState;
    PuzzleTile[,]           currentTiles;
    List<PuzzleState>       prevStates;
    List<SolutionElement>   solution;
    System.Random           randomGenerator;
    float                   interactionTimer;
    bool                    completed = false;

    public Vector2 tileSize => _tileSize;
    public Vector2 worldOffset => _worldOffset;
    public bool inputEnabled => (interactionTimer <= 0.0f);
    public Camera mainCamera => _mainCamera;

    bool isSliding => (puzzleType & PuzzleType.Sliding) != 0;

    void Start()
    {
        Build();
    }

    void Update()
    {
        if (completed)
        {
            return;
        }

        if (interactionTimer > 0.0f)
        {
            interactionTimer -= Time.deltaTime;
        }
        else
        {
            if (currentState.CheckSolution())
            {
                Debug.Log("Puzzle completed!");
                completed = true;
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                HandleLeftClick();
            }
        }
    }

    void HandleLeftClick()
    {
        if (!GetMouseGridPos(out var gridPos)) return;

        if (currentState.GetImmoveable(gridPos.x, gridPos.y)) return;

        if (currentState.GetEmptyNeighbour(gridPos, out var neighbour))
        {
            // Ok sound

            // Get actual object
            currentTiles[gridPos.x, gridPos.y].MoveTo(neighbour, animationTime);
            currentTiles[neighbour.x, neighbour.y] = currentTiles[gridPos.x, gridPos.y];
            currentTiles[gridPos.x, gridPos.y] = null;
            currentState.Swap(gridPos, neighbour);
        }
        else
        {
            // Bad sound
        }

        interactionTimer = interactionCooldown;
    }

    bool GetMouseGridPos(out Vector2Int gridPos)
    {
        var worldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                Rect rect = new Rect(x * _tileSize.x + _worldOffset.x, y * _tileSize.y + _worldOffset.y, _tileSize.x, _tileSize.y);
                if (rect.Contains(worldPos))
                {
                    gridPos = new Vector2Int(x, y);
                    return true;
                }
            }
        }
        gridPos = Vector2Int.zero;
        return false;
    }

    [Button("Build")]
    void Build()
    {
        randomGenerator = new System.Random();
        if (!randomSeed) randomGenerator = new System.Random(seed);

        _tileSize = Vector2.zero;
        var sr = baseTilePrefab.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("Base tile needs a sprite renderer at root level");
            return;
        }
        _tileSize = sr.bounds.size;
        _worldOffset = new Vector2(-gridSize.x * _tileSize.x * 0.5f, -gridSize.y * _tileSize.y * 0.5f);

        if (puzzleBackground)
        {
            puzzleBackground.size = new Vector2(_tileSize.x * gridSize.x + 8, _tileSize.y * gridSize.y + 8);
        }

        currentState = new PuzzleState(puzzleType, gridSize, baseImage != null);
        currentState.Identity();

        if ((puzzleType & PuzzleType.Sliding) != 0)
        {
            for (int i = 0; i < unmoveablePieceCount; i++)
            {
                var p = gridSize.RandomXY(randomGenerator);

                currentState.SetImmoveable(p.x, p.y, true);
            }

            // Remove a piece from the puzzle
            if (baseImage)
            {
                // Remove a piece from the puzzle
                var clearPiece = gridSize.RandomXY(randomGenerator);

                currentState.Clear(clearPiece.x, clearPiece.y);
            }
            else
            {
                int rx = (gridSize.x % 2 != 0) ? (Mathf.FloorToInt(gridSize.x * 0.5f)) : 0;
                int ry = (gridSize.y % 2 != 0) ? (Mathf.FloorToInt(gridSize.y * 0.5f)) : gridSize.y - 1;

                currentState.Clear(rx, ry);
            }
        }

        if (shuffle)
        {
            Shuffle();
        }

        CreatePieces();
    }

    void Shuffle()
    {
        solution = new();
        // Start from initial state
        prevStates = new() { currentState.Clone() };

        if ((puzzleType & PuzzleType.Sliding) != 0)
        {
            for (int i = 0; i < shuffleAmmount; i++)
            {
                int nTries = 0;
                while (nTries < 10)
                {
                    var gridPos = currentState.GetRandomGridPos(randomGenerator, true, false);
                    currentState.GetEmptyNeighbour(gridPos, out var neighbour);

                    currentState.Swap(gridPos, neighbour);

                    // Check if state already exists in previous states
                    bool alreadySeen = false;
                    foreach (var prevState in prevStates)
                    {
                        if (prevState.IsSame(currentState))
                        {
                            alreadySeen = true;
                            break;
                        }
                    }
                    if (alreadySeen)
                    {
                        // This state has already existed, we need to undo what we did and try again
                        currentState.Swap(gridPos, neighbour);

                        nTries++;
                        continue;
                    }

                    prevStates.Add(currentState.Clone());

                    SolutionElement solutionElement = new()
                    {
                        action = SolutionElement.ActionType.Move,
                        start = neighbour,
                        end = gridPos,
                    };

                    solution.Add(solutionElement);

                    break;
                }
            }
        }

        solution.Reverse();

        if (solutionText)
        {
            var st = "";
            foreach (var s in solution)
            {
                switch (s.action)
                {
                    case SolutionElement.ActionType.Move:
                        st += $"Move from {s.start.x},{s.start.y} to {s.end.x},{s.end.y}\n";
                        break;
                    case SolutionElement.ActionType.Rotate:
                        break;
                    default:
                        break;
                }
            }

            solutionText.text = st;
        }
    }



    void CreatePieces()
    {
        Clear();

        int index = 0;
        currentTiles = new PuzzleTile[gridSize.x, gridSize.y];
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                if (currentState.HasElement(x, y))
                {
                    currentTiles[x, y] = Instantiate(baseTilePrefab, transform);
                    currentTiles[x, y].gridPos = new Vector2Int(x, y);
                    currentTiles[x, y].name = $"Piece {index++}";
                    currentTiles[x, y].transform.position = new Vector3(x * _tileSize.x + _worldOffset.x + _tileSize.x * 0.5f, y * _tileSize.y + _worldOffset.y + _tileSize.y * 0.5f, 0.0f);
                    currentTiles[x, y].SetImmoveable(currentState.GetImmoveable(x, y));
                    if (baseImage)
                    {
                        var op = currentState.GetOriginalPosition(x, y);
                        float tw = tileSize.x;
                        float th = tileSize.y;
                        Rect uv = new Rect(op.x * tw, op.y * th, tw, th);

                        Sprite sprite = Sprite.Create(baseImage, uv, new Vector2(0.5f, 0.5f), 1.0f);
                        sprite.name = $"Custom Sprite ({baseImage.name}: {uv})";

                        currentTiles[x, y].SetImage(sprite);
                    }
                    else
                    {
                        currentTiles[x, y].SetImage(null);
                    }
                }
            }
        }
    }

    [Button("Clear")]
    void Clear()
    {
        List<GameObject> toDelete = new();
        var puzzleTiles = GetComponentsInChildren<PuzzleTile>();
        foreach (var pt in puzzleTiles)
        {
            toDelete.Add(pt.gameObject);
        }

        foreach (var obj in toDelete)
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
#else
            Destroy(obj);
#endif
        }
    }

    [Button("List Pieces")]
    void ListPieces()
    {
        string str = "";
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                if (currentTiles[x, y] == null) str += $"({x}, {y}) = NULL\n";
                else str += $"({x}, {y}) = {currentTiles[x, y].name}\n";
            }
        }

        Debug.Log(str);
    }
}
