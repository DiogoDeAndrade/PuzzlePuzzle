using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDef", menuName = "Puzzle Puzzle/Level Def")]
public class LevelDef : ScriptableObject
{
    [SerializeField, Header("Puzzle Parameters")]
    public PuzzleType          puzzleType = PuzzleType.Sliding;
    [SerializeField]
    public Vector2Int          gridSize = new Vector2Int(2, 2);
    [SerializeField]
    public bool                shuffle;
    [SerializeField, ShowIf(nameof(shuffle))]
    public int                 shuffleAmmount = 3;
    [SerializeField]
    public bool                randomSeed;
    [SerializeField, HideIf(nameof(randomSeed))]
    public int                 seed;
    [SerializeField, ShowIf(nameof(isSliding)), Header("Sliding")]
    public int                 unmoveablePieceCount;
    [SerializeField, ShowIf(nameof(isLightsOut)), Header("Lights Out")]
    public PuzzleState.NeighborhoodType neightborhoodType = PuzzleState.NeighborhoodType.VonNeumann;
    [SerializeField, ShowIf(nameof(isLightsOut))]
    public int                 neighborhoodDistance = 1;
    [SerializeField, ShowIf(nameof(isPipemania)), Header("Pipemania")]
    public int                 numberOfOuts = 2;
    [SerializeField, ShowIf(nameof(isPipemania))]
    public int                 minPathLength = 7;
    [SerializeField, ShowIf(nameof(isPipemania))]
    public int                 blockTiles = 10;
    [SerializeField, ShowIf(nameof(isRhythm)), Header("Rhythm")]
    public AudioClip           musicTrackClip;
    [SerializeField, ShowIf(nameof(isRhythm))]
    public int                 bpm = 120;
    [SerializeField, ShowIf(nameof(isRhythm))]
    public float               beatThreshold = 0.25f;
    [SerializeField, ShowIf(nameof(isRhythm))]
    public bool                undoLastOnBeatFail;
    [SerializeField]
    public Texture2D           baseImage;

    public bool isSliding => (puzzleType & PuzzleType.Sliding) != 0;
    public bool isLightsOut => (puzzleType & PuzzleType.LightsOut) != 0;
    public bool isPipemania => (puzzleType & PuzzleType.Pipemania) != 0;
    public bool isRhythm => (puzzleType & PuzzleType.Rhythm) != 0;

}
