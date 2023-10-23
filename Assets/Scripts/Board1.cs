using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class Board : MonoBehaviour
{
    [Header("Bomb")]
    [SerializeField] private GameObject adjacentBombPrefab;
    [SerializeField] private GameObject columnBombPrefab;
    [SerializeField] private GameObject rowBombPrefab;
    [SerializeField] private GameObject colorBombPrefab;
    [Header("Collectable")]
    [SerializeField] int maxCollectable = 3;
    [SerializeField] int collectableCount = 0;
    [Range(0, 1)]
    [SerializeField] float chanceForCollectable = 0.1f;
    [SerializeField] GameObject[] collectablePrefabs;

    #region "BOMB"
    List<GamePiece> GetRowPieces(int row)
    {
        List<GamePiece> gamePieces = new List<GamePiece>();

        for (int i = 0; i < width; i++)
        {
            if (m_allGamePieces[i, row] != null)
                gamePieces.Add(m_allGamePieces[i, row]);
        }
        return gamePieces;
    }
    List<GamePiece> GetColumnPieces(int column)
    {
        List<GamePiece> gamePieces = new List<GamePiece>();
        for (int i = 0; i < height; i++)
        {
            if (m_allGamePieces[column, i] != null)
                gamePieces.Add(m_allGamePieces[column, i]);
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
                    allPiecesToClear = RemoveCollectables(allPiecesToClear);
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
                {
                    horizontal = true;
                }
                if (piece.xIndex == startX && piece.yIndex != startY)
                {
                    vertical = true;
                }
            }
        }
        return (horizontal && vertical);
    }
    GameObject DropBomb(int x, int y, Vector2 swapDirection, List<GamePiece> gamePieces)
    {
        GameObject bomb = null;
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
                    {
                        bomb = MakeBomb(colorBombPrefab, x, y);
                    }
                }
                else
                {
                    if (swapDirection.x != 0)
                    {
                        if (rowBombPrefab != null)
                            bomb = MakeBomb(rowBombPrefab, x, y);
                    }
                    else
                    {
                        if (columnBombPrefab != null)
                            bomb = MakeBomb(columnBombPrefab, x, y);
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
    #endregion

    #region "Collectable"
    List<GamePiece> FindCollectablesAt(int row, bool clearByBottom = false)
    {
        List<GamePiece> foundCollectables = new List<GamePiece>();

        for (int i = 0; i < width; i++)
        {
            if (m_allGamePieces[i, row] != null)
            {
                Collectable collectableComponent = m_allGamePieces[i, row].GetComponent<Collectable>();

                if (collectableComponent != null)
                {
                    if ((clearByBottom && collectableComponent.clearByBottom) || !clearByBottom)
                    {
                        foundCollectables.Add(m_allGamePieces[i, row]);
                    }
                }
            }
        }

        return foundCollectables;
    }

    List<GamePiece> FindAllCollectables()
    {
        List<GamePiece> foundCollectables = new List<GamePiece>();

        for (int i = 0; i < height; i++)
        {
            List<GamePiece> collectableRow = FindCollectablesAt(i);
            foundCollectables = foundCollectables.Union(collectableRow).ToList();
        }

        return foundCollectables;
    }
    List<GamePiece> RemoveCollectables(List<GamePiece> bombedPieces)
    {
        List<GamePiece> collectablePieces = FindAllCollectables();
        List<GamePiece> piecesToRemove = new List<GamePiece>();

        foreach (GamePiece piece in collectablePieces)
        {
            Collectable collectableComponent = piece.GetComponent<Collectable>();

            if (collectableComponent != null && !collectableComponent.clearByBomb)
            {
                piecesToRemove.Add(piece);
            }
        }

        return bombedPieces.Except(piecesToRemove).ToList();
    }
    bool CanAddCollectable()
    {
        return (collectablePrefabs.Length > 0 && collectableCount < maxCollectable &&
            Random.Range(0f, 1f) <= chanceForCollectable);
    }
    #endregion

}
