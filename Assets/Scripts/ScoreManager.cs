using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreManager : Singleton<ScoreManager>
{
    [SerializeField] TextMeshProUGUI scoreText;

    public int m_currentScore = 0;
    public int m_countValue = 0;
    int m_increment = 5;

    public void UpdateScoreText(int scoreValue)
    {
        if (scoreText != null)
        {
            scoreText.text = scoreValue.ToString();
        }
    }

    public void AddScore(int value)
    {
        m_currentScore += value;
        StartCoroutine(CountScoreRoutine());
    }
    
    IEnumerator CountScoreRoutine()
    {
        int iterations = 0;

        while (m_countValue < m_currentScore && iterations < 100000)
        {
            m_countValue += m_increment;
            UpdateScoreText(m_countValue);
            iterations++;
            yield return new WaitForSeconds(0.05f);
        }

        m_countValue = m_currentScore;
        UpdateScoreText(m_currentScore);
    }
}
