using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using TMPro;
using NUnit.Framework.Constraints;
using System.IO;
using static UnityEngine.RuleTile.TilingRuleOutput;
using UnityEngine.Audio;
using System;

public class Puzzle : MonoBehaviour
{
    [SerializeField, Header("Puzzle Parameters")] 
    private PuzzleType          puzzleType = PuzzleType.Sliding;
    [SerializeField] 
    private Vector2Int          gridSize = new Vector2Int(2, 2);
    [SerializeField]
    private bool                shuffle;
    [SerializeField, ShowIf(nameof(shuffle))]
    private int                 shuffleAmmount = 3;
    [SerializeField]
    private bool                randomSeed;
    [SerializeField, HideIf(nameof(randomSeed))]
    private int                 seed;
    [SerializeField, ShowIf(nameof(isSliding)), Header("Sliding")]
    private int                 unmoveablePieceCount;
    [SerializeField, ShowIf(nameof(isLightsOut)), Header("Lights Out")]
    private PuzzleState.NeighborhoodType    neightborhoodType = PuzzleState.NeighborhoodType.VonNeumann;
    [SerializeField, ShowIf(nameof(isLightsOut))]
    private int                 neighborhoodDistance = 1;
    [SerializeField, ShowIf(nameof(isPipemania)), Header("Pipemania")]
    private int                 numberOfOuts = 2;
    [SerializeField, ShowIf(nameof(isPipemania))]
    private int                 minPathLength = 5;    
    [SerializeField, ShowIf(nameof(isPipemania))]
    private int                 blockTiles = 10;
    [SerializeField, ShowIf(nameof(isPipemania))]
    private SpriteRenderer[]    drainPipes;
    [SerializeField, ShowIf(nameof(isPipemania))]
    private Sprite              emptyDrainPipe;
    [SerializeField, ShowIf(nameof(isPipemania))]
    private Sprite              fullDrainPipe;
    [SerializeField, ShowIf(nameof(isRhythm)), Header("Rhythm")]
    private AudioSource         musicTrackSrc;
    [SerializeField, ShowIf(nameof(isRhythm))]
    private int                 bpm = 120;
    [SerializeField, ShowIf(nameof(isRhythm))]
    private float               beatThreshold = 0.1f;
    [SerializeField, ShowIf(nameof(isRhythm))]
    private bool                undoLastOnBeatFail;
    [SerializeField, Header("Interaction")]
    private float               interactionCooldown = 0.5f;
    [SerializeField]
    private float               animationTime = 0.25f;
    [SerializeField, Header("Tiles")] 
    private PuzzleTile          baseTilePrefab;
    [SerializeField]
    private Texture2D           baseImage;
    [SerializeField, Header("References")]
    private SpriteRenderer      puzzleBackground;
    [SerializeField]
    private TextMeshProUGUI     solutionText;
    [SerializeField]
    private SpriteRenderer      pumpSprite;
    [SerializeField]
    private Camera              _mainCamera;


    class PuzzleElement
    {        
    }

    class SolutionElement
    {
        public enum ActionType { Move, Rotate, ToggleLight };

        public ActionType       action;
        public Vector2Int       start;
        public Vector2Int       end;
    }

    class EndPoint
    {
        public Vector2Int       pos;
        public int              rotation;
        public SpriteRenderer   spriteRenderer;
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
    Vector2Int              pipeStartPos;
    int                     pipeStartRotation;
    List<EndPoint>          pipeEndPos;
    float                   prevSoundTimeSamples;
    float                   beatTime;
    List<SolutionElement>   undoBuffer;

    public Vector2 tileSize => _tileSize;
    public Vector2 worldOffset => _worldOffset;
    public bool inputEnabled => (interactionTimer <= 0.0f);
    public bool isComplete => completed;
    public Camera mainCamera => _mainCamera;

    public bool isSliding => (puzzleType & PuzzleType.Sliding) != 0;
    public bool isLightsOut => (puzzleType & PuzzleType.LightsOut) != 0;
    public bool isPipemania => (puzzleType & PuzzleType.Pipemania) != 0;
    public bool isRhythm=> (puzzleType & PuzzleType.Rhythm) != 0;

    void Start()
    {
        Build();
    }

    void Update()
    {
        if (completed)
        {
            musicTrackSrc.FadeTo(0.0f, 0.5f);
            return;
        }

        if ((musicTrackSrc) && (isRhythm))
        {
            if (!musicTrackSrc.isPlaying)
            {
                musicTrackSrc.Play();
                musicTrackSrc.volume = 0.0f;
                musicTrackSrc.FadeTo(1.0f, 0.5f);
                prevSoundTimeSamples = musicTrackSrc.timeSamples;
            }
            else
            {
                float prevTimeSeconds = (float)prevSoundTimeSamples / musicTrackSrc.clip.frequency;
                float currTimeSeconds = (float)musicTrackSrc.timeSamples / musicTrackSrc.clip.frequency;

                int prevBeat = Mathf.FloorToInt(prevTimeSeconds * bpm / 60.0f);
                int currBeat = Mathf.FloorToInt(currTimeSeconds * bpm / 60.0f);

                if (prevBeat != currBeat)
                {
                    // Animate
                    puzzleBackground.transform.localScale = Vector3.one * 1.1f;
                    puzzleBackground.transform.ScaleTo(Vector3.one, 0.75f * 60.0f / bpm);

                    beatTime = Time.time;
                }

                prevSoundTimeSamples = musicTrackSrc.timeSamples;
            }
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
            if (Input.GetMouseButtonDown(1))
            {
                HandleRightClick();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                completed = false;
                Build();
            }
        }
    }

    void HandleLeftClick()
    {
        if (!GetMouseGridPos(out var gridPos)) return;

        if (isSliding)
        {
            if (!currentState.HasElement(gridPos.x, gridPos.y)) return;

            if (currentState.GetImmoveable(gridPos.x, gridPos.y)) return;

            if (currentState.GetEmptyNeighbour(gridPos, out var neighbour))
            {
                // Ok sound
                if (IsOnBeat(out float timeDistance))
                {
                    // Get actual object
                    var movementTween = currentTiles[gridPos.x, gridPos.y].MoveTo(neighbour, animationTime);
                    currentTiles[neighbour.x, neighbour.y] = currentTiles[gridPos.x, gridPos.y];
                    currentTiles[gridPos.x, gridPos.y] = null;
                    currentState.Swap(gridPos, neighbour);

                    if ((isLightsOut) && (movementTween != null))
                    {
                        // When movement finishes, toggle light
                        movementTween.Done(() =>
                        {
                            currentState.ToggleLight(neighbour);
                            UpdatePipes();
                        });
                    }
                    else
                    {
                        movementTween.Done(() => UpdatePipes());
                    }

                    if (undoLastOnBeatFail)
                    {
                        undoBuffer.Add(new SolutionElement()
                        {
                            action = SolutionElement.ActionType.Move,
                            start = gridPos,
                            end = neighbour
                        });
                    }
                }
                else
                {
                    // Bad sound

                    if (undoLastOnBeatFail)
                    {
                        var elem = undoBuffer.PopLast();
                        Undo(elem);
                    }
                }
            }
            else
            {
                // Bad sound
            }
        }
        else if (isLightsOut)
        {
            // Ok sound 
            if (IsOnBeat(out float timeDistance))
            {
                currentState.ToggleLight(gridPos);
            }
            else
            {
                // Bad sound

                // Undo doesn't make sense, since undo in this case is just the same state as actually doing the operation
            }
        }

        interactionTimer = interactionCooldown;
    }

    private void Undo(SolutionElement elem)
    {
        if (elem == null) return;

        switch (elem.action)
        {
            case SolutionElement.ActionType.Move:
                // Get actual object
                if (isLightsOut)
                {
                    currentState.ToggleLight(elem.end);
                }

                var movementTween = currentTiles[elem.end.x, elem.end.y].MoveTo(elem.start, animationTime * 0.5f).Done(() => UpdatePipes());
                currentTiles[elem.start.x, elem.start.y] = currentTiles[elem.end.x, elem.end.y];
                currentTiles[elem.end.x, elem.end.y] = null;
                currentState.Swap(elem.end, elem.start);
                break;
            case SolutionElement.ActionType.Rotate:
                var rotationTween = currentTiles[elem.start.x, elem.start.y].Rotate(animationTime, false).Done(() => UpdatePipes());

                currentState.Rotate(elem.start, false);

                break;
            case SolutionElement.ActionType.ToggleLight:
                // Undo of toggle light by itself doesn't make much sense, since it reverts to the same state as actually
                // doing it
                break;
            default:
                break;
        }
    }

    void HandleRightClick()
    {
        if (!GetMouseGridPos(out var gridPos)) return;

        if (isPipemania)
        {
            if (currentState.HasElement(gridPos.x, gridPos.y))
            {
                if (currentState.GetPipeType(gridPos.x, gridPos.y) >= 0)
                {
                    if (IsOnBeat(out float timeDistance))
                    {
                        var rotationTween = currentTiles[gridPos.x, gridPos.y].Rotate(animationTime).Done(() => UpdatePipes());

                        currentState.Rotate(gridPos);

                        if (undoLastOnBeatFail)
                        {
                            undoBuffer.Add(new SolutionElement()
                            {
                                action = SolutionElement.ActionType.Rotate,
                                start = gridPos
                            });
                        }
                    }
                    else
                    {
                        // Bad sound

                        if (undoLastOnBeatFail)
                        {
                            var elem = undoBuffer.PopLast();
                            Undo(elem);
                        }

                    }
                }
            }
        }

        interactionTimer = interactionCooldown;
    }

    bool IsOnBeat(out float timeDistance)
    {
        float currTime = Time.time;

        // Check if we are within the current beat window
        float d1 = Mathf.Abs(currTime - beatTime);
        if (d1 < beatThreshold)
        {
            timeDistance = d1;
            return true;
        }

        // Check if we are slightly early but within the previous beat's tolerance window
        float prevBeatTime = beatTime - (60.0f / bpm);
        float d2 = Mathf.Abs(currTime - prevBeatTime);
        if (d2 < beatThreshold)
        {
            timeDistance = d2;
            return true;
        }

        timeDistance = Mathf.Min(d1, d2);

        return false;
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

    void UpdatePipes()
    {
        if (!isPipemania) return;

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                if (currentTiles[x, y] != null)
                {
                    currentTiles[x, y].SetFull(false);
                    currentState.SetFull(x, y, false);
                }
            }
        }
        foreach (var p in pipeEndPos)
        {
            p.spriteRenderer.sprite = emptyDrainPipe;
        }

        UpdatePipes(pipeStartPos.x, pipeStartPos.y);
    }

    void UpdatePipes(int x, int y)
    {
        if (IsInsideGrid(x, y))
        {
            if (currentTiles[x, y].IsFull()) return;

            currentTiles[x, y].SetFull(true);
            currentState.SetFull(x, y, true);

            byte mask = pipeBitmask[currentState.GetPipeType(x, y)];
            mask = RotateMask(mask, currentState.GetTotalRotation(x, y));

            UpdatePipes(x, y, mask);
        }
        else
        {
            // Check endpoints
            if ((x == pipeStartPos.x) && (y == pipeStartPos.y))
            {
                // It's the start point, check bitmask
                byte mask = 0b0001; // Mask for the pump
                mask = RotateMask(mask, pipeStartRotation);

                UpdatePipes(x, y, mask);
            }
        }
    }

    byte RotateMask(byte mask, int count)
    {
        byte ret = mask;
        for (int i = 0; i < count; i++)
        {
            ret = (byte)(((ret & 0b0111) << 1) + ((ret & 0b1000) >> 3));
        }
        return ret;
    }

    void UpdatePipes(int x, int y, byte mask)
    { 
        for (int i = 0; i < 4; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                var newPos = UpdatePipePos(new Vector2Int(x, y), i);
                if (IsInsideGrid(newPos.x, newPos.y))
                {
                    if (!currentState.HasElement(newPos.x, newPos.y)) continue;
                    // Get rotation of this position
                    var type = currentState.GetPipeType(newPos.x, newPos.y);
                    if (type == -1) continue;
                    var rotation = currentState.GetTotalRotation(newPos.x, newPos.y);
                    var otherMask = RotateMask(pipeBitmask[type], rotation);
                    var newI = (i + 2) % 4;
                    if ((otherMask & (1 << newI)) != 0)
                    {
                        UpdatePipes(newPos.x, newPos.y);
                    }
                }
                else
                {
                    foreach (var p in pipeEndPos)
                    {
                        if ((p.pos.x == newPos.x) && (p.pos.y == newPos.y))
                        {
                            p.spriteRenderer.sprite = fullDrainPipe;
                        }
                    }

                }
            }
        }
    }

    bool IsInsideGrid(int x, int y)
    {
        return (x >= 0) && (y >= 0) && (x < gridSize.x) && (y < gridSize.y);
    }

    Vector2Int UpdatePipePos(Vector2Int pos, int rotation)
    {
        switch (rotation)
        {
            case 0: return new Vector2Int(pos.x + 1, pos.y);
            case 1: return new Vector2Int(pos.x, pos.y + 1);
            case 2: return new Vector2Int(pos.x - 1, pos.y);
            case 3: return new Vector2Int(pos.x, pos.y - 1);
            default:
                break;
        }
        return pos;
    }

    [Button("Build")]
    void Build()
    {
        if (!randomSeed) randomGenerator = new System.Random(seed);
        else
        {
            // Get an actual random seed
            int s = System.Environment.TickCount;
            Debug.Log($"Current seed = {s}");
            randomGenerator = new System.Random(s);            
        }

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

        currentState = new PuzzleState(puzzleType, gridSize, baseImage != null, neightborhoodType, neighborhoodDistance);
        currentState.Identity();

        if (isPipemania)
        {
            // Create pipe map
            CreatePipeMap();
        }

        if (isSliding)
        {
            for (int i = 0; i < unmoveablePieceCount; i++)
            {
                var p = gridSize.RandomXY(randomGenerator);

                if ((isPipemania) && (currentState.GetPipeType(p.x, p.y) >= 0)) continue;

                currentState.SetImmoveable(p.x, p.y, true);
            }

            // Remove a piece from the puzzle
            if (baseImage)
            {
                // Remove a piece from the puzzle
                Vector2Int clearPiece;
                do
                {
                    clearPiece = gridSize.RandomXY(randomGenerator);
                }
                while (currentState.GetPipeType(clearPiece.x, clearPiece.y) >= 0);

                currentState.Clear(clearPiece.x, clearPiece.y);
            }
            else
            {
                Vector2Int clearPiece;

                do
                {
                    int rx = (gridSize.x % 2 != 0) ? (Mathf.FloorToInt(gridSize.x * 0.5f)) : 0;
                    int ry = (gridSize.y % 2 != 0) ? (Mathf.FloorToInt(gridSize.y * 0.5f)) : gridSize.y - 1;
                    clearPiece = new Vector2Int(rx, ry);
                }
                while (currentState.GetPipeType(clearPiece.x, clearPiece.y) >= 0);

                currentState.Clear(clearPiece.x, clearPiece.y);
            }
        }


        if (shuffle)
        {
            Shuffle();
        }

        CreatePieces();
        UpdatePipes();

        if ((isRhythm) && (undoLastOnBeatFail))
        {
            undoBuffer = new();
        }
    }

    static readonly byte[] pipeBitmask = new byte[] { 0b1001, 0b0101, 0b1101, 0b1111 };

    void CreatePipeMap()
    {
        int r = randomGenerator.Range(0, 3);
        if (r == 0) { pipeStartPos = new Vector2Int(randomGenerator.Range(0, gridSize.x / 2), -1); pipeStartRotation = 1; }
        else if (r == 1) { pipeStartPos = new Vector2Int(-1, randomGenerator.Range(0, gridSize.y)); pipeStartRotation = 0; }
        else if (r == 2) { pipeStartPos = new Vector2Int(randomGenerator.Range(0, gridSize.x / 2), gridSize.y); pipeStartRotation = 3; }

        var gs = new Vector2Int(gridSize.x + 2, gridSize.y + 2);

        pipeEndPos = new();

        var start = new Vector2Int(pipeStartPos.x + 1, pipeStartPos.y + 1);
        var allPaths = new List<Vector2Int>();

        for (int i = 0; i < numberOfOuts; i++)
        {
            int nTries = 0;
            while (nTries < 50)
            {
                var pipeGrid = new int[gs.x, gs.y];
                for (int k = 0; k < gs.x; k++) pipeGrid[k, 0] = 1;
                for (int k = 0; k < gs.x; k++) pipeGrid[k, gs.y - 1] = 1;
                for (int k = 0; k < gs.y; k++) pipeGrid[0, k] = 1;
                for (int k = 0; k < gs.y; k++) pipeGrid[gs.x - 1, k] = 1;
                for (int k = 0; k < blockTiles; k++)
                {
                    var bt = new Vector2Int(randomGenerator.Range(0, gs.x), randomGenerator.Range(0, gs.y));
                    pipeGrid[bt.x, bt.y] = 1;
                }

                r = randomGenerator.Range(0, 4);
                int er = 0;
                Vector2Int o = Vector2Int.zero;
                if (r == 0) { o = new Vector2Int(gs.x - 1, randomGenerator.Range(0, gs.y)); er = 2; }
                else if (r == 1) { o = new Vector2Int(randomGenerator.Range(0, gs.x), gs.y - 1); er = 3; }
                else if (r == 2) { o = new Vector2Int(0, randomGenerator.Range(0, gs.y)); er = 0; }
                else if (r == 4) { o = new Vector2Int(randomGenerator.Range(0, gs.x), 0); er = 1; }

                pipeGrid[start.x, start.y] = pipeGrid[o.x, o.y] = 0;

                var path = AStar.GetPath(pipeGrid, start, o);

                pipeGrid[start.x, start.y] = pipeGrid[o.x, o.y] = 1;

                if ((path != null) && (path.Count > minPathLength))
                {
                    allPaths.AddRange(path);

                    pipeEndPos.Add(new EndPoint {
                        pos = new Vector2Int(o.x - 1, o.y - 1),
                        rotation = er
                    });
                    break;
                }

                nTries++;
            }
        }

        foreach (var p in allPaths)
        {
            if (p == start) continue;
            bool isEnd = false;
            foreach (var e in pipeEndPos)
            {
                if ((e.pos.x == p.x - 1) &&
                    (e.pos.y == p.y - 1))
                {
                    isEnd = true;
                    break;
                }
            }
            if (isEnd) continue;

            byte bmask = 0;
            if (allPaths.Contains(new Vector2Int(p.x + 1, p.y))) bmask |= 0b0001;
            if (allPaths.Contains(new Vector2Int(p.x, p.y + 1))) bmask |= 0b0010;
            if (allPaths.Contains(new Vector2Int(p.x - 1, p.y))) bmask |= 0b0100;
            if (allPaths.Contains(new Vector2Int(p.x, p.y - 1))) bmask |= 0b1000;

            int pipeType = -1;
            int pipeRot = 0;
            while (pipeRot < 4)
            {
                for (int j = pipeBitmask.Length - 1; j >= 0; j--)
                {
                    if (pipeBitmask[j] == bmask)
                    {
                        pipeType = j;
                        break;
                    }
                }
                if (pipeType != -1) break;

                pipeRot++;
                bmask = (byte)(((bmask & 0b0001) << 3) + (bmask >> 1));
            }

            if (pipeType != -1)
                currentState.SetPipe(p.x - 1, p.y - 1, pipeType, pipeRot);
        }
    }

    void Shuffle()
    {
        solution = new();
        // Start from initial state
        prevStates = new() { currentState.Clone() };

        List<int> shuffleOptions = new();
        if (isSliding) { shuffleOptions.Add(0); }
        else if (isLightsOut) { shuffleOptions.Add(1); }

        if (isPipemania) shuffleOptions.Add(2);

        for (int i = 0; i < shuffleAmmount; i++)
        {
            int opt = shuffleOptions.Random();

            switch (opt)
            {
                case 0:
                    {
                        // Move sliding piece
                        int nTries = 0;
                        while (nTries < 10)
                        {
                            var gridPos = currentState.GetRandomGridPos(randomGenerator, true, false, false);
                            currentState.GetEmptyNeighbour(gridPos, out var neighbour);

                            if (isLightsOut) currentState.ToggleLight(gridPos);
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
                                if (isLightsOut) currentState.ToggleLight(gridPos);

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
                    break;
                case 1:
                    {
                        // Turn on/off light
                        int nTries = 0;
                        while (nTries < 10)
                        {
                            var gridPos = currentState.GetRandomGridPos(randomGenerator, false, true, false);

                            currentState.ToggleLight(gridPos);

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
                                currentState.ToggleLight(gridPos);

                                nTries++;
                                continue;
                            }

                            prevStates.Add(currentState.Clone());

                            SolutionElement solutionElement = new()
                            {
                                action = SolutionElement.ActionType.ToggleLight,
                                start = gridPos
                            };

                            solution.Add(solutionElement);

                            break;
                        }
                    }
                    break;
                case 2:
                    {
                        // Rotate piece
                        int nTries = 0;
                        while (nTries < 10)
                        {
                            var gridPos = currentState.GetRandomGridPos(randomGenerator, false, false, true);

                            currentState.Rotate(gridPos, false);

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
                                currentState.Rotate(gridPos, true);

                                nTries++;
                                continue;
                            }

                            prevStates.Add(currentState.Clone());

                            SolutionElement solutionElement = new()
                            {
                                action = SolutionElement.ActionType.Rotate,
                                start = gridPos
                            };

                            solution.Add(solutionElement);

                            break;
                        }
                    }
                    break;
                default:
                    break;
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
                    case SolutionElement.ActionType.ToggleLight:
                        st += $"Toggle light at {s.start.x},{s.start.y}\n";
                        break;
                    case SolutionElement.ActionType.Rotate:
                        st += $"Rotate piece at {s.start.x},{s.start.y}\n";
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
                    currentTiles[x, y].transform.position = GetWorldPos(x, y);
                    currentTiles[x, y].transform.rotation = Quaternion.Euler(0, 0, currentState.GetPieceRotation(x, y) * 90.0f);
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
                    currentTiles[x, y].SetPipe(currentState.GetPipeType(x, y), currentState.GetPipeRotation(x, y));
                }
            }
        }

        if (pumpSprite)
        {
            pumpSprite.enabled = isPipemania;
            pumpSprite.transform.position = GetWorldPos(pipeStartPos.x, pipeStartPos.y);
            pumpSprite.transform.rotation = Quaternion.Euler(0, 0, pipeStartRotation * 90.0f);
        }

        int outCount = ((pipeEndPos != null) ? (pipeEndPos.Count) : (0));
        for (int i = 0; i < outCount; i++)
        {
            var d = pipeEndPos[i];
            drainPipes[i].gameObject.SetActive(true);
            drainPipes[i].transform.position = GetWorldPos(d.pos.x, d.pos.y);
            drainPipes[i].transform.rotation = Quaternion.Euler(0, 0, d.rotation * 90.0f);
            d.spriteRenderer = drainPipes[i];
        }
        for (int i = outCount; i < drainPipes.Length; i++)
        {
            drainPipes[i].gameObject.SetActive(false);
        }
    }

    Vector3 GetWorldPos(int x, int y)
    {
        return new Vector3(x * _tileSize.x + _worldOffset.x + _tileSize.x * 0.5f, y * _tileSize.y + _worldOffset.y + _tileSize.y * 0.5f, 0.0f);
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

    public PuzzleState GetCurrentState()
    {
        return currentState;
    }
}
