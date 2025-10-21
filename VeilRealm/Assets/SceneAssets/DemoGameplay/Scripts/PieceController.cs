using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ControlScheme
{
    public KeyCode up = KeyCode.UpArrow;
    public KeyCode down = KeyCode.DownArrow;
    public KeyCode left = KeyCode.LeftArrow;
    public KeyCode right = KeyCode.RightArrow;
}

[System.Serializable]
public enum PieceClass
{
    MARSHAL = 10, // 1
    GENERAL = 9, // 1
    COLONEL = 8, // 2
    MAJOR = 7, // 3
    CAPTAIN = 6, // 4
    LIEUTENANT = 5, // 4
    SERGEANT = 4, // 4
    MINER = 3, // 5
    SCOUT = 2, // 8

    SPY = 1, // 1

    /* Extras */
    BOMB = 100, // 6
    FLAG = 1000, // 1

    /* 40 pieces per board */
}

public enum Team
{
    RED,
    BLUE
}

public class PieceController : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private GameObject spotlight;

    [SerializeField] private int x;
    [SerializeField] private int y;

    [Header("Piece Move Values")]
    [SerializeField] private float pieceMoveValX = 4.65f;
    [SerializeField] private float pieceMoveValY = 4.65f;

    [Header("Spotlight Move Values")]
    [SerializeField] private float spotlightMoveValX = 4.65f;
    [SerializeField] private float spotlightMoveValY = 4.65f;

    [Header("Control Scheme")]
    [SerializeField] private ControlScheme controls;
    [SerializeField] public PieceClass pieceClass = PieceClass.SCOUT;
    [SerializeField] public Team team = Team.RED; 

    private bool isSelected = false;
    private readonly List<GameObject> activeHighlights = new List<GameObject>();

    void Start()
    {
        boardManager.RegisterPiece(this, x, y);
    }

    bool doIMove()
    {
        if (pieceClass == PieceClass.BOMB || pieceClass == PieceClass.FLAG)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    void Update()
    {
        if (!doIMove()) { return; }
        if (Input.GetKeyDown(controls.up))
        {
            if (boardManager.TryMovePiece(this, x, y, x, y + 1))
            {
                y += 1;
                transform.position = new Vector3(transform.position.x, transform.position.y + pieceMoveValY, 0);
                ClearHighlights();
            }
        }

        if (Input.GetKeyDown(controls.down))
        {
            if (boardManager.TryMovePiece(this, x, y, x, y - 1))
            {
                y -= 1;
                transform.position = new Vector3(transform.position.x, transform.position.y - pieceMoveValY, 0);
                ClearHighlights();
            }
        }

        if (Input.GetKeyDown(controls.left))
        {
            if (boardManager.TryMovePiece(this, x, y, x - 1, y))
            {
                x -= 1;
                transform.position = new Vector3(transform.position.x - pieceMoveValX, transform.position.y, 0);
                ClearHighlights();
            }
        }

        if (Input.GetKeyDown(controls.right))
        {
            if (boardManager.TryMovePiece(this, x, y, x + 1, y))
            {
                x += 1;
                transform.position = new Vector3(transform.position.x + pieceMoveValX, transform.position.y, 0);
                ClearHighlights();
            }
        }
    }

    private void OnMouseDown()
    {
        if (!doIMove()) { return; }
        if (isSelected)
        {
            isSelected = false;
            ClearHighlights();
        }
        else
        {
            isSelected = true;
            ShowPossibleMoves();
        }
    }

    private void ShowPossibleMoves()
    {
        ClearHighlights();

        var moves = boardManager.GetPossibleMoves(x, y, team);

        /* Free squares */
        foreach (var m in moves.freeSquares)
        {
            Vector3 worldPos = transform.position + new Vector3((m.x - x) * spotlightMoveValX, (m.y - y) * spotlightMoveValY, 0f);
            var hl = Instantiate(spotlight, worldPos, Quaternion.identity);
            activeHighlights.Add(hl);

            var sr = hl.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = Color.yellow;

            var marker = hl.GetComponent<SpotlightTarget>();
            if (marker == null) marker = hl.AddComponent<SpotlightTarget>();
            marker.Init(this, m);
        }

        /* Enemy Squares */
        foreach (var m in moves.enemySquares)
        {
            Vector3 worldPos = transform.position + new Vector3((m.x - x) * spotlightMoveValX, (m.y - y) * spotlightMoveValY, 0f);
            var hl = Instantiate(spotlight, worldPos, Quaternion.identity);
            activeHighlights.Add(hl);

            var sr = hl.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = Color.red;

            var marker = hl.GetComponent<SpotlightTarget>();
            if (marker == null) marker = hl.AddComponent<SpotlightTarget>();
            marker.Init(this, m); 
        }
    }


    private void ClearHighlights()
    {
        for (int i = 0; i < activeHighlights.Count; i++)
        {
            if (activeHighlights[i] != null) Destroy(activeHighlights[i]);
        }
        activeHighlights.Clear();
    }

    public void OnSpotlightClicked(Vector2Int target)
    {
        var targetObj = boardManager.GetPieceAt(target.x, target.y);
        if (targetObj != null)
        {
            var targetPiece = targetObj.GetComponent<PieceController>();
            if (targetPiece != null && targetPiece.team != team)
            {
                // ATTACK
                if (boardManager.AttackPiece(this, x, y, target.x, target.y))
                {
                    int dx = target.x - x;
                    int dy = target.y - y;

                    x = target.x;
                    y = target.y;

                    transform.position = new Vector3(
                        transform.position.x + dx * pieceMoveValX,
                        transform.position.y + dy * pieceMoveValY,
                        0f
                    );
                }
                ClearHighlights();
                isSelected = false;
                return;
            }
        }

        // NORMAL MOVE
        if (boardManager.TryMovePiece(this, x, y, target.x, target.y))
        {
            int dx = target.x - x;
            int dy = target.y - y;

            x = target.x;
            y = target.y;

            transform.position = new Vector3(
                transform.position.x + dx * pieceMoveValX,
                transform.position.y + dy * pieceMoveValY,
                0f
            );

            ClearHighlights();
            isSelected = false;
        }
    }

}
