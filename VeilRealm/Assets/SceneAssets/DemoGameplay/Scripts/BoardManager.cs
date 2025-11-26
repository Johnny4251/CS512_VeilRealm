using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct MoveOptions
{
    public List<Vector2Int> freeSquares;
    public List<Vector2Int> enemySquares;
}

public class BoardManager : MonoBehaviour
{
    private GameObject[,] grid;

    private bool gameOver = false;
    public bool ISGameOver => gameOver;
        
    private bool redWins = false;
    private bool blueWins = false;

    private int redCaptures = 0;
    private int blueCaptures = 0;
    [SerializeField] private int gridSizeRows = 10;
    [SerializeField] private int gridSizeCols = 10;

    public int GridRows => gridSizeRows;
    public int GridCols => gridSizeCols;


    [SerializeField] public bool redMove = true;
    [SerializeField] public bool blueMove = false;

    [SerializeField] private Camera mainCamera;
    [SerializeField] private TMPro.TextMeshProUGUI continueText;

    [SerializeField] private float zoomOutZ = -1000f;
    [SerializeField] private float zoomDuration = 2f;
    [SerializeField] private Vector3 cameraOriginalPos;
    private bool waitingForContinue = false;

    public AudioClip attackSound;
    public AudioClip killSound;
    public AudioClip explosionSound;
    public AudioClip gameOverSound;

    public GameObject gameOverUI;

    public TMPro.TextMeshProUGUI winnerstats;
    public TMPro.TextMeshProUGUI loserstats;
    public TMPro.TextMeshProUGUI reasonwin;

    [Header("Predefined Obstacles")]
    [SerializeField]

    private Vector2Int[] walls =
    {
        new Vector2Int(6, 5),
        new Vector2Int(6, 4),
        new Vector2Int(3, 5),
        new Vector2Int(3, 4),
    };

    private static readonly Vector2Int[] kFourDirs = new[]
    {
        new Vector2Int(1, 0),  // right
        new Vector2Int(-1, 0), // left
        new Vector2Int(0, 1),  // up
        new Vector2Int(0, -1), // down
        };

    private HashSet<Vector2Int> wallSet;

    private PieceController selectedPiece;

    public bool IsWallAt(int x, int y)
    {
        return wallSet.Contains(new Vector2Int(x, y));
    }

    void Awake()
    {
        // x first, y second
        grid = new GameObject[gridSizeCols, gridSizeRows];

        wallSet = new HashSet<Vector2Int>(walls ?? new Vector2Int[0]);
        foreach (var w in wallSet)
        {
            if (!InBounds(w.x, w.y))
                Debug.LogWarning($"Wall {w} is out of bounds.");
        }

        if (mainCamera != null)
            cameraOriginalPos = mainCamera.transform.position;

        if (continueText != null)
            continueText.gameObject.SetActive(false);
        UpdatePieceVisibility();
    }
    

    private bool InBounds(int x, int y) =>
        x >= 0 && x < gridSizeCols && y >= 0 && y < gridSizeRows;
    private bool IsWall(int x, int y) =>
        wallSet.Contains(new Vector2Int(x, y));

    private bool IsFree(int x, int y)
    {
        if (!InBounds(x, y))
            return false;

        if (IsWall(x, y))
            return false;

        if (grid[x, y] != null)
            return false;

        return true;
    }

    public void UpdatePieceVisibility()
    {
        foreach (var pieceObj in grid)
        {
            if (pieceObj == null) continue;

            var piece = pieceObj.GetComponent<PieceController>();
            if (piece == null) continue;

            // Hide the other team’s pieces
            if (redMove && piece.team == Team.BLUE)
                piece.HidePiece();
            else if (blueMove && piece.team == Team.RED)
                piece.HidePiece();
            else
                piece.RevealPiece();
        }
    }

    private void CheckForMovablePieces()
    {
        // bool redFlagExists = false;
        // bool blueFlagExists = false;
        bool redHasMovable = false;
        bool blueHasMovable = false;

        foreach (var obj in grid)
        {
            if (obj == null) continue;
            var piece = obj.GetComponent<PieceController>();
            if (piece == null) continue;

            if (piece.team == Team.RED)
            {
                // if (piece.pieceClass == PieceClass.FLAG)
                //     redFlagExists = true;
                if (piece.pieceClass != PieceClass.FLAG && piece.pieceClass != PieceClass.BOMB)
                    redHasMovable = true;
            }
            else if (piece.team == Team.BLUE)
            {
                // if (piece.pieceClass == PieceClass.FLAG)
                //     blueFlagExists = true;
                if (piece.pieceClass != PieceClass.FLAG && piece.pieceClass != PieceClass.BOMB)
                    blueHasMovable = true;
            }
        }

        // if (!redFlagExists)
        // {
        //     blueWins = true;
        //     HandleGameOver(Team.BLUE, Team.RED, false);
        // }
        // else if (!blueFlagExists)
        // {
        //     redWins = true;
        //     HandleGameOver(Team.RED, Team.BLUE, false);
        // }

        // Check for tie
        if (!redHasMovable && !blueHasMovable)
        {
            // Draw condition - both players have no moves
            HandleGameOver(Team.RED, Team.BLUE, true, false);
        }
        else if (!redHasMovable)
        {
            blueWins = true;
            HandleGameOver(Team.BLUE, Team.RED, false, false);
        }
        else if (!blueHasMovable)
        {
            redWins = true;
            HandleGameOver(Team.RED, Team.BLUE, false, false);
        }
    }

    private string GetTeamColor(Team team)
    {
        return team == Team.RED ? "#FF4C4C" : "#4CB2FF";
    }


    private IEnumerator AnimateTurnTransition(string playerName, string attackSummary = null)
    {
        waitingForContinue = true;

        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.position;
        Vector3 targetPos = new Vector3(startPos.x, startPos.y, zoomOutZ);
        RectTransform rect = continueText.GetComponent<RectTransform>();
        Vector2 clashPos = new Vector2(100, 80);  
        Vector2 movePos = new Vector2(120, 25); 

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / zoomDuration);
            yield return null;
        }
        continueText.text = string.Empty;
        if (!string.IsNullOrEmpty(attackSummary))
        {
            continueText.text = $"{attackSummary}\n\n";
            rect.anchoredPosition = clashPos;
        }
        else
        {
            rect.anchoredPosition = movePos;
        }

        string colorTag = playerName.Contains("Red") ? "#FF4C4C" : "#4CB2FF";
        continueText.text += $"<b><color={colorTag}>{playerName}</color></b>\nPress <b><color=#00FFFF>SPACE</color></b> to continue";

        continueText.gameObject.SetActive(true);

        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        continueText.gameObject.SetActive(false);

        elapsed = 0f;
        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            mainCamera.transform.position = Vector3.Lerp(targetPos, cameraOriginalPos, elapsed / zoomDuration);
            yield return null;
        }

        waitingForContinue = false;
    }



    private void GetPossibleMovesHelper(int x, int y, List<Vector2Int> results)
    {
        results.Clear();

        if (!InBounds(x, y) || IsWall(x, y))
            return;

        foreach (var d in kFourDirs)
        {
            int nx = x + d.x;
            int ny = y + d.y;
            if (IsFree(nx, ny))
                results.Add(new Vector2Int(nx, ny));
        }
    }

    public GameObject GetPieceAt(int x, int y)
    {
        if (!InBounds(x, y)) return null;
        return grid[x, y];
    }

    public MoveOptions GetPossibleMoves(int x, int y, Team team)
    {
        MoveOptions result = new MoveOptions
        {
            freeSquares = new List<Vector2Int>(),
            enemySquares = new List<Vector2Int>()
        };

        if (team == Team.BLUE && !blueMove)
        {
            return result;
        } 

        if(team == Team.RED && !redMove)
        {
            return result;
        } 
        
        var pieceObj = GetPieceAt(x, y);
        if (pieceObj == null)
        {
            return result;
        }
        var piece = pieceObj.GetComponent<PieceController>();
        if (piece == null)
        {
            return result;
        }


        int maxSteps;
        if (piece.pieceClass == PieceClass.SCOUT)
        {
            maxSteps = Mathf.Max(gridSizeCols, gridSizeRows);
        }
        else
        {
            maxSteps = 1;
        }


        foreach (var d in kFourDirs)
        {
            for (int step = 1; step <= maxSteps; step++)
            {
                int nx = x + d.x * step;
                int ny = y + d.y * step;

                if (!InBounds(nx, ny) || IsWall(nx, ny))
                    break;

                var occupant = grid[nx, ny];
                if (occupant == null)
                {
                    result.freeSquares.Add(new Vector2Int(nx, ny));
                }
                else
                {
                    var otherPiece = occupant.GetComponent<PieceController>();
                    if (otherPiece != null && otherPiece.team != team)
                    {
                        result.enemySquares.Add(new Vector2Int(nx, ny));
                    }
                    break;
                }
            }
        }

        return result;
    }

    public bool TryMovePiece(PieceController piece, int fromX, int fromY, int toX, int toY)
    {
        if (!IsFree(toX, toY))
            return false;

        if (!InBounds(fromX, fromY) || IsWall(fromX, fromY))
            return false;

        if (piece.team == Team.BLUE && !blueMove)
        {
            return false;
        } else if(piece.team == Team.BLUE && blueMove)
        {
            blueMove = false;
            redMove = true;
            StartCoroutine(AnimateTurnTransition("Red Player"));
            UpdatePieceVisibility();
        }

        if(piece.team == Team.RED && !redMove)
        {
            return false;
        } else if(piece.team == Team.RED && redMove)
        {
            blueMove = true;
            redMove = false;
            StartCoroutine(AnimateTurnTransition("Blue Player"));
            UpdatePieceVisibility();
        }

        if (grid[fromX, fromY] == null)
            grid[fromX, fromY] = piece.gameObject;

        grid[fromX, fromY] = null;
        grid[toX, toY] = piece.gameObject;

        Debug.Log($"{piece.name} moved {fromX},{fromY} -> {toX},{toY}");
        return true;
    }

    public void RegisterPiece(PieceController piece, int x, int y)
    {
        if (!InBounds(x, y))
        {
            Debug.LogError($"RegisterPiece out of bounds: {x},{y}");
            return;
        }

        grid[x, y] = piece.gameObject;

        // Ensure visibility is correct whenever a piece is placed/registered.
        UpdatePieceVisibility();
    }


    private void ToggleTurnAfterAttack(Team attacker)
    {
        if (attacker == Team.RED)
        {
            redMove = false;
            blueMove = true;
        }
        else
        {
            redMove = true;
            blueMove = false;
        }

        UpdatePieceVisibility();
    }

    public bool AttackPiece(PieceController attacker, int fromX, int fromY, int toX, int toY)
    {
        if (!InBounds(toX, toY))
            return false;

        if (attacker.team == Team.BLUE && !blueMove)
            return false;
            
        if (attacker.team == Team.RED && !redMove)
            return false;

        if (AudioManager.Instance != null && attackSound != null)
        {
            AudioManager.Instance.PlayOneShot(attackSound);
        }
        
        var targetObj = grid[toX, toY];
        if (targetObj == null)
            return false;

        var defender = targetObj.GetComponent<PieceController>();
        if (defender == null)
            return false;

        bool attackerWins = false;
        bool bothDie = false;
        // if (defender.pieceClass == PieceClass.FLAG)
        // {
        //     attackerWins = true;
        // }
        // else if (defender.pieceClass == PieceClass.BOMB)

        if (defender.pieceClass == PieceClass.FLAG)
        {

            string summary2 = $"The {attacker.team} captured the {defender.team}'s FLAG!\nThe {attacker.team} team wins the game!";
            Debug.Log(summary2);

            Destroy(defender.gameObject);
            grid[toX, toY] = attacker.gameObject;
            grid[fromX, fromY] = null;

            HandleGameOver(attacker.team, defender.team, false, true);
            if (!gameOver)
            {
                StartCoroutine(AnimateTurnTransition(GetNextPlayerName(attacker.team), summary2));
            }

            return true;
        }
        
        if (defender.pieceClass == PieceClass.BOMB)
        {
            if (attacker.pieceClass == PieceClass.MINER)
                attackerWins = true;
            else
                attackerWins = false;
        }
        else if (attacker.pieceClass == PieceClass.SPY && defender.pieceClass == PieceClass.MARSHAL)
        {
            attackerWins = true;
        }
        else
        {
            int atk = (int)attacker.pieceClass;
            int def = (int)defender.pieceClass;

            if (atk > def)
                attackerWins = true;
            else if (atk == def)
                bothDie = true;
            else
                attackerWins = false;
        }

        if ((defender.pieceClass == PieceClass.BOMB) && (attackerWins == false))
        {
            if (AudioManager.Instance != null && explosionSound != null)
            {
                AudioManager.Instance.PlayOneShot(explosionSound);
            }
        }
        else
        {
            if (AudioManager.Instance != null && attackSound != null)
            {
                AudioManager.Instance.PlayOneShot(attackSound);
            }
        }

        if (AudioManager.Instance != null && killSound != null)
        {
            AudioManager.Instance.PlayOneShot(killSound);
        }

        string attackerColor = GetTeamColor(attacker.team);
        string defenderColor = GetTeamColor(defender.team);
        string actionColor = "#C080FF";
        string neutralColor = "#A0A0A0";

        string summary = $"The <b><color={attackerColor}>{attacker.team} {attacker.pieceClass}</color></b> " +
                        $"<color={actionColor}>engaged</color> the <b><color={defenderColor}>{defender.team} {defender.pieceClass}</color></b>!\n";

        if (bothDie)
        {
            blueCaptures++;
            redCaptures++;
            Destroy(attacker.gameObject);
            Destroy(defender.gameObject);
            grid[fromX, fromY] = null;
            grid[toX, toY] = null;

            string[] phrases = { "Both pieces perished in battle!", "Neither survived the clash!", "A mutual destruction occurred!", "Both warriors fell!" };
            summary += $"<b><color={neutralColor}>{phrases[Random.Range(0, phrases.Length)]}</color></b>";

            Debug.Log($"{attacker.name} and {defender.name} both died!");
            CheckForMovablePieces();
            if (!gameOver)
            {
                StartCoroutine(AnimateTurnTransition(GetNextPlayerName(attacker.team), summary));
                ToggleTurnAfterAttack(attacker.team);
            }
            return true;
        }

        if (attackerWins)
        {
            if (attacker.team == Team.RED)
            {
                redCaptures++;
            }
            else
            {
                blueCaptures++;
            }
            Destroy(defender.gameObject);
            grid[toX, toY] = attacker.gameObject;
            grid[fromX, fromY] = null;

            string[] verbs = { "destroyed", "defeated", "obliterated", "demolished", "crushed", "slayed" };
            string action = verbs[Random.Range(0, verbs.Length)];

            summary += $"The <b><color={attackerColor}>{attacker.team} {attacker.pieceClass}</color></b> " +
                       $"<color={actionColor}>{action}</color> the <b><color={defenderColor}>{defender.team} {defender.pieceClass}</color></b>!";

            Debug.Log($"{attacker.name} defeated {defender.name}!");
            CheckForMovablePieces();
            if (!gameOver)
            {
                StartCoroutine(AnimateTurnTransition(GetNextPlayerName(attacker.team), summary));
                ToggleTurnAfterAttack(attacker.team);
            }
            return true;
        }
        else
        {
            if (defender.team == Team.RED)
            {
                redCaptures++;
            }
            else
            {
                blueCaptures++;
            }
            Destroy(attacker.gameObject);
            grid[fromX, fromY] = null;

            string[] verbs = { "defended bravely against", "repelled", "vanquished", "overpowered", "outsmarted" };
            string action = verbs[Random.Range(0, verbs.Length)];

            summary += $"The <b><color={defenderColor}>{defender.team} {defender.pieceClass}</color></b> " +
                       $"<color={actionColor}>{action}</color> the <b><color={attackerColor}>{attacker.team} {attacker.pieceClass}</color></b>!";

            Debug.Log($"{defender.name} defeated {attacker.name}!");
            CheckForMovablePieces();
            if (!gameOver)
            {
                StartCoroutine(AnimateTurnTransition(GetNextPlayerName(attacker.team), summary));
                ToggleTurnAfterAttack(attacker.team);
            }
            return true;
        }
    }

    private string GetNextPlayerName(Team current)
    {
        return current == Team.RED ? "Blue Player" : "Red Player";
    }

    void HandleGameOver(Team winner, Team loser, bool tied, bool flagTaken)
    {
        gameOver = true;
        zoomDuration = 0;
        continueText = null;
        string neutralColor = "#A0A0A0";
        if (tied)
        {
            Debug.Log("Game is a tie!");
            gameOverUI.SetActive(true);
            string redColor = GetTeamColor(Team.RED);
            string blueColor = GetTeamColor(Team.BLUE);

            winnerstats.text = $"Game is a tie! Red took {redCaptures} pieces!";
            loserstats.text = $"Both teams fought valiantly! Blue took {blueCaptures} pieces!";
            string reasonText = $"\n<b><color={neutralColor}>NO MOVABLE PIECES</color></b>";
            reasonwin.text = reasonText;
        }
        else
        {
            Debug.Log("Winner is: " + winner + "\nLoser is: " + loser);
            string winnerColor = GetTeamColor(winner);
            string loserColor = GetTeamColor(loser);

            gameOverUI.SetActive(true);
            string winnerText = $"<b>{winner} WON</b> - Captured <color={winnerColor}>{(winner == Team.RED ? redCaptures : blueCaptures)}</color> pieces.";
            string loserText = $"<b>{loser} LOST</b> - Captured <color={loserColor}>{(loser == Team.RED ? redCaptures : blueCaptures)}</color> pieces.";
    
            if (flagTaken)
            {
                string reasonText = $"\n<b>{loser}'s FLAG</b> was Captured!";
                reasonwin.text = reasonText;
            }
            else
            {
                string reasonText = $"\n<b>{loser}</b> had <b><color={neutralColor}>NO MOVABLE PIECES</color></b> left!";
                reasonwin.text = reasonText;
            }

            winnerstats.text = winnerText;
            loserstats.text = loserText;
        }
        redCaptures = 0;
        blueCaptures = 0;

        if (AudioManager.Instance != null && gameOverSound != null)
        {
            AudioManager.Instance.PlayOneShot(gameOverSound);
        }

    }

    /*
    void Update()
    {
        Debug.Log(grid[0, 0].GetComponent<PieceController>().team);
        Debug.Log(grid[0, 0].GetComponent<PieceController>().pieceClass);
    }
    */

}
