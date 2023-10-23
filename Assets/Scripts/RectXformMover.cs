using System.Collections;
using UnityEngine;

public class RectXformMover : MonoBehaviour
{
    [SerializeField] private Vector3 startPos;
    [SerializeField] private Vector3 onScreenPos;
    [SerializeField] private Vector3 endPos;
    [SerializeField] private float timeToMove;

    private bool isMoving = false;

    private RectTransform m_rectXform;

    private void Awake()
    {
        m_rectXform = GetComponent<RectTransform>();
    }

    private void Move(Vector3 startPos, Vector3 endPos, float timeToMove)
    {
        if (!isMoving)
        {
            StartCoroutine(MoveRoutine(startPos, endPos, timeToMove));
        }
    }

    IEnumerator MoveRoutine(Vector3 startPos, Vector3 endPos, float timeToMove)
    {
        if (m_rectXform != null)
        {
            m_rectXform.anchoredPosition = startPos;
        }

        bool reachedDestination = false;
        float elapsedTime = 0f;
        isMoving = true;

        while (!reachedDestination)
        {
            if (Vector3.Distance(m_rectXform.anchoredPosition, endPos) < 0.01f)
            {
                reachedDestination = true;
                break;
            }
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp(elapsedTime / timeToMove, 0f, 1f);
            t = t * t * t * (t * (t * 6 - 15) + 10);

            if (m_rectXform != null)
            {
                m_rectXform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);

            }
            yield return null;
        }
        isMoving = false;
    }
    
    public void MoveOn()
    {
        Move(startPos, onScreenPos, timeToMove);
    }

    public void MoveOff()
    {
        Move(onScreenPos, endPos, timeToMove);
    }
}
