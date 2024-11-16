using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using System.Linq;

public class MazeGenerator : MonoBehaviour
{
    public GameObject[] tiles; // Array of tiles representing maze parts

    public GameObject player; // Player object
    public GameObject enemyType1; // First enemy type
    public GameObject enemyType2; // Second enemy type
    public GameObject flag; // Flag object
    public GameObject itemPrefab; // Item prefab
    public GameObject rockPrefab; // Rock prefab

    const int N = 1; // North wall
    const int E = 2; // East wall
    const int S = 4; // South wall
    const int W = 8; // West wall

    Dictionary<Vector2, int> cell_walls = new Dictionary<Vector2, int>(); // Wall directions

    float tile_size = 10; // Size of each tile
    public int width = 10; // Width of the maze
    public int height = 10; // Height of the maze

    List<List<int>> map = new List<List<int>>(); // Maze representation
    List<Vector2> validPositions = new List<Vector2>(); // Valid positions for objects

    void Start()
    {
        // Initialize wall directions
        cell_walls[new Vector2(0, -1)] = N;
        cell_walls[new Vector2(1, 0)] = E;
        cell_walls[new Vector2(0, 1)] = S;
        cell_walls[new Vector2(-1, 0)] = W;

        // Generate the maze
        MakeMaze();

        // Instantiate the player at the center of the calculated position
        InstantiatePlayer();

        // Spawn enemies
        SpawnEnemies();

        // Place the flag at the farthest position
        PlaceFlag();

        // Spawn an item near the player
        SpawnItemNearPlayer();

        // Spawn a rock
        SpawnRockInMaze();
    }

    private void InstantiatePlayer()
    {
        // Default to the first tile (0,0)
        Vector2 playerStart = new Vector2(0, 0);

        // Check if (0,0) is a valid position
        if (!validPositions.Contains(playerStart))
        {
            playerStart = validPositions.First();
        }

        // Calculate the center of the tile
        float centerX = playerStart.y * tile_size + (tile_size / 2);
        float centerZ = playerStart.x * tile_size + (tile_size / 2);
        float centerY = 1.0f; // Ensure player spawns above the ground

        // Instantiate the player at the calculated position
        GameObject p = GameObject.Instantiate(player);
        p.transform.position = new Vector3(centerX, centerY, centerZ); // Set height explicitly

        // Remove the player's position from valid positions
        validPositions.Remove(playerStart);

        Debug.Log($"Player spawned at: {playerStart} with position ({centerX}, {centerY}, {centerZ})");
    }



    private void MakeMaze()
    {
        List<Vector2> unvisited = new List<Vector2>();
        List<Vector2> stack = new List<Vector2>();

        for (int i = 0; i < width; i++)
        {
            map.Add(new List<int>());
            for (int j = 0; j < height; j++)
            {
                map[i].Add(N | E | S | W);
                unvisited.Add(new Vector2(i, j));
                validPositions.Add(new Vector2(i, j)); // Add valid positions
            }
        }

        Vector2 current = new Vector2(0, 0);
        unvisited.Remove(current);

        while (unvisited.Count > 0)
        {
            List<Vector2> neighbors = CheckNeighbors(current, unvisited);

            if (neighbors.Count > 0)
            {
                Vector2 next = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];
                stack.Add(current);

                Vector2 dir = next - current;

                int current_walls = map[(int)current.x][(int)current.y] - cell_walls[dir];
                int next_walls = map[(int)next.x][(int)next.y] - cell_walls[-dir];

                map[(int)current.x][(int)current.y] = current_walls;
                map[(int)next.x][(int)next.y] = next_walls;

                current = next;
                unvisited.Remove(current);
            }
            else if (stack.Count > 0)
            {
                current = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
            }
        }

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                GameObject tile = GameObject.Instantiate(tiles[map[i][j]]);
                tile.transform.parent = gameObject.transform;

                tile.transform.Translate(new Vector3(j * tile_size, 0, i * tile_size));
                tile.name += $" {i} {j}";
                tile.GetComponentInChildren<NavMeshSurface>().BuildNavMesh();
            }
        }
    }

    private List<Vector2> CheckNeighbors(Vector2 cell, List<Vector2> unvisited)
    {
        List<Vector2> list = new List<Vector2>();

        foreach (var n in cell_walls.Keys)
        {
            if (unvisited.Contains(cell + n))
            {
                list.Add(cell + n);
            }
        }
        return list;
    }

    private void SpawnEnemies()
    {
        Vector2 randomPosition1 = GetRandomValidPosition();
        GameObject enemy1 = GameObject.Instantiate(enemyType1);
        enemy1.transform.position = new Vector3(randomPosition1.y * tile_size + (tile_size / 2), 0f, randomPosition1.x * tile_size + (tile_size / 2));

        Vector2 randomPosition2 = GetRandomValidPosition();
        GameObject enemy2 = GameObject.Instantiate(enemyType2);
        enemy2.transform.position = new Vector3(randomPosition2.y * tile_size + (tile_size / 2), 0f, randomPosition2.x * tile_size + (tile_size / 2));
    }

    private void PlaceFlag()
    {
        Vector2 farthestPosition = validPositions
            .OrderByDescending(pos => Vector2.Distance(new Vector2(width / 2, height / 2), pos))
            .First();

        validPositions.Remove(farthestPosition);

        GameObject flagObject = GameObject.Instantiate(flag);
        flagObject.transform.position = new Vector3(farthestPosition.y * tile_size + (tile_size / 2), 0f, farthestPosition.x * tile_size + (tile_size / 2));
    }

    private Vector2 GetRandomValidPosition()
    {
        if (validPositions.Count == 0)
        {
            Debug.LogError("No valid positions available!");
            return new Vector2(0, 0); // Default to (0,0) if no valid positions are available
        }

        int index = UnityEngine.Random.Range(0, validPositions.Count);
        Vector2 position = validPositions[index];
        validPositions.RemoveAt(index);
        return position;
    }

    private void SpawnItemNearPlayer()
    {
        Vector2 closestPosition = validPositions
            .OrderBy(pos => Vector2.Distance(new Vector2(width / 2, height / 2), pos))
            .FirstOrDefault();

        if (closestPosition == default(Vector2))
        {
            Debug.LogWarning("No valid positions for item spawn.");
            return;
        }

        validPositions.Remove(closestPosition);

        Vector3 itemPosition = new Vector3(closestPosition.y * tile_size + (tile_size / 2), 0.5f, closestPosition.x * tile_size + (tile_size / 2));
        GameObject item = GameObject.Instantiate(itemPrefab);
        item.transform.position = itemPosition;
    }

    private void SpawnRockInMaze()
    {
        Vector2 randomPosition = GetRandomValidPosition();

        Vector3 rockPosition = new Vector3(randomPosition.y * tile_size + (tile_size / 2), 0.5f, randomPosition.x * tile_size + (tile_size / 2));
        GameObject rock = GameObject.Instantiate(rockPrefab);
        rock.transform.position = rockPosition;
    }
}
