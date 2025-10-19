using System;
using System.Collections.Generic;
using TMPro;
using UC;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
    [SerializeField]
    private bool        autoStart = true;
    [SerializeField]
    private int         _level;
    [SerializeField]
    private LevelDef[]  levels;
    [SerializeField]
    private Hypertag    levelDisplayTextTag;
    [SerializeField]
    private Hypertag    movesDisplayTextTag;
    [SerializeField]
    private Transform   spawnPos;
    [SerializeField]
    private Transform   exitPos;
    [SerializeField]
    private Puzzle      puzzlePrefab;
    [SerializeField]
    private Hypertag    backgroundTag;
    [SerializeField]
    private Color[]     backgroundColors;
    [SerializeField]
    private List<AudioClip> songs;
    [SerializeField]
    private List<Texture2D> images;

    public int level
    {
        get { return _level; }
        set { _level = value; }
    }

    Puzzle      currentPuzzle;
    int         movesCount;       

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameManager>();
            }
            return _instance;
        }
    }

    static GameManager _instance;
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
#if !UNITY_EDITOR
        autoStart = false;
#endif

        currentPuzzle = FindFirstObjectByType<Puzzle>();

        if (autoStart)
        {
            InitLevel();
        }
    }

    public void Reset()
    {
        _level = 0;
    }

    public void ResetLevel()
    {
        EventSystem.current.SetSelectedGameObject(null);
        currentPuzzle?.Restart();
    }

    public void InitLevel()
    {
        if (currentPuzzle)
        {
            var prevPuzzle = currentPuzzle;
            currentPuzzle.transform.MoveTo(exitPos.position, 0.25f).EaseFunction(Ease.Sqr).Done(
                () => {
                    Destroy(prevPuzzle.gameObject);
                });
            currentPuzzle = null;
        }

        if (levelDisplayTextTag)
        {
            var levelDisplayText = Hypertag.FindFirstObjectWithHypertag<TextMeshProUGUI>(levelDisplayTextTag);

            if (levelDisplayText)
            {
                levelDisplayText.text = $"Level {_level + 1}";
            }
        }

        UpdateMovesCount();

        currentPuzzle = Instantiate(puzzlePrefab);
        currentPuzzle.transform.position = spawnPos.transform.position;
        currentPuzzle.InitLevel(GetLevel());
        currentPuzzle.transform.MoveTo(Vector3.zero, 0.25f).EaseFunction(Ease.Sqrt);

        SpriteRenderer backgroundRenderer = Hypertag.FindFirstObjectWithHypertag<SpriteRenderer>(backgroundTag);
        if (backgroundRenderer)
        {
            Color bgColor = backgroundColors[_level % backgroundColors.Length];
            Material material = backgroundRenderer.material;
            if (material)
            {
                material.SetColor("_Color2", bgColor);
            }            

            backgroundRenderer.color = bgColor;
        }
    }

    public void NextLevel()
    {
        _level++;
        movesCount = 0;
        PlayerPrefs.SetInt("CurrentLevel", _level);
        PlayerPrefs.Save();
        InitLevel();
    }

    void UpdateMovesCount()
    {
        if (movesDisplayTextTag)
        {
            var movesDisplayText = Hypertag.FindFirstObjectWithHypertag<TextMeshProUGUI>(movesDisplayTextTag);

            if (movesDisplayText)
            {
                if (movesCount == 0)
                    movesDisplayText.text = "No moves";
                else if (movesCount == 1)
                    movesDisplayText.text = $"{movesCount} Move";
                else 
                    movesDisplayText.text = $"{movesCount} Moves";
            }
        }
    }

    public void IncrementMoveCount()
    {
        movesCount++;
        UpdateMovesCount();
    }

    LevelDef GetLevel()
    {
        if (_level >= levels.Length)
        {
            int baseLevel = _level - levels.Length + 1;
            var newLevel = ScriptableObject.CreateInstance<LevelDef>();
            var randomGenerator = new System.Random(_level * 12345 + _level * 123 + _level);

            // Puzzle type
            int baseType = randomGenerator.Range(0, 3);
            newLevel.puzzleType = (PuzzleType)(1 << baseType);
            if (randomGenerator.Range(0, 100) < baseLevel)
            {
                int secondaryType = randomGenerator.Range(0, 3);
                newLevel.puzzleType |= (PuzzleType)(1 << secondaryType);

                if (randomGenerator.Range(0, 100) < baseLevel)
                {
                    int thirdType = randomGenerator.Range(0, 3);
                    newLevel.puzzleType |= (PuzzleType)(1 << thirdType);
                }
            }

            // Grid size
            int gx = Mathf.Clamp(randomGenerator.Range(0, baseLevel / 8), 4, 8);
            int gy = Mathf.Clamp(randomGenerator.Range(0, baseLevel / 8), 4, 8);
            newLevel.gridSize = new Vector2Int(gx, gy);

            // Other variables
            newLevel.shuffle = true;
            newLevel.shuffleAmmount = (int)(Mathf.Max(gx, gy) * 1.5f + randomGenerator.Range(1, baseLevel / 4));
            newLevel.randomSeed = false;
            newLevel.seed = randomGenerator.Next();
            
            if (newLevel.isSliding)
            {
                // For sliding
                newLevel.unmoveablePieceCount = randomGenerator.Range(0, Mathf.Min(gx, gy) / 3) + randomGenerator.Range(0, baseLevel / 5);
            }

            if (newLevel.isPipemania)
            {
                newLevel.numberOfOuts = Mathf.Clamp(randomGenerator.Range(0, baseLevel / 20) + 2, 1, Mathf.Max(gx, gy) / 2);
                newLevel.minPathLength = (int)(Mathf.Max(gx, gy) * 1.5f);
                newLevel.blockTiles = (int)(gx * gy * 0.2f);
            }

            if (randomGenerator.Range(0, 200) < baseLevel)
            {
                newLevel.puzzleType |= PuzzleType.Rhythm;
                newLevel.musicTrackClip = songs.Random(randomGenerator);
            }

            newLevel.baseImage = images.Random(randomGenerator);

            return newLevel;
        }

        return levels[_level];
    }

    public void ReturnToMainMenu()
    {
        FullscreenFader.FadeOut(0.5f, Color.black, () =>
        {
            SceneManager.LoadScene("MainMenu");
        });
    }
}
