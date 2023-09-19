using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int boardSize;

    [SerializeField] private GameObject tileNormalPrefab;
    [SerializeField] private GameObject tileObstaclePrefab;
    [SerializeField] private GameObject[] gamePiecePrefabs;

    [SerializeField] private GameObject adjacentBombPrefab;
    [SerializeField] private GameObject columnBombPrefab;
    [SerializeField] private GameObject rowBombPrefab;
    [SerializeField] private GameObject colorBombPrefab;

    [SerializeField] private float swapTime = 0.5f;

    GameObject m_targetTileBomb;
    GameObject m_clickedTileBomb;

    Tile[,] m_allTiles;
    GamePiece[,] m_allGamePieces;

    ParticleManager m_particleManager;

    Tile m_clickedTile;
    Tile m_targetTile;

    bool m_playerInputEnabled = true;
    int falseYoffset = 10;
    float moveTime = 0.5f;

    public StartingObjects[] startingTiles;
    public StartingObjects[] startingGamePieces;

    [System.Serializable] // 보이게 만들기
    public class StartingObjects
    {
        public GameObject Prefab;
        public int x;
        public int y;
        public int z;
    }

    private void Start()
    {
        m_allTiles = new Tile[width, height];
        m_allGamePieces = new GamePiece[width, height];
        m_particleManager = FindObjectOfType<ParticleManager>();

        SetupTiles();
        SetupGamePieces();
        SetupCamera();
        FillBoard(falseYoffset, moveTime);
    }

    GameObject GetRandomGamePiece()
    {
        int randIndex = Random.Range(0, gamePiecePrefabs.Length);

        if (gamePiecePrefabs[randIndex] == null)
            Debug.LogWarning("Board : " + randIndex + " does not contain a valid GamePiece Prefab!");
        return gamePiecePrefabs[randIndex];
    }
    public void PlaceGamePiece(GamePiece gamePiece, int x, int y)
    {
        if (gamePiece == null)
        {
            Debug.LogWarning("Board : Invalid GamePiece!");
            return;
        }

        gamePiece.transform.position = new Vector3(x, y, 0);
        if (IsWithinBounds(x, y)) m_allGamePieces[x, y] = gamePiece;
        gamePiece.SetCoord(x, y);
    }
    bool IsWithinBounds(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);
    }
    private void FillBoard(int falseYoffset = 0, float moveTime = 0.1f)
    {
        int maxInteractions = 100;
        int iterations = 0;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (m_allGamePieces[i, j] == null && m_allTiles[i, j].tileType != TileType.Obstacle)
                {
                    GamePiece piece = FillRandomAt(i, j, falseYoffset, moveTime);

                    while (HasMatchOnFill(i, j))
                    {
                        ClearPieceAt(i, j);
                        piece = FillRandomAt(i, j, falseYoffset, moveTime);
                        iterations++;

                        if (iterations >= maxInteractions) break;
                    }
                }
            }
        }
    }
    private GamePiece FillRandomAt(int i, int j, int falseYoffset = 0, float moveTime = 0.1f)
    {
        if (!IsWithinBounds(i, j)) return null;

        GameObject randomPiece = Instantiate(GetRandomGamePiece(), Vector3.zero, Quaternion.identity, transform);

        if (randomPiece != null)
        {
            MakeGamePiece(randomPiece, i, j, falseYoffset, moveTime);
            return randomPiece.GetComponent<GamePiece>();
        }
        return null;
    }
    private void RePlaceWithRandom(List<GamePiece> gamePieces, int x, int y)
    {
        foreach (GamePiece piece in gamePieces)
        {
            ClearPieceAt(piece.xIndex, piece.yIndex);
            if (falseYoffset == 0)
                FillRandomAt(piece.xIndex, piece.yIndex);
            else
                FillRandomAt(piece.xIndex, piece.yIndex, falseYoffset, moveTime);
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
            leftMatches = new List<GamePiece>();
        if (downMatches == null)
            downMatches = new List<GamePiece>();

        return (leftMatches.Count > 0 || downMatches.Count > 0);
    }

    private void SetupGamePieces()
    {
        foreach (StartingObjects sPiece in startingGamePieces)
        {
            if (sPiece != null)
            {
                GameObject piece = Instantiate(sPiece.Prefab, new Vector3(sPiece.x, sPiece.y, 0), Quaternion.identity);
                MakeGamePiece(piece, sPiece.x, sPiece.y, falseYoffset, moveTime);
            }
        }
    }
    private void SetupCamera()
    {
        Camera.main.transform.position = new Vector3((float)(width - 1) / 2, (float)(height - 1) / 2, -10f);
        float aspecRatio = (float)Screen.width / (float)Screen.height;
        float verticalSize = (float)height / 2f + (float)boardSize;
        float horizontalSize = ((float)width / 2f + (float)boardSize) / aspecRatio;

        Camera.main.orthographicSize = (verticalSize > horizontalSize) ? verticalSize : horizontalSize;
    }
    private void SetupTiles()
    {
        foreach (var sTiles in startingTiles)
        {
            if (sTiles != null)
            {
                MakeTile(sTiles.Prefab, sTiles.x, sTiles.y, sTiles.z);
            }
        }

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (m_allTiles[i, j] == null)
                    MakeTile(tileNormalPrefab, i, j);
            }
        }
    }
    private void MakeTile(GameObject prefab, int i, int j, int k = 0)
    {
        if (prefab == null) return;

        GameObject tile = Instantiate(prefab, new Vector3(i, j, k), Quaternion.identity, transform);
        tile.name = "Tile (" + i + ", " + j + ")";
        m_allTiles[i, j] = tile.GetComponent<Tile>();
        m_allTiles[i, j].Init(i, j, this);
    }
    public void ClickTile(Tile tile)
    {
        if (m_clickedTile == null)
        {
            m_clickedTile = tile;
            Debug.Log("Clicked tile : " + tile.name);
        }
    }
    public void DragToTile(Tile tile)
    {
        if (m_clickedTile != null && IsNextTo(tile, m_clickedTile)) // 이웃된 GamePiece만 이동
        {
            m_targetTile = tile;
            Debug.Log("targeted tile : " + tile.name);
        }
    }
    public void ReleaseTile()
    {
        if (m_targetTile != null && m_clickedTile != null)
        {
            SwitchTiles(m_clickedTile, m_targetTile);
        }
        m_targetTile = null;
        m_clickedTile = null;
    }
    private void SwitchTiles(Tile clickedTile, Tile targetTile)
    {
        StartCoroutine(SwitchTileRoutine(clickedTile, targetTile));
    }
    IEnumerator SwitchTileRoutine(Tile clickedTile, Tile targetTile)
    {
        Debug.Log(m_playerInputEnabled);

        if (m_playerInputEnabled)
        {
            //타일 교환 
            GamePiece clickedPiece = m_allGamePieces[clickedTile.xIndex, clickedTile.yIndex];
            GamePiece targetPiece = m_allGamePieces[targetTile.xIndex, targetTile.yIndex];

            Debug.Log(clickedPiece);
            Debug.Log(targetPiece);

            if (targetPiece != null && clickedPiece != null)
            {
                clickedPiece.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
                targetPiece.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);

                yield return new WaitForSeconds(swapTime);

                List<GamePiece> clickedPieceMatches = FindMatchesAt(clickedTile.xIndex, clickedTile.yIndex);
                List<GamePiece> targetPieceMatches = FindMatchesAt(targetTile.xIndex, targetTile.yIndex);
                List<GamePiece> colorMatches = new List<GamePiece>();

                if (IsColorBomb(clickedPiece) && !IsColorBomb(targetPiece))
                {
                    clickedPiece.matchValue = targetPiece.matchValue;
                    colorMatches = FindAllMatchValue(clickedPiece.matchValue);
                }
                else if (!IsColorBomb(clickedPiece) && IsColorBomb(targetPiece))
                {
                    targetPiece.matchValue = clickedPiece.matchValue;
                    colorMatches = FindAllMatchValue(targetPiece.matchValue);
                }
                else if (IsColorBomb(clickedPiece) && IsColorBomb(targetPiece))
                {
                    foreach (GamePiece piece in m_allGamePieces)
                    {
                        if (!colorMatches.Contains(piece))
                        {
                            colorMatches.Add(piece);
                        }
                    }
                }

                if (clickedPieceMatches.Count == 0 && targetPieceMatches.Count == 0 && colorMatches.Count == 0)
                {
                    clickedPiece.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);
                    targetPiece.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
                }
                else
                {
                    yield return new WaitForSeconds(swapTime);
                    Vector2 swipeDirection = new Vector2(targetTile.xIndex - clickedTile.xIndex,
                        targetTile.yIndex - clickedTile.yIndex);
                    m_clickedTileBomb = DropBomb(clickedTile.xIndex, clickedTile.yIndex,
                        swipeDirection, clickedPieceMatches);
                    m_targetTileBomb = DropBomb(targetTile.xIndex, targetTile.yIndex,
                        swipeDirection, targetPieceMatches);

                    if (m_clickedTileBomb != null && targetPiece != null)
                    {
                        GamePiece clickedBombPiece = m_clickedTileBomb.GetComponent<GamePiece>();

                        if (!IsColorBomb(clickedBombPiece))
                            clickedBombPiece.ChangeColor(targetPiece);
                    }

                    if (m_targetTileBomb != null && clickedPiece != null)
                    {
                        GamePiece targetBombPiece = m_targetTileBomb.GetComponent<GamePiece>();

                        if (!IsColorBomb(targetBombPiece))
                            targetBombPiece.ChangeColor(clickedPiece);
                    }

                    ClearAndRefillBoard(clickedPieceMatches.Union(targetPieceMatches).ToList().Union(colorMatches).ToList());
                }
            }
        }
    }
    bool IsNextTo(Tile start, Tile end) // 이웃된 GamePiece만 이동
    {
        if (Mathf.Abs(start.xIndex - end.xIndex) == 1 && start.yIndex == end.yIndex)
            return true;
        if (start.xIndex == end.xIndex && Mathf.Abs(start.yIndex - end.yIndex) == 1)
            return true;

        return false;
    }


    List<GamePiece> FindMatches(int startX, int startY, Vector2 searchDirection, int minLength = 3)
    {
        List<GamePiece> matches = new List<GamePiece>();
        GamePiece startPiece = null;

        if (IsWithinBounds(startX, startY))
        {
            startPiece = m_allGamePieces[startX, startY];
        }

        if (startPiece != null)
            matches.Add(startPiece);
        else
            return null;

        int nextX, nextY;
        int maxValue = (width > height) ? width : height;

        for (int i = 1; i < maxValue - 1; i++)
        {
            nextX = startX + (int)Mathf.Clamp(searchDirection.x, -1, 1) * i;
            nextY = startY + (int)Mathf.Clamp(searchDirection.y, -1, 1) * i;

            if (!IsWithinBounds(nextX, nextY))
                break;

            GamePiece nextPiece = m_allGamePieces[nextX, nextY];

            if (nextPiece == null) break;
            else
            {
                if (nextPiece.matchValue == startPiece.matchValue)
                    matches.Add(nextPiece);
                else
                    break;
            }
        }

        if (matches.Count >= minLength)
            return matches;

        return null;
    }
    List<GamePiece> FindVerticalMatches(int startX, int startY, int minLength = 3)
    {
        List<GamePiece> upwardMatches = FindMatches(startX, startY, new Vector2(0, 1), 2);
        List<GamePiece> downwardMatches = FindMatches(startX, startY, new Vector2(0, -1), 2);

        if (upwardMatches == null)
            upwardMatches = new List<GamePiece>();
        if (downwardMatches == null)
            downwardMatches = new List<GamePiece>();

        var combinedMatches = upwardMatches.Union(downwardMatches).ToList(); // Union 합집합

        return (combinedMatches.Count >= minLength) ? combinedMatches : null;
    }
    List<GamePiece> FindHorizontalMatches(int startX, int startY, int minLength = 3)
    {
        List<GamePiece> rightMatches = FindMatches(startX, startY, new Vector2(1, 0), 2);
        List<GamePiece> leftMatches = FindMatches(startX, startY, new Vector2(-1, 0), 2);

        if (rightMatches == null)
            rightMatches = new List<GamePiece>();
        if (leftMatches == null)
            leftMatches = new List<GamePiece>();

        var combinedMatches = rightMatches.Union(leftMatches).ToList();

        return (combinedMatches.Count >= minLength) ? combinedMatches : null;
    }
    List<GamePiece> FindMatchesAt(int x, int y, int minLength = 3)
    {
        List<GamePiece> horizMatches = FindHorizontalMatches(x, y, minLength);
        List<GamePiece> vertMatches = FindVerticalMatches(x, y, minLength);

        if (horizMatches == null)
            horizMatches = new List<GamePiece>();

        if (vertMatches == null)
            vertMatches = new List<GamePiece>();

        var combineMatches = horizMatches.Union(vertMatches).ToList();

        return combineMatches;
    }
    List<GamePiece> FindMatchesAt(List<GamePiece> gamePieces, int minLength = 3)
    {
        List<GamePiece> matches = new List<GamePiece>();

        foreach (var piece in gamePieces)
            matches = matches.Union(FindMatchesAt(piece.xIndex, piece.yIndex, minLength)).ToList();

        return matches;
    }
    List<GamePiece> FindAllMatches()
    {
        List<GamePiece> combinMatches = new List<GamePiece>();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                List<GamePiece> matches = FindMatchesAt(i, j);
                combinMatches = combinMatches.Union(matches).ToList();
            }
        }
        return combinMatches;
    }
    List<GamePiece> FindAllMatchValue(MatchValue match)
    {
        List<GamePiece> colorMatches = new List<GamePiece>();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j <height; j++)
            {
                if (m_allGamePieces != null)
                {
                    if (m_allGamePieces[i,j].matchValue == match)
                    {
                        colorMatches.Add(m_allGamePieces[i, j]);
                    }
                }
            }
        }

        return colorMatches;
    }

    void HighlightTileOff(int x, int y)
    {
        if (m_allTiles[x, y].tileType != TileType.breakable)
        {
            SpriteRenderer spriteRenderer = m_allTiles[x, y].GetComponent<SpriteRenderer>();
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0);
        }
    }
    void HighlightTileOn(int x, int y, Color col)
    {
        if (m_allTiles[x, y].tileType != TileType.breakable)
        {
            SpriteRenderer spriteRenderer = m_allTiles[x, y].GetComponent<SpriteRenderer>();
            spriteRenderer.color = col;
        }
    }
    void HighlightMatchesAt(int x, int y)
    {
        HighlightTileOff(x, y);
        var combineMatches = FindMatchesAt(x, y);

        if (combineMatches.Count > 0)
        {
            foreach (GamePiece piece in combineMatches)
            {
                //HighlightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
            }
        }
    }
    void HighlightMatchesAt(List<GamePiece> gamePieces)
    {
        foreach (GamePiece piece in gamePieces)
        {
            if (piece != null)
            {
                //HighlightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
            }
        }
    }
    void HightLightMatches() // 3매치 된 얘들 하이라이트 주기 (나중에 없애기)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                //HighlightMatchesAt(i, j);
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

            //HighlightTileOff(x, y);
        }
    }
    void ClearPieceAt(List<GamePiece> gamePieces, List<GamePiece> bombedPieces)
    {
        foreach (var piece in gamePieces)
        {
            if (piece != null)
            {
                ClearPieceAt(piece.xIndex, piece.yIndex);

                if (m_particleManager != null)
                {
                    if (bombedPieces.Contains(piece))
                        m_particleManager.BombFXAt(piece.xIndex, piece.yIndex);
                    else
                        m_particleManager.ClearPieceFXAt(piece.xIndex, piece.yIndex);
                }
            }
        }
    }
    void BreakTileAt(int x, int y)
    {
        Tile tileToBreak = m_allTiles[x, y];

        if (tileToBreak != null && tileToBreak.tileType == TileType.breakable)
        {
            if (m_particleManager != null)
            {
                m_particleManager.BreakTileFXAt(tileToBreak.breakableValue, x, y);
            }
            tileToBreak.BreakTile();
        }
    }
    void BreakTileAt(List<GamePiece> gamePieces)
    {
        foreach (var pieces in gamePieces)
        {
            if (pieces != null)
            {
                BreakTileAt(pieces.xIndex, pieces.yIndex);
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
            if (!columns.Contains(piece.xIndex)) columns.Add(piece.xIndex);
        //Debug.Log(columns.Count);
        return columns;
    }
    List<GamePiece> CollapseColumn(List<GamePiece> gamePieces)
    {
        List<GamePiece> movingPiece = new List<GamePiece>();
        List<int> columnsToCollapse = GetColumns(gamePieces);

        foreach (int column in columnsToCollapse)
        {
            movingPiece = movingPiece.Union(CollapseColumn(column)).ToList(); // 인덱스 에러
        }
        return movingPiece;
    }
    void ClearAndRefillBoard(List<GamePiece> gamePieces)
    {
        StartCoroutine(ClearAndRefillBoardRoutine(gamePieces));
    }
    IEnumerator ClearAndRefillBoardRoutine(List<GamePiece> gamePieces) // do while 문 확인하기
    {
        m_playerInputEnabled = false;
        List<GamePiece> matches = gamePieces;

        do
        {
            //clear and collapse
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
            Debug.Log("1");
            movingPieces = CollapseColumn(gamePieces);

            yield return new WaitForSeconds(0.25f);

            Debug.Log("2");
            while (!IsCollapsed(movingPieces))
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.25f);

            matches = FindMatchesAt(movingPieces);
            Debug.Log("3");
            if (matches.Count == 0)
            {
                isFinished = true;
                break;
            }
            else
                yield return StartCoroutine(ClearAndCollapseRoutine(matches));
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
    List<GamePiece> GetRowPieces(int row)
    {
        List<GamePiece> gamePieces = new List<GamePiece>();

        for (int i = 0; i < width; i++)
        {
            if (m_allGamePieces[i, row] != null)
            {
                gamePieces.Add(m_allGamePieces[i, row]);
            }
        }
        return gamePieces;
    }
    List<GamePiece> GetColumnPieces(int column)
    {
        List<GamePiece> gamePieces = new List<GamePiece>();

        for (int i = 0; i < height; i++)
        {
            if (m_allGamePieces[column, i] != null)
            {
                gamePieces.Add(m_allGamePieces[column, i]);
            }
        }
        return gamePieces;
    }
    List<GamePiece> GetAdjacentPieces(int x, int y, int offset = 1)
    {
        List<GamePiece> gamePieces = new List<GamePiece>();

        for (int i = x - offset; i <= x + offset; i++)
        {
            for (int j = y - offset; j <= y + offset; j++)
            {
                if (IsWithinBounds(i, j))
                {
                    gamePieces.Add(m_allGamePieces[i, j]);
                }
            }
        }
        return gamePieces;
    }
    List<GamePiece> GetBombedPieces(List<GamePiece> gamePieces)
    {
        List<GamePiece> allPiecesToClear = new List<GamePiece>();
        foreach (var piece in gamePieces)
        {
            if (piece != null)
            {
                List<GamePiece> pieceToClear = new List<GamePiece>();

                Bomb bomb = piece.GetComponent<Bomb>();
                if (bomb != null)
                {
                    switch (bomb.bombType)
                    {
                        case BombType.Column:
                            pieceToClear = GetColumnPieces(bomb.xIndex);
                            break;
                        case BombType.Row:
                            pieceToClear = GetRowPieces(bomb.yIndex);
                            break;
                        case BombType.Adjacent:
                            pieceToClear = GetAdjacentPieces(bomb.xIndex, bomb.yIndex, 1);
                            break;
                        case BombType.Color:
                            break;
                    }
                    allPiecesToClear = allPiecesToClear.Union(pieceToClear).ToList();
                }
            }
        }
        return allPiecesToClear;
    }
    bool IsCornerMatch(List<GamePiece> gamePieces)
    {
        bool vertical = false;
        bool horizontal = false;

        int startX = -1;
        int startY = -1;

        foreach (GamePiece piece in gamePieces)
        {
            if (piece != null)
            {
                if (startX == -1 || startY == -1)
                {
                    startX = piece.xIndex;
                    startY = piece.yIndex;
                    continue;
                }

                if (piece.xIndex != startX && piece.yIndex == startY)
                    horizontal = true;

                if (piece.xIndex == startX && piece.yIndex != startY)
                    vertical = true;
            }
        }

        return (horizontal && vertical);
    }
    GameObject DropBomb(int x, int y, Vector2 swapDirection, List<GamePiece> gamePieces)
    {
        GameObject bomb = null;
        // bomb 조건 : 4개 이상

        if (gamePieces.Count >= 4)
        {
            if (IsCornerMatch(gamePieces))
            {
                if (adjacentBombPrefab != null)
                    bomb = MakeBomb(adjacentBombPrefab, x, y);
            }
            else
            {
                if (gamePieces.Count >= 5)
                {
                    if (colorBombPrefab != null)
                        bomb = MakeBomb(colorBombPrefab, x, y);
                }
                else
                {
                    if (swapDirection.x != 0)
                    {
                        if (columnBombPrefab != null)
                            bomb = MakeBomb(columnBombPrefab, x, y);
                    }
                    else
                    {
                        if (rowBombPrefab != null)
                            bomb = MakeBomb(rowBombPrefab, x, y);
                    }
                }
            }
        }
        return bomb;
    }

    void ActivateBomb(GameObject bomb)
    {
        int x = (int)bomb.transform.position.x;
        int y = (int)bomb.transform.position.y;

        if (IsWithinBounds(x, y))
        {
            m_allGamePieces[x, y] = bomb.GetComponent<GamePiece>();
        }
    }
    bool IsColorBomb(GamePiece gamePiece)
    {
        Bomb bomb = gamePiece.GetComponent<Bomb>();

        if (bomb != null)
        {
            return (bomb.bombType == BombType.Color);
        }

        return false;
    }
}




