﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MazeTileType : byte
{
    Wall,
    Free
}


public class Maze : MonoBehaviour
{
    [Header("General settings")]
    [SerializeField]
    private Vector2 mazeCenter = Vector2.zero;

    [SerializeField]
    private GameObject mazeTilePrefab;

    [SerializeField]
    private Sprite freeTileSprite;

    [SerializeField]
    private Sprite wallTileSprite;

    [Header("Instance-related settings")]
    [SerializeField]
    private TextAsset mazeDefinition;

    [SerializeField]
    private char wallTileChar = '#';

    [SerializeField]
    private char freeTileChar = '-';

    [SerializeField]
    private char agentSpawnChar = 'A';

    /// <summary>
    /// Maze tiles are stored in row-major order
    /// First index is thus row/y, second indices are columns/x
    /// </summary>
    public List<List<MazeTileType>> MazeTiles { get; private set; }

    public Vector2Int AgentSpawnTilePos { get; private set; }

    private Vector2Int mazeTilesExtent;
    private SpriteRenderer[,] mazeTileRenderers;

    private float mazeElementsScale = 1.0f;
    private float xTileToPosMultiplier;
    private float yTileToPosMultiplier;

    private void Awake()
    {
        VerifyPrefabs();
    }

    public List<Vector2Int> GetNeighbors(int row, int col)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        int flags = 0;

        if (MazeTiles[row + 1][col] == MazeTileType.Free)
        {
            neighbors.Add(new Vector2Int(col, row + 1));
            flags += 1;
        }
        if (MazeTiles[row][col + 1] == MazeTileType.Free)
        {
            neighbors.Add(new Vector2Int(col + 1, row));
            flags += 2;
        }
        if (MazeTiles[row - 1][col] == MazeTileType.Free)
        {
            neighbors.Add(new Vector2Int(col, row - 1));
            flags += 4;
        }
        if (MazeTiles[row][col - 1] == MazeTileType.Free)
        {
            neighbors.Add(new Vector2Int(col - 1, row));
            flags += 8;
        }

        if(MazeTiles[row + 1][col - 1] == MazeTileType.Free && ((flags & 9) == 9))
        {
            neighbors.Add(new Vector2Int(col - 1,row + 1));
        }

        if (MazeTiles[row + 1][col + 1] == MazeTileType.Free && ((flags & 3) == 3))
        {
            neighbors.Add(new Vector2Int(col + 1, row + 1));
        }

        if (MazeTiles[row - 1][col + 1] == MazeTileType.Free && ((flags & 6) == 6))
        {
            neighbors.Add(new Vector2Int(col + 1, row - 1));
        }

        if (MazeTiles[row - 1][col - 1] == MazeTileType.Free && ((flags & 12) == 12))
        {
            neighbors.Add(new Vector2Int(col - 1, row - 1));
        }

        return neighbors;
    }

    public Vector3 GetWorldPositionForMazeTile(int x, int y)
    {
        return new Vector3(
                    mazeCenter.x + (x - mazeTilesExtent.x) * xTileToPosMultiplier,
                    mazeCenter.y + (mazeTilesExtent.y - y) * yTileToPosMultiplier,
                    0.0f);
    }

    public Vector3 GetWorldPositionForMazeTile(Vector2Int tile)
    {
        return GetWorldPositionForMazeTile(tile.x, tile.y);
    }

    /// <summary>
    /// May return out-of-bounds tile index so check the result with IsInBoundsTile(...) function
    /// </summary>
    public Vector2Int GetMazeTileForWorldPosition(Vector3 position)
    {
        return new Vector2Int(
            (int)((position.x - mazeCenter.x + xTileToPosMultiplier * 0.5f) / xTileToPosMultiplier + mazeTilesExtent.x),
            (int)(mazeTilesExtent.y - (position.y - mazeCenter.y - yTileToPosMultiplier * 0.5f) / yTileToPosMultiplier));
    }

    public Vector3 GetElementsScale()
    {
        return new Vector3(mazeElementsScale, mazeElementsScale, 1.0f);
    }

    public bool IsInBoundsTile(int x, int y)
    {
        return x >= 0 && y >= 0 && MazeTiles.Count > y && MazeTiles[y].Count > x;
    }

    public bool IsInBoundsTile(Vector2Int tile)
    {
        return IsInBoundsTile(tile.x, tile.y);
    }

    public bool IsValidTileOfType(Vector2Int tile, MazeTileType type)
    {
        return IsInBoundsTile(tile) && MazeTiles[tile.y][tile.x] == type;
    }

    public void SetFreeTileColor(Vector2Int tile, Color color)
    {
        if(IsValidTileOfType(tile, MazeTileType.Free))
        {
            mazeTileRenderers[tile.y, tile.x].color = color;
        }
        else
        {
            Debug.Log("Invalid tile provided as input: " + tile.ToString());
        }
    }

    public void ResetTileColors()
    {
        for(int i = 0; i < mazeTileRenderers.GetLength(0); ++i)
        {
            for(int j = 0; j < mazeTileRenderers.GetLength(1); ++j)
            {
                mazeTileRenderers[i, j].color = Color.white;
            }
        }
    }

    #region Creation & Initialization of a maze. You can probably ignore this code. :-)

    public void BuildMaze()
    {
        if(mazeDefinition == null)
        {
            Debug.LogError("No maze definition provided!");
            return;
        }

        ProcessMazeDefinition();

        if(MazeTiles.Count == 0)
        {
            Debug.LogError("Cannot spawn empty maze!");
            return;
        }

        SpawnMazeTiles();
    }

    private void ProcessMazeDefinition()
    {
        MazeTiles = new List<List<MazeTileType>>();

        using (var reader = new System.IO.StringReader(mazeDefinition.text))
        {
            string line;
            int previousLineLength = -1;
            int currentLine = 0;

            for (currentLine = 0; (line = reader.ReadLine()?.Trim()) != null; ++currentLine)
            {
                if (line.Length == 0) { continue; } // Skip empty lines since they are allowed in definition

                if (currentLine > 0 && previousLineLength != line.Length)
                {
                    Debug.LogError(
                        string.Format("Incorrect maze definition. Problem on line {0} with content '{1}'.",
                        currentLine, line));
                    return;
                }

                List<MazeTileType> newRow = new List<MazeTileType>();

                for (int i = 0; i < line.Length; ++i)
                {
                    if (!ProcessMazeDefCharacter(line[i], newRow, currentLine, i))
                    {
                        Debug.LogError("Maze creation failed.");
                        return;
                    }
                }

                MazeTiles.Add(newRow);

                previousLineLength = line.Length;
            }
        }

        mazeTilesExtent = new Vector2Int(
            (int)Mathf.Ceil(MazeTiles[0].Count / 2.0f), 
            (int)Mathf.Ceil(MazeTiles.Count / 2.0f));

        float screenHeightInUnits = GameManager.Instance.MainCamera.orthographicSize * 2.0f;
        float screenWidthInUnits = screenHeightInUnits * (Screen.width / (float)Screen.height);

        var freeTileSr = mazeTilePrefab.GetComponent<SpriteRenderer>();

        mazeElementsScale = Mathf.Min(
            screenWidthInUnits / ((mazeTilesExtent.x + 1) * 2.0f),
            screenHeightInUnits / ((mazeTilesExtent.y + 1) * 2.0f));
        
        xTileToPosMultiplier = (freeTileSr.sprite.rect.width / freeTileSr.sprite.pixelsPerUnit) * mazeElementsScale;
        yTileToPosMultiplier = (freeTileSr.sprite.rect.height / freeTileSr.sprite.pixelsPerUnit) * mazeElementsScale;

        mazeTileRenderers = new SpriteRenderer[MazeTiles.Count, MazeTiles[0].Count];
    }

    private bool ProcessMazeDefCharacter(char character, List<MazeTileType> rowTiles, int row, int col)
    {
        if(character == wallTileChar)
        {
            rowTiles.Add(MazeTileType.Wall);
        }
        else if(character == freeTileChar)
        {
            rowTiles.Add(MazeTileType.Free);
        }
        else if(character == agentSpawnChar)
        {
            rowTiles.Add(MazeTileType.Free);
            AgentSpawnTilePos = new Vector2Int(col, row);
        }
        else
        {
            Debug.LogError(string.Format(
                "Unknown character found in data at [row {0}, col {1}]: {2}", row, col, character));
            return false;
        }

        return true;
    }

    private void SpawnMazeTiles()
    {
        for (int y = 0; y < MazeTiles.Count; ++y)
        {
            for(int x = 0; x < MazeTiles[y].Count; ++x)
            {
                var newTileGo = Instantiate(mazeTilePrefab, transform);

                newTileGo.transform.position = GetWorldPositionForMazeTile(x, y);
                newTileGo.transform.localScale = GetElementsScale();

                var newTileSrComp = newTileGo.GetComponent<SpriteRenderer>();

                if(newTileSrComp)
                {
                    newTileSrComp.sprite = MazeTiles[y][x] == MazeTileType.Free ? freeTileSprite : wallTileSprite;
                    mazeTileRenderers[y, x] = newTileSrComp;
                }
                else
                {
                    Debug.LogError("Missing SpriteRenderer on the tile prefab!");
                }
            }
        }
    }

    private void VerifyPrefabs()
    {
        if (mazeTilePrefab == null)
        {
            Debug.LogError("No tile prefab defined!");
        }

        if (freeTileSprite == null || wallTileSprite == null)
        {
            Debug.LogError("Tile sprite is missing!");
        }
    }
    #endregion
}
