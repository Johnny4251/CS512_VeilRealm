using System.Collections.Generic;
using UnityEngine;

public class RandomBoardSetup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    
    [SerializeField] private Transform bottomLeftAnchor;

    [Header("World spacing between squares")]
    [SerializeField] private float cellSizeX = 4.65f;
    [SerializeField] private float cellSizeY = 4.65f;

    [Header("Piece Prefabs (add 40 each if doing classic Stratego)")]
    [SerializeField] private List<GameObject> redPrefabs;
    [SerializeField] private List<GameObject> bluePrefabs;

    [Header("Layout Settings")]
    [Tooltip("If true, use 4 rows per side like Stratego (0–3 for Red, 6–9 for Blue). If false, use half-board each.")]
    [SerializeField] private bool useStrategoLayout = true;

    [Tooltip("How many starting rows per side when using Stratego layout.")]
    [SerializeField] private int rowsPerSide = 4;

    [Tooltip("Automatically spawn pieces in Start()")]
    [SerializeField] private bool spawnOnStart = true;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnAll();
        }
    }

    [ContextMenu("Spawn Now")]
    public void SpawnAll()
    {
        if (boardManager == null)
        {
            Debug.LogError("RandomBoardSetup: BoardManager reference is not set.");
            return;
        }

        if (bottomLeftAnchor == null)
        {
            Debug.LogError("RandomBoardSetup: bottomLeftAnchor is not set.");
            return;
        }

        int rows = boardManager.GridRows;
        int cols = boardManager.GridCols;

        int redMinY, redMaxYInclusive;
        int blueMinY, blueMaxYInclusive;

        if (useStrategoLayout)
        {
            redMinY = 0;
            redMaxYInclusive = rowsPerSide - 1;   

            blueMinY = rows - rowsPerSide;       
            blueMaxYInclusive = rows - 1;       
        }
        else
        {
            redMinY = 0;
            redMaxYInclusive = (rows / 2) - 1;   
            blueMinY = rows / 2;                 
            blueMaxYInclusive = rows - 1;        
        }

        
        var redSlots  = BuildSlots(redMinY,  redMaxYInclusive, cols);
        var blueSlots = BuildSlots(blueMinY, blueMaxYInclusive, cols);

        Shuffle(redSlots);
        Shuffle(blueSlots);

        SpawnTeam(redPrefabs,  redSlots,  Team.RED);
        SpawnTeam(bluePrefabs, blueSlots, Team.BLUE);
    }

    private List<Vector2Int> BuildSlots(int minY, int maxYInclusive, int cols)
    {
        var list = new List<Vector2Int>();

        for (int y = minY; y <= maxYInclusive; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                
                if (boardManager.IsWallAt(x, y))
                    continue;

                list.Add(new Vector2Int(x, y));
            }
        }

        return list;
    }

    private void SpawnTeam(List<GameObject> prefabs, List<Vector2Int> slots, Team team)
    {
        if (prefabs == null || prefabs.Count == 0)
        {
            Debug.LogWarning($"RandomBoardSetup: No prefabs set for team {team}.");
            return;
        }

        if (prefabs.Count > slots.Count)
        {
            Debug.LogWarning(
                $"RandomBoardSetup: Not enough slots for team {team}. " +
                $"Pieces: {prefabs.Count}, Slots: {slots.Count}. Extra prefabs will not be spawned."
            );
        }

        int count = Mathf.Min(prefabs.Count, slots.Count);

        for (int i = 0; i < count; i++)
        {
            Vector2Int gridPos = slots[i];
            Vector3 worldPos   = GridToWorld(gridPos.x, gridPos.y);

            GameObject instance = Instantiate(prefabs[i], worldPos, Quaternion.identity);

            PieceController piece = instance.GetComponent<PieceController>();
            if (piece == null)
            {
                Debug.LogError($"RandomBoardSetup: Prefab '{prefabs[i].name}' has no PieceController component.");
                continue;
            }

            
            piece.Init(boardManager, team, gridPos.x, gridPos.y);
        }
    }

    private Vector3 GridToWorld(int x, int y)
    {
        return bottomLeftAnchor.position + new Vector3(
            x * cellSizeX,
            y * cellSizeY,
            0f
        );
    }

    private void Shuffle(List<Vector2Int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
