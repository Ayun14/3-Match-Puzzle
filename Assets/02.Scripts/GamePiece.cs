using System.Collections;
using UnityEngine;

public enum InterpType
{
    Linear,
    EaseOut,
    EaseIn,
    SmoothStep,
    SmootherStep
}

public enum MatchValue
{
    Blue,
    Magenta,
    Indigo,
    Green,
    Teal,
    Red,
    Cyan,
    Yellow,
    Wild,
    None
}

public class GamePiece : MonoBehaviour
{
    public int xIndex;
    public int yIndex;

    Board m_board;

    public InterpType interpolation = InterpType.SmootherStep;
    public MatchValue matchValue;
    bool isMoving = false;

    public void Init(Board board)
    {
        m_board = board;
    }

    public void SetCoord(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Move((int)transform.position.x + 1, (int)transform.position.y, 0.5f);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Move((int)transform.position.x - 1, (int)transform.position.y, 0.5f);
        }
    }

    public void Move(int destX, int desty, float timeToMove)
    {
        if (!isMoving)
        {
            StartCoroutine(MoveRoutine(new Vector3(destX, desty, 0), timeToMove));
        }
    }

    IEnumerator MoveRoutine(Vector3 destination, float timeToMove)
    {
        Vector3 startPosition = transform.position;
        bool reachedDestination = false;
        float elapsedTime = 0f;

        isMoving = true;

        while (!reachedDestination)
        {
            //러프 써서 이동 구현
            if (Vector2.Distance(transform.position, destination) < 0.001f)
            {
                reachedDestination = true;
                if (m_board != null)
                {
                    m_board.PlaceGamePiece(this, (int)destination.x, (int)destination.y);
                }
                break;
            }
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / timeToMove;

            switch (interpolation)
            {
                case InterpType.Linear:
                    break;
                case InterpType.EaseIn:
                    t = 1 - Mathf.Cos(t * Mathf.PI * 0.5f);
                    break;
                case InterpType.EaseOut:
                    t = Mathf.Sin(t * Mathf.PI * 0.5f);
                    break;
                case InterpType.SmoothStep:
                    t = t * t * (3 - 2 * t);
                    break;
                case InterpType.SmootherStep:
                    t = t * t * t * (t * (t * 6 - 15) + 10);
                    break;

            }
            transform.position = Vector3.Lerp(startPosition, destination, t);

            yield return null;
        }
        isMoving = false;
    }

    public void ChangeColor(GamePiece pieceToMatch)
    {
        SpriteRenderer renderToChange = GetComponent<SpriteRenderer>();

        if (pieceToMatch != null)
        {
            SpriteRenderer rendererToMatch = pieceToMatch.GetComponent<SpriteRenderer>();

            if (rendererToMatch != null && renderToChange != null)
            {
                renderToChange.color = rendererToMatch.color;
            }
            matchValue = pieceToMatch.matchValue;
        }
    }
}
