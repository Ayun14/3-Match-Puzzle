using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class Board : MonoBehaviour
{
    #region "FindMatches"
    List<GamePiece> FindMatches(int startX, int startY, Vector2 searchDirection, int minLength = 3)
    {
        List<GamePiece> matches = new List<GamePiece>();
        GamePiece startPiece = null;

        if (IsWithinBounds(startX, startY))
        {
            startPiece = m_allGamePieces[startX, startY];
        }

        if (startPiece != null)
        {
            matches.Add(startPiece);
        }
        else
        {
            return null;
        }

        int nextX, nextY;
        int maxValue = (width > height) ? width : height;

        for (int i = 1; i < maxValue - 1; i++)
        {
            nextX = startX + (int)Mathf.Clamp(searchDirection.x, -1, 1) * i;
            nextY = startY + (int)Mathf.Clamp(searchDirection.y, -1, 1) * i;

            if (!IsWithinBounds(nextX, nextY))
            {
                break;
            }

            GamePiece nextPiece = m_allGamePieces[nextX, nextY];

            if (nextPiece == null) break;
            else
            {
                if (nextPiece.matchValue == startPiece.matchValue && nextPiece.matchValue != MatchValue.None && !matches.Contains(nextPiece))
                {
                    matches.Add(nextPiece);
                }
                else
                {
                    break;
                }
            }
        }

        if (matches.Count >= minLength)
        {
            return matches;
        }

        return null;
    }
    List<GamePiece> FindVerticalMatches(int startX, int startY, int minLength = 3)
    {
        List<GamePiece> upwardMatches = FindMatches(startX, startY, new Vector2(0, 1), 2);
        List<GamePiece> downwardMatches = FindMatches(startX, startY, new Vector2(0, -1), 2);

        if (upwardMatches == null)
        {
            upwardMatches = new List<GamePiece>();
        }

        if (downwardMatches == null)
        {
            downwardMatches = new List<GamePiece>();
        }

        var combinedMatches = upwardMatches.Union(downwardMatches).ToList();

        return (combinedMatches.Count >= minLength) ? combinedMatches : null;

    }
    List<GamePiece> FindHorizontalMatches(int startX, int startY, int minLength = 3)
    {
        List<GamePiece> rightMatches = FindMatches(startX, startY, new Vector2(1, 0), 2);
        List<GamePiece> leftMatches = FindMatches(startX, startY, new Vector2(-1, 0), 2);

        if (rightMatches == null)
        {
            rightMatches = new List<GamePiece>();
        }

        if (leftMatches == null)
        {
            leftMatches = new List<GamePiece>();
        }

        var combinedMatches = rightMatches.Union(leftMatches).ToList();

        return (combinedMatches.Count >= minLength) ? combinedMatches : null;
    }
    List<GamePiece> FindMatchesAt(int x, int y, int minLength = 3)
    {
        List<GamePiece> horizMatches = FindHorizontalMatches(x, y, minLength);
        List<GamePiece> vertMatches = FindVerticalMatches(x, y, minLength);

        if (horizMatches == null)
        {
            horizMatches = new List<GamePiece>();
        }

        if (vertMatches == null)
        {
            vertMatches = new List<GamePiece>();
        }

        var combineMatches = horizMatches.Union(vertMatches).ToList();

        return combineMatches;
    }
    List<GamePiece> FindMatchesAt(List<GamePiece> gamePieces, int minLegth = 3)
    {
        List<GamePiece> matches = new List<GamePiece>();

        foreach (var piece in gamePieces)
        {
            matches = matches.Union(FindMatchesAt(piece.xIndex, piece.yIndex, minLegth)).ToList();
        }

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
    List<GamePiece> FindAllMatchValue(MatchValue matchValue)
    {
        List<GamePiece> colorMatches = new List<GamePiece>();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (m_allGamePieces[i, j] != null)
                {
                    if (m_allGamePieces[i, j].matchValue == matchValue)
                    {
                        colorMatches.Add(m_allGamePieces[i, j]);
                    }
                }
            }
        }
        return colorMatches;
    }
    #endregion

    #region "Highlight"
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
                HighlightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
            }
        }
    }
    void HighlightMatchesAt(List<GamePiece> gamePieces)
    {
        foreach (GamePiece piece in gamePieces)
        {
            if (piece != null)
            {
                HighlightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
            }
        }
    }
    void HighlightMatches()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                HighlightMatchesAt(i, j);
            }
        }
    }
    #endregion
}
