using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using TMPro;

public class Puzzle : MonoBehaviour
{
    [SerializeField, Header("Puzzle Parameters")] 
    private PuzzleType      puzzleType = PuzzleType.Sliding;
    [SerializeField] 
    private Vector2Int      gridSize = new Vector2Int(2, 2);
    [SerializeField]
    private bool            shuffle;
    [SerializeField, ShowIf(nameof(shuffle))]
    private int             shuffleAmmount = 3;
    [SerializeField]
    private bool            randomSeed;
    [SerializeField, HideIf(nameof(randomSeed))]
    private int             seed;
    [SerializeField, Header("Prefabs")] 
    private PuzzleTile      baseTilePrefab;
    [SerializeField, Header("Visuals")]
    private SpriteRenderer  puzzleBackground;
    [SerializeField]
    private TextMeshProUGUI solutionText;

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

    Vector2                 tileSize;
    Vector2                 offset;
    PuzzleState             currentState;
    PuzzleTile[,]           currentTiles;
    List<PuzzleState>       prevStates;
    List<SolutionElement>   solution;
    System.Random           randomGenerator;


    void Start()
    {
        Build();
    }

    void Update()
    {
        
    }

    [Button("Build")]
    void Build()
    {
        randomGenerator = new System.Random();
        if (!randomSeed) randomGenerator = new System.Random(seed);

        tileSize = Vector2.zero;
        var sr = baseTilePrefab.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("Base tile needs a sprite renderer at root level");
            return;
        }
        tileSize = sr.bounds.size;
        offset = new Vector2(-gridSize.x * tileSize.x * 0.5f, -gridSize.y * tileSize.y * 0.5f);

        if (puzzleBackground)
        {
            puzzleBackground.size = new Vector2(tileSize.x * gridSize.x + 8, tileSize.y * gridSize.y + 8);
        }

        currentState = new PuzzleState(puzzleType, gridSize);
        currentState.Identity();

        if ((puzzleType & PuzzleType.Sliding) != 0)
        {
            // Remove a piece from the puzzle
            int rx = (gridSize.x % 2 != 0) ? (Mathf.FloorToInt(gridSize.x * 0.5f)) : 0;
            int ry = (gridSize.y % 2 != 0) ? (Mathf.FloorToInt(gridSize.y * 0.5f)) : gridSize.y - 1;

            currentState.Clear(rx, ry);
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
                    var gridPos = currentState.GetRandomGridPos(randomGenerator, true);
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

        currentTiles = new PuzzleTile[gridSize.x, gridSize.y];
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                if (currentState.HasElement(x, y))
                {
                    currentTiles[x, y] = Instantiate(baseTilePrefab, transform);
                    currentTiles[x, y].transform.position = new Vector3(x * tileSize.x + offset.x + tileSize.x * 0.5f, y * tileSize.y + offset.y + tileSize.y * 0.5f, 0.0f);
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
}
