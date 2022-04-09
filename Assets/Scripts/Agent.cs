using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;

public class Agent : MonoBehaviour
{
    public Vector2Int CurrentTile { get; private set; }

    private Sprite _sprite;
    public Sprite Sprite
    {
        get
        {
            if(_sprite == null)
            {
                _sprite = GetComponentInChildren<SpriteRenderer>()?.sprite;
            }

            return _sprite;
        }
    }

    protected float movementSpeed;
    protected Maze parentMaze;
    protected bool isInitialized = false;
    private Vector2Int lastGoal;
    List<Vector2Int> path;

    protected virtual void Start()
    {
        GameManager.Instance.DestinationChanged += OnDestinationChanged;
        lastGoal = GameManager.Instance.DestinationTile;
    }

    protected virtual void Update()
    {
        // TODO Assignment 2 ... this function might be of your interest. :-)
        // You are free to add new functions, create new classes, etc.
        // ---
        // The CurrentTile property should held the current location (tile-based) of an agent
        //
        // Have a look at Maze class, it contains several useful properties and functions.
        // For example, Maze.MazeTiles stores the information about the tiles of the maze.
        // Then, there are several functions for conversion/retrieval of tile positions, as well as for changing tile colors.
        // 
        // Finally, you can also have a look at GameManager to see what it provides.

        // NOTE
        // The code below is just a simple demonstration of some of the functionality / functions
        // You will need to replace it / change it

        if(lastGoal != GameManager.Instance.DestinationTile)
        {
            lastGoal = GameManager.Instance.DestinationTile;
            StopAllCoroutines();
            parentMaze.ResetTileColors();

            StartCoroutine(CalculatePath(CurrentTile, lastGoal));
            
        }

        var destWorld = parentMaze.GetWorldPositionForMazeTile(GameManager.Instance.DestinationTile);
        
        /*if(destWorld.x > transform.position.x && parentMaze.IsValidTileOfType(new Vector2Int(CurrentTile.x + 1, CurrentTile.y), MazeTileType.Free))
        {
            transform.Translate(Vector3.right * movementSpeed * Time.deltaTime);
        } 
        else if(destWorld.x < transform.position.x && parentMaze.IsValidTileOfType(new Vector2Int(CurrentTile.x - 1, CurrentTile.y), MazeTileType.Free))
        {
            transform.Translate(-Vector3.right * movementSpeed * Time.deltaTime);
        }*/

        var oldTile = CurrentTile;
        // Notice on the player's behavior that using this approach, a new tile is computed for a player
        // as soon as his origin crosses the tile border. Therefore, the player now often stops somehow "in the middle".
        // For this demo code, it does not really matter but just keep this in mind when dealing with movement.
        var afterTranslTile = parentMaze.GetMazeTileForWorldPosition(transform.position);

        if(oldTile != afterTranslTile)
        {
            parentMaze.SetFreeTileColor(oldTile, Color.red);
            CurrentTile = afterTranslTile;
        }

        if(CurrentTile == GameManager.Instance.DestinationTile)
        {
            parentMaze.ResetTileColors();
            Debug.Log("YESSS");
        }
    }

    private IEnumerator CalculatePath(Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> closedSet = new List<Vector2Int>();
        SimplePriorityQueue<Vector2Int> openSet = new SimplePriorityQueue<Vector2Int>();

        openSet.Enqueue(start, Heuristic(start, goal));

        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();

        for(int row = 0; row < parentMaze.MazeTiles.Count; row++)
        {
            for (int col = 0; col < parentMaze.MazeTiles[0].Count; col++)
            {
                gScore.Add(new Vector2Int(col, row), Mathf.Infinity);
            }
        }
        gScore[start] = 0;
        Vector2Int current = start;

        while (openSet.Count > 0)
        {
            parentMaze.SetFreeTileColor(current, Color.red);
            current = openSet.Dequeue();
            parentMaze.SetFreeTileColor(current, Color.blue);
            yield return new WaitForEndOfFrame();
            if (current == goal)
            {
                StartCoroutine(ReconstructPath(cameFrom, current, start));
                break;
            }
            closedSet.Add(current);
            foreach(Vector2Int neighbor in parentMaze.GetNeighbors(current.y, current.x))
            {
                if (closedSet.Contains(neighbor))
                {
                    continue;
                }
                else
                {
                    parentMaze.SetFreeTileColor(neighbor, Color.green);
                    openSet.Enqueue(neighbor, gScore[neighbor] + Heuristic(neighbor, goal));
                }
                float tentative_gScore = gScore[current] + Heuristic(current, neighbor);
                if(tentative_gScore >= gScore[neighbor])
                {
                    continue;
                }
                cameFrom[neighbor] = current;
                gScore[neighbor] = tentative_gScore;
                openSet.UpdatePriority(neighbor, gScore[neighbor] + Heuristic(neighbor, goal));
                yield return new WaitForEndOfFrame();
            }
        }
    }

    private IEnumerator ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current, Vector2 start)
    {
        List<Vector2Int> total_path = new List<Vector2Int>();
        total_path.Add(current);
        while(current != start)
        {
            foreach(var wp in cameFrom.Keys)
            {
                if(wp == current)
                {
                    yield return new WaitForSeconds(0.1f);
                    current = cameFrom[wp];
                    total_path.Add(current);
                    parentMaze.SetFreeTileColor(current, Color.green);
                }
            }
        }
        total_path.Reverse();
        path = total_path;
    }


    private void ShowPath(Vector2Int start, Vector2Int goal)
    {
        /*var path = CalculatePath(start, goal);
        foreach (var tile in path)
        {
            parentMaze.SetFreeTileColor(tile, Color.red);
        }*/
    }

    private float Heuristic(Vector2Int p1, Vector2Int p2)
    {
        return Vector3.Distance(parentMaze.GetWorldPositionForMazeTile(p1), parentMaze.GetWorldPositionForMazeTile(p2));
    }

    // This function is called every time the user sets a new destination using a left mouse button
    protected virtual void OnDestinationChanged(Vector2Int newDestinationTile)
    {
        // TODO Assignment 2 ... this function might be of your interest. :-)
        // The destination tile index is also accessible via GameManager.Instance.DestinationTile
    }

    public virtual void InitializeData(Maze parentMaze, float movementSpeed, Vector2Int spawnTilePos)
    {
        this.parentMaze = parentMaze;

        // The multiplication below ensures that movement speed is considered in tile-units so it stays
        // consistent across different scales of the maze
        this.movementSpeed = movementSpeed * parentMaze.GetElementsScale().x; 

        transform.position = parentMaze.GetWorldPositionForMazeTile(spawnTilePos.x, spawnTilePos.y);
        transform.localScale = parentMaze.GetElementsScale();

        CurrentTile = spawnTilePos;

        isInitialized = true;
    }
}
