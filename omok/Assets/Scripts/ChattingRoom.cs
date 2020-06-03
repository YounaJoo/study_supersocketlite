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
    
    public Button startBtn = null;
    public Button exitBtn = null;
    
    private void Start()
    {
        gameCanvas = this.gameObject;

        gameMenuPanel = gameCanvas.transform.GetChild(0).gameObject;
        chatRoomPanel = gameCanvas.transform.GetChild(1).gameObject;

        startBtn = chatRoomPanel.transform.GetChild(0).transform.GetChild(3).GetComponentInChildren<Button>();

        exitBtn = chatRoomPanel.transform.GetChild(0).transform.GetChild(4).GetComponentInChildren<Button>();
        exitBtn.onClick.AddListener(MainClient.Instance.exitBtn);
    }
}
