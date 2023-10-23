using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : Singleton<GameManager>
{
    public int moveLeft = 30;
    public int scoreGoal = 10000;
    [SerializeField] private Fade screenFader;
    [SerializeField] private TextMeshProUGUI levelNameText;
    [SerializeField] private TextMeshProUGUI moveLeftText;

    [Space]
    [SerializeField] MessageWindow messageWindow;
    [SerializeField] RectXformMover rectXformMover;
    [SerializeField] Sprite loseIcon;
    [SerializeField] Sprite winIcon;
    [SerializeField] Sprite goalIcon;

    Board m_board;

    bool m_isReadyToBegin;
    bool m_isGameOver;
    bool m_isWinner;

    private void Start()
    {
        m_board = FindObjectOfType<Board>();

        Scene scene = SceneManager.GetActiveScene();

        if (levelNameText != null)
        {
            levelNameText.text = scene.name;
        }
        messageWindow.ShowMessage(goalIcon, $"Score Goal:\n {scoreGoal}");
        rectXformMover.MoveOn();
        StartCoroutine(ExeuteGameLoop());
    }
    IEnumerator ExeuteGameLoop()
    {
        yield return StartCoroutine("StartGameRoutine");
        yield return StartCoroutine("PlayGameRoutine");
        yield return StartCoroutine("EndGameRoutine");
    }

    public void BeginGame()
    {
        m_isReadyToBegin = true;
    }

    public void EndGame()
    {
        if (m_isWinner)
        {
            SceneManager.LoadScene(0);
        }
    }

    private IEnumerator StartGameRoutine()
    {
        while (!m_isReadyToBegin)
        {
            yield return null;
        }

        rectXformMover.MoveOff();
        if (screenFader != null) screenFader.FadeOff();

        yield return new WaitForSeconds(0.6f);

        if (m_board != null) m_board.SetupBoard();
    }

    private IEnumerator PlayGameRoutine()
    {
        while (!m_isGameOver)
        {
            if (moveLeft == 0 && m_board.m_playerInputEnabled)
            {
                m_isWinner = true;
                m_isGameOver = true;
            }
            moveLeftText.text = "" + moveLeft;
            yield return null;
        }
    }

    private IEnumerator EndGameRoutine()
    {
        if (screenFader != null)
        {
            screenFader.FadeOn();
        }

        if (m_isWinner && ScoreManager.Instance.m_currentScore >= scoreGoal)
        {
            messageWindow.ShowMessage(winIcon, $"You Win!!", "OK");
            rectXformMover.MoveOn();

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayWinSound();
            }
        }
        else
        {
            messageWindow.ShowMessage(loseIcon, $"You Lose...", "OK");
            rectXformMover.MoveOn();

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayLoseSound();
            }
        }
        yield return null;
    }
}
