using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameMenu : MonoBehaviour
{
    private const int USERCOUNT = 2;
    
    public GameObject[] playerList = new GameObject[USERCOUNT];
    public GameObject currentPlayer;
    public GameObject gameLog;
    public GameObject pausePanel;
    public Text timer;

    public void setPlayer(string[] userList)
    {
        for (int i = 0; i < USERCOUNT; i++)
        {
            playerList[i].GetComponent<Text>().text = userList[i];
        }
    }

}
 