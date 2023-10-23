using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class Board : MonoBehaviour
{
    ParticleManager m_particleManager;
    GamePiece[,] m_allGamePieces;
    
    [Header("Board Setting")]
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int borderSize;
    [Space]
    [SerializeField] private GameObject[] gamePiecePrefabs;     

    GameObject m_clickedTileBomb;
    GameObject m_targetTileBomb;
    [Space]
    [SerializeField] private float swapTime = 0.5f;
    [SerializeField] int falseYoffset = 10;
    [SerializeField] float moveTime = 0.5f;
    [Header("Starting GamePieces")]
    [SerializeField] StartingObjects[] startingGamePieces;

    public bool m_playerInputEnabled = true;
    int m_scoreMultiplier = 0;

    [System.Serializable]
    public class StartingObjects
    {
        public GameObject prefab;
        public int x;
        public int y;
        public int z;
    }

    void Start()
    {
        m_allTiles = new Tile[width, height];
        m_allGamePieces = new GamePiece[width, height];
        m_particleManager = FindObjectOfType<ParticleManager>();
        //SetupBoard();
        //HighlightMatches();
    }

    public void SetupBoard()
    {
        SetupTiles();
        SetupGamePieces();
        SetupCamera();
        FillBoard(falseYoffset, moveTime);
    }

    private void SetupCamera()
    {
        Camera.main.transform.position = new Vector3((float)(width - 1) / 2, (float)(height - 1) / 2, -10f);
        float aspecRatio = (float)Screen.width / (float)Screen.height;
        float verticalSize = (float)height / 2f + (float)borderSize;
        float horizontalSize = ((float)width / 2f + (float)borderSize) / aspecRatio;

        Camera.main.orthographicSize = (verticalSize > horizontalSize) ? verticalSize : horizontalSize;
    }

    GameObject GetRandomObject(GameObject[] objectArray)
    {
        int randIndex = Random.Range(0, objectArray.Length);
        if (objectArray[randIndex] == null)
        {
            Debug.LogWarning("Board : " + randIndex + " does not contain a valid GameObject!!");
        }
        return objectArray[randIndex];
    }
    GameObject GetRandomCollectable()
    {
        return GetRandomObject(collectablePrefabs);
    }
    GameObject GetRandomGamePiece()
    {
        return GetRandomObject(gamePiecePrefabs);
    }
    public void PlaceGamePiece(GamePiece gamePiece, int x, int y)
    {
        if (gamePiece == null)
        {
            Debug.LogWarning("Board : Invalid GamePiece!!");
            return;
        }
        gamePiece.transform.position = new Vector3(x, y, 0);
        if (IsWithinBounds(x, y))
        {
            m_allGamePieces[x, y] = gamePiece;
        }
        gamePiece.SetCoord(x, y);
    }
    bool IsWithinBounds(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);
    }
    void FillBoard(int falseYoffset = 0, float moveTime = 0.1f)
    {
        int maxInteractions = 100;
        int iterations = 0;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                GamePiece piece = null;

                if (m_allGamePieces[i, j] == null && m_allTiles[i, j].tileType != TileType.Obstacle)
                {
                    if (j == height - 1 && CanAddCollectable())
                    {
                        piece = FillRandomCollectableAt(i, j, falseYoffset, moveTime);
                        collectableCount++;
                    }
                    else
                    {
                        piece = FillRandomGamePieceAt(i, j, falseYoffset, moveTime);

                        while (HasMatchOnFill(i, j))
                        {
                            ClearPieceAt(i, j);
                            piece = FillRandomGamePieceAt(i, j, falseYoffset, moveTime);
                            iterations++;

                            if (iterations >= maxInteractions) break;
                        }
                    }
                }
            }
        }
    }
    private GamePiece FillRandomGamePieceAt(int i, int j, int falseYoffset = 0, float moveTime = 0.1f)
    {
        if (!IsWithinBounds(i, j)) return null;

        GameObject randomPiece = Instantiate(GetRandomGamePiece(), Vector3.zero, Quaternion.identity);
        if (randomPiece != null)
        {
            MakeGamePiece(randomPiece, i, j, falseYoffset, moveTime);
            return randomPiece.GetComponent<GamePiece>();
        }
        return null;
    }
    private GamePiece FillRandomCollectableAt(int i, int j, int falseYoffset = 0, float moveTime = 0.1f)
    {
        if (!IsWithinBounds(i, j)) return null;

        GameObject randomPiece = Instantiate(GetRandomCollectable(), Vector3.zero, Quaternion.identity);
        if (randomPiece != null)
        {
            MakeGamePiece(randomPiece, i, j, falseYoffset, moveTime);
            return randomPiece.GetComponent<GamePiece>();
        }
        return null;
    }
    private void ReplaceWithRamdom(List<GamePiece> gamePieces, int x, int y)
    {
        foreach (GamePiece piece in gamePieces)
        {
            ClearPieceAt(piece.xIndex, piece.yIndex);
            if (falseYoffset == 0)
            {
                FillRandomGamePieceAt(piece.xIndex, piece.yIndex);
            }
            else
            {
                FillRandomGamePieceAt(piece.xIndex, piece.yIndex, falseYoffset, moveTime);
            }
        }
    }
    private void MakeGamePiece(GameObject piece, int i, int j, int falseYoffset = 0, float moveTime = 0.1f)
    {
        if (piece != null && IsWithinBounds(i, j))
        {
            piece.GetComponent<GamePiece>().Init(this);

            PlaceGamePiece(piece.GetComponent<GamePiece>(), i, j);

            if (falseYoffset != 0)
            {
                piece.transform.position = new Vector3(i, j + falseYoffset, 0);
                piece.GetComponent<GamePiece>().Move(i, j, moveTime);
            }

            piece.transform.parent = transform;
        }
    }
    private GameObject MakeBomb(GameObject piece, int i, int j)
    {
        if (piece != null && IsWithinBounds(i, j))
        {
            GameObject bomb = Instantiate(piece, new Vector3(i, j, 0), Quaternion.identity);
            bomb.GetComponent<Bomb>().Init(this);
            bomb.GetComponent<Bomb>().SetCoord(i, j);
            bomb.transform.parent = transform;
            return bomb;
        }
        return null;
    }
    private bool HasMatchOnFill(int x, int y, int minLength = 3)
    {
        List<GamePiece> leftMatches = FindMatches(x, y, new Vector2(-1, 0), minLength);
        List<GamePiece> downMatches = FindMatches(x, y, new Vector2(0, -1), minLength);

        if (leftMatches == null)
        {
            leftMatches = new List<GamePiece>();
        }
        if (downMatches == null)
        {
            downMatches = new List<GamePiece>();
        }

        return (leftMatches.Count > 0 || downMatches.Count > 0);
    }
    void SetupGamePieces()
    {
        foreach (StartingObjects sPiece in startingGamePieces)
        {
            if (sPiece != null)
            {
                GameObject piece = Instantiate(sPiece.prefab, new Vector3(sPiece.x, sPiece.y, 0), Quaternion.identity);
                MakeGamePiece(piece, sPiece.x, sPiece.y, falseYoffset, moveTime);
            }
        }
    }    
          
    void ClearPieceAt(int x, int y)
    {
        GamePiece pieceToClear = m_allGamePieces[x, y];

        if (pieceToClear != null)
        {
            m_allGamePieces[x, y] = null;
            Destroy(pieceToClear.gameObject);
        }
        //HighlightTileOff(x, y);
    }
    void ClearPieceAt(List<GamePiece> gamePieces, List<GamePiece> bombedPieces)
    {
        int bonus = 0;

        foreach (var piece in gamePieces)
        {
            if (piece != null)
            {
                ClearPieceAt(piece.xIndex, piece.yIndex);

                if (gamePieces.Count >= 4)
                    bonus = 20;

                piece.Scorepoints(bonus, m_scoreMultiplier);

                if (m_particleManager != null)
                {
                    if (bombedPieces.Contains(piece))
                    {
                        m_particleManager.BombFXAt(piece.xIndex, piece.yIndex);
                    }
                    else
                    {
                        m_particleManager.ClearPieceFXAt(piece.xIndex, piece.yIndex);
                    }
                }
            }
        }
    }
    void ClearBoard()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                ClearPieceAt(i, j);
            }
        }
    }

    List<GamePiece> CollapseColumn(int column, float collapseTime = 0.1f)
    {
        List<GamePiece> movingPieces = new List<GamePiece>();
        for (int i = 0; i < height - 1; i++)
        {
            if (m_allGamePieces[column, i] == null && m_allTiles[column, i].tileType != TileType.Obstacle)
            {
                for (int j = i + 1; j < height; j++)
                {
                    if (m_allGamePieces[column, j] != null)
                    {
                        m_allGamePieces[column, j].Move(column, i, collapseTime * (j - i));
                        m_allGamePieces[column, i] = m_allGamePieces[column, j];
                        m_allGamePieces[column, i].SetCoord(column, i);

                        if (!movingPieces.Contains(m_allGamePieces[column, i]))
                        {
                            movingPieces.Add(m_allGamePieces[column, i]);
                        }
                        m_allGamePieces[column, j] = null;
                        break;
                    }
                }
            }
        }
        return movingPieces;
    }
    List<int> GetColumns(List<GamePiece> gamePieces)
    {
        List<int> columns = new List<int>();
        foreach (GamePiece piece in gamePieces)
        {
            if (!columns.Contains(piece.xIndex)) columns.Add(piece.xIndex);
        }
        return columns;
    }
    List<GamePiece> CollapseColumn(List<GamePiece> gamePieces)
    {
        List<GamePiece> movingPiece = new List<GamePiece>();
        List<int> columnsToCollapse = GetColumns(gamePieces);

        foreach (int column in columnsToCollapse)
        {
            movingPiece = movingPiece.Union(CollapseColumn(column)).ToList();
        }
        return movingPiece;
    }
    void ClearAndRefillBoard(List<GamePiece> gamePieces)
    {
        StartCoroutine(ClearAndRefillBoardRoutine(gamePieces));
    }
    IEnumerator ClearAndRefillBoardRoutine(List<GamePiece> gamePieces)
    {
        m_playerInputEnabled = false;
        List<GamePiece> matches = gamePieces;
        m_scoreMultiplier = 0;
        do
        {
            m_scoreMultiplier++;
            yield return StartCoroutine(ClearAndCollapseRoutine(matches));
            yield return null;
            //refill
            yield return StartCoroutine(RefillRoutine());
            matches = FindAllMatches();
            yield return new WaitForSeconds(0.5f);

        } while (matches.Count != 0);

        m_playerInputEnabled = true;
    }
    IEnumerator RefillRoutine()
    {
        FillBoard(falseYoffset, moveTime);
        yield return null;
    }
    IEnumerator ClearAndCollapseRoutine(List<GamePiece> gamePieces)
    {
        List<GamePiece> movingPieces = new List<GamePiece>();
        List<GamePiece> matches = new List<GamePiece>();

        //HighlightMatchesAt(gamePieces);
        yield return new WaitForSeconds(0.25f);
        bool isFinished = false;
        while (!isFinished)
        {
            List<GamePiece> bombedPieces = GetBombedPieces(gamePieces);
            gamePieces = gamePieces.Union(bombedPieces).ToList();

            bombedPieces = GetBombedPieces(gamePieces);
            gamePieces = gamePieces.Union(bombedPieces).ToList();

            List<GamePiece> collectablePieces = FindCollectablesAt(0, true);
            List<GamePiece> allCollectablePieces = FindAllCollectables();
            List<GamePiece> blockers = gamePieces.Intersect(allCollectablePieces).ToList();
            collectablePieces = collectablePieces.Union(blockers).ToList();
            collectableCount -= collectablePieces.Count;

            gamePieces = gamePieces.Union(collectablePieces).ToList();

            ClearPieceAt(gamePieces, bombedPieces);
            BreakTileAt(gamePieces);

            if (m_clickedTileBomb != null)
            {
                ActivateBomb(m_clickedTileBomb);
                m_clickedTileBomb = null;
            }

            if (m_targetTileBomb != null)
            {
                ActivateBomb(m_targetTileBomb);
                m_targetTileBomb = null;
            }

            yield return new WaitForSeconds(0.2f);

            movingPieces = CollapseColumn(gamePieces);

            while (!IsCollapsed(movingPieces))
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);

            matches = FindMatchesAt(movingPieces);
            collectablePieces = FindCollectablesAt(0, true);
            //collectableCount -= collectablePieces.Count;
            matches = matches.Union(collectablePieces).ToList();

            if (matches.Count == 0)
            {
                isFinished = true;
                break;
            }
            else
            {
                m_scoreMultiplier++;
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayBonusSound();
                }
                yield return StartCoroutine(ClearAndCollapseRoutine(matches));
            }
        }
        yield return null;
    }
    bool IsCollapsed(List<GamePiece> gamePieces)
    {
        foreach (var piece in gamePieces)
        {
            if (piece != null)
                if (piece.transform.position.y - (float)piece.yIndex > 0.001f)
                    return false;
        }
        return true;
    }
}
   



































































































































































































































































































































































































































































//바경준 왔다감.
