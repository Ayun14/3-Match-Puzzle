using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public partial class Board : MonoBehaviour
{
    [SerializeField] private GameObject tileNormalPrefab;
    [SerializeField] private GameObject tileObstaclePrefab;
    [Space]
    [SerializeField] StartingObjects[] startingTiles;

    Tile[,] m_allTiles;
    
    Tile m_clickedTile;
    Tile m_targetTile;

    void SetupTiles()
    {
        foreach (var sTiles in startingTiles)
        {
            if (sTiles != null)
            {
                MakeTile(sTiles.prefab, sTiles.x, sTiles.y, sTiles.z);
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
        }
    }
    public void DragToTile(Tile tile)
    {
        if (m_clickedTile != null && IsNextTo(tile, m_clickedTile))
        {
            m_targetTile = tile;
        }
    }
    public void ReleaseTile()
    {
        if (m_clickedTile != null && m_targetTile != null)
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
        if (m_playerInputEnabled)
        {
            GamePiece clickedPiece = m_allGamePieces[clickedTile.xIndex, clickedTile.yIndex];
            GamePiece targetPiece = m_allGamePieces[targetTile.xIndex, targetTile.yIndex];

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

                    GameManager.Instance.moveLeft--;

                    Vector2 swipeDirection = new Vector2(targetTile.xIndex - clickedTile.xIndex,
                        targetTile.yIndex - clickedTile.yIndex);
                    m_clickedTileBomb = DropBomb(clickedTile.xIndex, clickedTile.yIndex,
                        swipeDirection, clickedPieceMatches);
                    m_targetTileBomb = DropBomb(targetTile.xIndex, targetTile.yIndex, swipeDirection, targetPieceMatches);

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
    bool IsNextTo(Tile start, Tile end)
    {
        if (Mathf.Abs(start.xIndex - end.xIndex) == 1 && start.yIndex == end.yIndex)
        {
            return true;
        }

        if (start.xIndex == end.xIndex && Mathf.Abs(start.yIndex - end.yIndex) == 1)
        {
            return true;
        }

        return false;
    }
    void BreakTileAt(int x, int y)
    {
        Tile tileToBreak = m_allTiles[x, y];

        if (tileToBreak != null && tileToBreak.tileType == TileType.breakable)
        {
            if (m_particleManager != null)
                m_particleManager.BreakTileFXAt(tileToBreak.breakableValue, x, y);
            tileToBreak.BreakTile();
        }
    }
    void BreakTileAt(List<GamePiece> gamePieces)
    {
        foreach (var piece in gamePieces)
        {
            if (piece != null)
            {
                BreakTileAt(piece.xIndex, piece.yIndex);
            }
        }
    }
}
