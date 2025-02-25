using System;
using UnityEngine;
using UnityEngine.UIElements;

[Flags]
public enum PuzzleType
{
    Sliding = 1,
    LightsOut = 2,
    Pipemania = 4,
    Rhythm = 8
};

public class PuzzleState
{
    class PuzzleElement
    {
        public Vector2Int   originalPosition;
        public bool         lightState;
        public bool         immoveable;

        public PuzzleElement(Vector2Int originalPosition, bool lightState)
        {
            this.originalPosition = originalPosition;
            this.lightState = lightState;

            immoveable = false;
        }

        public PuzzleElement Clone()
        {
            return new PuzzleElement(originalPosition, lightState);
        }
    };

    public enum NeighborhoodType { VonNeumann, Moore };

    private PuzzleType          puzzleType;
    private Vector2Int          gridSize;
    private bool                hasImage;
    private NeighborhoodType    neighborhoodType;
    private int                 neighborhoodDistance;
    private PuzzleElement[,]    state;

    public PuzzleState(PuzzleType puzzleType, Vector2Int gridSize, bool hasImage, NeighborhoodType neighborhoodType, int neighborhoodDistance)
    {
        this.gridSize = gridSize;
        this.puzzleType = puzzleType;
        this.hasImage = hasImage;
        this.neighborhoodType = neighborhoodType;
        this.neighborhoodDistance = neighborhoodDistance;

        state = new PuzzleElement[gridSize.x, gridSize.y];
    }

    public void Identity()
    {
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                state[x, y] = new PuzzleElement(new Vector2Int(x, y), true);
            }
        }
    }

    public void Clear(int x, int y)
    {
        state[x, y] = null;
    }

    public void SetImmoveable(int x, int y, bool v)
    {
        if (state[x, y] != null)
        {
            state[x, y].immoveable = v;
        }
    }

    public void Swap(Vector2Int p1, Vector2Int p2)
    {
        var tmp = state[p2.x, p2.y];
        state[p2.x, p2.y] = state[p1.x, p1.y];
        state[p1.x, p1.y] = tmp;
    }

    public Vector2Int GetRandomGridPos(System.Random randomGenerator, bool withEmptyNeighbour, bool allowImmobile)
    {
        while (true)
        {
            var pos = gridSize.RandomXY(randomGenerator);
            if (state[pos.x, pos.y] != null)
            {
                if ((!allowImmobile) && (GetImmoveable(pos.x, pos.y))) continue;

                if (withEmptyNeighbour)
                {
                    if (GetEmptyNeighbour(pos, out var neighbour)) return pos;
                }
                else return pos;
            }
        }
    }

    public bool GetEmptyNeighbour(Vector2Int pos, out Vector2Int neighbour)
    {
        if ((pos.x > 0) && (state[pos.x - 1, pos.y] == null)) { neighbour = new Vector2Int(pos.x - 1, pos.y); return true; }
        if ((pos.x < gridSize.x - 1) && (state[pos.x + 1, pos.y] == null)) { neighbour = new Vector2Int(pos.x + 1, pos.y); return true; }
        if ((pos.y > 0) && (state[pos.x, pos.y - 1] == null)) { neighbour = new Vector2Int(pos.x, pos.y - 1); return true; }
        if ((pos.y < gridSize.y - 1) && (state[pos.x, pos.y + 1] == null)) { neighbour = new Vector2Int(pos.x, pos.y + 1); return true; }

        neighbour = Vector2Int.zero;
        return false;
    }

    public bool HasElement(int x, int y)
    {
        return state[x, y] != null;
    }

    public Vector2Int GetOriginalPosition(int x, int y)
    {
        return state[x, y].originalPosition;
    }

    public bool GetImmoveable(int x, int y)
    {
        return state[x, y].immoveable;
    }

    public bool isLightOn(int x, int y)
    {
        return state[x, y].lightState;
    }

    public bool IsSame(PuzzleState currentState)
    {
        if (puzzleType != currentState.puzzleType) return false;
        if (gridSize != currentState.gridSize) return false;

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                if (((state[x, y] == null) && (currentState.state[x, y] != null)) ||
                    ((state[x, y] != null) && (currentState.state[x, y] == null)))
                {
                    return false;
                }

                if ((state[x, y] != null) && (currentState.state[x, y] != null))
                {
                    if (((puzzleType & PuzzleType.Sliding) != 0) && (hasImage))
                    {
                        if (state[x, y].originalPosition != currentState.state[x, y].originalPosition) return false;
                    }

                    if ((puzzleType & PuzzleType.LightsOut) != 0)
                    {
                        {
                            if (state[x, y].lightState != currentState.state[x, y].lightState) return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    public PuzzleState Clone()
    {
        var ret = new PuzzleState(puzzleType, gridSize, hasImage, neighborhoodType, neighborhoodDistance);
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                if (state[x, y] != null)              
                {
                    ret.state[x, y] = state[x, y].Clone();
                }
            }
        }

        return ret;
    }

    public bool CheckSolution()
    {
        if ((puzzleType & PuzzleType.Sliding) != 0)
        {
            // Check center piece
            if (hasImage)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    for (int x = 0; x < gridSize.x; x++)
                    {
                        if (state[x, y] != null)
                        {
                            if ((state[x, y].originalPosition.x != x) ||
                                (state[x, y].originalPosition.y != y))
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            else
            {
                int rx = (gridSize.x % 2 != 0) ? (Mathf.FloorToInt(gridSize.x * 0.5f)) : 0;
                int ry = (gridSize.y % 2 != 0) ? (Mathf.FloorToInt(gridSize.y * 0.5f)) : gridSize.y - 1;

                if (state[rx, ry] != null) return false;
            }
        }

        if ((puzzleType & PuzzleType.LightsOut) != 0)
        {
            // All lights must be on
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    if (state[x, y] != null)
                    {
                        if (!state[x, y].lightState) return false;
                    }
                }
            }
        }

        return true;
    }

    internal void ToggleLight(Vector2Int gridPos)
    {
        for (int y = gridPos.y - neighborhoodDistance; y <= gridPos.y + neighborhoodDistance; y++)
        {
            if ((y < 0) || (y >= gridSize.y)) continue;
            for (int x = gridPos.x - neighborhoodDistance; x <= gridPos.x + neighborhoodDistance; x++)
            {
                if ((x < 0) || (x >= gridSize.x)) continue;
                if (state[x, y] == null) continue;

                if ((neighborhoodType == NeighborhoodType.VonNeumann) &&
                    (x != gridPos.x) && (y != gridPos.y)) continue;

                state[x, y].lightState = !state[x, y].lightState;
            }
        }
    }
}
