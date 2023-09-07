using System;
using UnityEngine;
using System.Collections;

public enum TileType
{
    Normal,
    Obstacle,
    breakable
}

[RequireComponent(typeof(SpriteRenderer))]
public class Tile : MonoBehaviour
{
    public int xIndex;
    public int yIndex;

    Board m_board;

    public TileType tileType = TileType.Normal;

    SpriteRenderer m_spriteRenderer;

    public int breakableValue = 0;
    public Sprite[] breakableSprites;
    private Color normalColor;

    private void Awake()
    {
        m_spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Init(int x, int y, Board board)
    {
        xIndex = x;
        yIndex = y;
        m_board = board;
    }

    private void OnMouseDown()
    {
        if (m_board != null)
        {
            m_board.ClickTile(this);
        }
    }

    private void OnMouseEnter()
    {
        if (m_board != null)
        {
            m_board.DragToTile(this);
        }
    }

    private void OnMouseUp()
    {
        if (m_board != null)
        {
            m_board.ReleaseTile();
        }
    }

    public void BreakTile()
    {
        if (tileType != TileType.breakable) return;

        StartCoroutine(BreakTileRoutine());
    }

    IEnumerator BreakTileRoutine()
    {
        breakableValue = Mathf.Clamp(--breakableValue, 0, breakableValue);
        yield return new WaitForSeconds(0.25f);

        if (breakableSprites[breakableValue] != null)
            m_spriteRenderer.sprite = breakableSprites[breakableValue];

        if (breakableValue == 0)
        {
            tileType = TileType.Normal;
            m_spriteRenderer.color = normalColor;
        }

        yield return null;
    }
}
