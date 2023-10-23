using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessageWindow : MonoBehaviour
{
    [SerializeField] private Image messageIcon;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI buttonText;

    public void ShowMessage(Sprite sprite = null, string message = "", string buttonMsg = "START")
    {
        if (messageIcon != null)
        {
            messageIcon.sprite = sprite;
        }
        if (messageIcon != null)
        {
            messageText.text = message;
        }
        if (buttonText != null)
        {
            buttonText.text = buttonMsg;
        }
    }
}
