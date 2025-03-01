using TMPro;
using UnityEngine;

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
    private Transform   spawnPos;
    [SerializeField]
    private Transform   exitPos;
    [SerializeField]
    private Puzzle      puzzlePrefab;

    public int level
    {
        get { return _level; }
        set { _level = value; }
    }

    Puzzle currentPuzzle;

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

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Reset()
    {
        _level = 0;
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

        currentPuzzle = Instantiate(puzzlePrefab);
        currentPuzzle.transform.position = spawnPos.transform.position;
        currentPuzzle.InitLevel(GetLevel());
        currentPuzzle.transform.MoveTo(Vector3.zero, 0.25f).EaseFunction(Ease.Sqrt);
    }

    public void NextLevel()
    {
        _level++;
        PlayerPrefs.SetInt("CurrentLevel", _level);
        PlayerPrefs.Save();
        InitLevel();
    }

    LevelDef GetLevel()
    {
        return levels[_level];
    }
}
