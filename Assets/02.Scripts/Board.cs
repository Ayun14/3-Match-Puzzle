using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int boardSize;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject[] gamePiecePrefabs;
    [SerializeField] private float swapTime = 0.5f;

    Tile[,] m_allTiles;
    GamePiece[,] m_allGamePieces;

    Tile m_clickedTile;
    Tile m_targetTile;

    private void Start()
    {
        m_allTiles = new Tile[width, height];
        m_allGamePieces = new GamePiece[width, height];
        SetupTiles();
        SetupCamera();
        FillRandom();
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

    private void FillRandom()
    {
        int maxInteractions = 100;
        int iterations = 0;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                GamePiece piece = FillRandomAt(i, j);

                while (HasMatchOnFill(i, j))
                {
                    ClearPieceAt(i, j);
                    piece = FillRandomAt(i, j);
                    iterations++;

                    if (iterations >= maxInteractions) break;
                }
            }
        }
    }

    private GamePiece FillRandomAt(int i, int j)
    {
        GameObject randomPiece = Instantiate(GetRandomGamePiece(), Vector3.zero, Quaternion.identity, transform);

        if (randomPiece != null)
        {
            randomPiece.GetComponent<GamePiece>().Init(this);

            PlaceGamePiece(randomPiece.GetComponent<GamePiece>(), i, j);

            randomPiece.transform.parent = transform;

            return randomPiece.GetComponent<GamePiece>();
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
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                GameObject tile = Instantiate(tilePrefab, new Vector3(i, j, 0), Quaternion.identity, transform);
                tile.name = "Tile (" + i + ", " + j + ")";
                m_allTiles[i, j] = tile.GetComponent<Tile>();
                m_allTiles[i, j].Init(i, j, this);
            }
        }
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
        //타일 교환 
        GamePiece clickedPiece = m_allGamePieces[clickedTile.xIndex, clickedTile.yIndex];
        GamePiece targetPiece = m_allGamePieces[targetTile.xIndex, targetTile.yIndex];

        if (targetPiece != null && clickedPiece != null)
        {
            clickedPiece.Move(targetTile.xIndex, targetTile.yIndex, 0.5f);
            targetPiece.Move(clickedTile.xIndex, clickedTile.yIndex, 0.5f);

            yield return new WaitForSeconds(swapTime);

            List<GamePiece> clickedPieceMatches = FindMatchesAt(clickedTile.xIndex, clickedTile.yIndex);
            List<GamePiece> targetPieceMatches = FindMatchesAt(targetTile.xIndex, targetTile.yIndex);

            if (clickedPieceMatches.Count == 0 && targetPieceMatches.Count == 0)
            {
                clickedPiece.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);
                targetPiece.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
            }
            else
            {
                yield return new WaitForSeconds(swapTime);
                ClearPieceAt(clickedPieceMatches);
                ClearPieceAt(targetPieceMatches);

                CollapseColumn(clickedPieceMatches);
                CollapseColumn(targetPieceMatches);

                //HighlightMatchesAt(clickedTile.xIndex, clickedTile.yIndex);
                //HighlightMatchesAt(targetTile.xIndex, targetTile.yIndex);

                //하이라이트 말고 지우기
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

            if (nextPiece.matchValue == startPiece.matchValue)
                matches.Add(nextPiece);
            else
                break;
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
        List<GamePiece> rightdMatches = FindMatches(startX, startY, new Vector2(1, 0), 2);
        List<GamePiece> leftMatches = FindMatches(startX, startY, new Vector2(-1, 0), 2);

        if (rightdMatches == null)
            rightdMatches = new List<GamePiece>();
        if (leftMatches == null)
            leftMatches = new List<GamePiece>();

        var combinedMatches = rightdMatches.Union(leftMatches).ToList();

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


    void HighlightTileOff(int x, int y)
    {
        SpriteRenderer spriteRenderer = m_allTiles[x, y].GetComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0);
    }

    void HighlightTileOn(int x, int y, Color col)
    {
        SpriteRenderer spriteRenderer = m_allTiles[x, y].GetComponent<SpriteRenderer>();
        spriteRenderer.color = col;
    }

    void HighlightMatchesAt(int x, int y)
    {
        HighlightTileOff(x, y);
        var combineMatches = FindMatchesAt(x, y);

        if (combineMatches.Count > 0)
        {
            foreach (GamePiece piece in combineMatches)
            {
                HighlightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
            }
        }
    }

    void HightLightMatches() // 3매치 된 얘들 하이라이트 주기 (나중에 없애기)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                HighlightMatchesAt(i, j);
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

            HighlightTileOff(x, y);
        }
    }

    void ClearPieceAt(List<GamePiece> gamePieces)
    {
        foreach (var piece in gamePieces)
        {
            ClearPieceAt(piece.xIndex, piece.yIndex);
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
        Debug.Log(column);
        for (int i = 0; i < height - 1; i++)
        {
            if (m_allGamePieces[column, i] == null)
            {
                for (int j = i + 1; i < height; j++)
                {
                    if (m_allGamePieces[column, j] != null)
                    {
                        m_allGamePieces[column, j].Move(column, i, collapseTime);
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
            movingPiece = movingPiece.Union(CollapseColumn(column)).ToList();
        }
        return movingPiece;
    }
}
