using System;
using System.Collections;
using System.Collections.Generic;
using ConnectToServer;
using UnityEngine;
using UnityEngine.UI;

public class ChattingRoom : MonoBehaviour
{
    private GameObject gameCanvas = null;
    private GameObject gameMenuPanel = null;
    private GameObject chatRoomPanel = null;
    public GameObject chatView = null;

    private Button startBtn = null;
    private Button exitBtn = null;
    private Button chatBtn = null;
    
    private Text sendMessage = null;
    private Text chatText = null;

    private float time = 0;
    
    private void Start()
    {
        gameCanvas = this.gameObject;

        gameMenuPanel = gameCanvas.transform.GetChild(0).gameObject;
        chatRoomPanel = gameCanvas.transform.GetChild(1).gameObject;

        startBtn = chatRoomPanel.transform.GetChild(0).transform.GetChild(3).GetComponentInChildren<Button>();

        exitBtn = chatRoomPanel.transform.GetChild(0).transform.GetChild(4).GetComponentInChildren<Button>();
        exitBtn.onClick.AddListener(delegate { MainClient.Instance.exitBtn(); });

        sendMessage = chatRoomPanel.transform.GetChild(1).transform.GetChild(1).transform.GetChild(1).GetComponent<Text>();
        chatBtn = chatRoomPanel.transform.GetChild(1).transform.GetChild(2).GetComponentInChildren<Button>();
        chatBtn.onClick.AddListener(delegate { MainClient.Instance.requestChatting(sendMessage.text); });

        chatView = chatRoomPanel.transform.GetChild(1).transform.GetChild(0).gameObject;
        chatText = chatView.GetComponentInChildren<Text>();
    }

    public void chatting(string userID, string message)
    {
        chatText.text += userID + " : " + message + "\n";
        chatView.GetComponent<ScrollRect>().verticalNormalizedPosition = 0.0f;
    }
}
