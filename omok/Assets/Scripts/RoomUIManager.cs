using System;
using System.Collections;
using System.Collections.Generic;
using CSBaseLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

/// <summary> RoomUIManager 
/// 1. 
/// </summary>
public class RoomUIManager : MonoBehaviour
{
    private const int userCount = 2;
    
    private int num;
    
    private GameObject Canvas;
    private GameObject Notice;
    public GameObject ready;
 
    //private List<string> remoteUserID;
    private string[] remoteUserID = new string[userCount];
    private string userID;
    
    private short userPos = -1;
    private string[] introStr = new[] { "당신은 방장입니다.\n상대방이 준비 완료되면 게임 시작해주세요", "게임 준비 버튼을 눌러주세요.\n방장만이 게임 시작이 가능합니다." };

    public RoomUIManager()
    {
        for (int i = 0; i < userCount; i++)
        {
            remoteUserID[i] = null;
        }
        createLoginUI();
    }

    public void createLoginUI()
    {
        Canvas = (GameObject) Instantiate(Resources.Load("Canvas_login"));

        num = 0; // Login
    }

    public void roomEnterUIChange(string userID, List<string> remoteUserID, short userPos)
    {
        if (num == 1)
        {
            return;
        } 
        
        if(num == 0)   
        {
            Destroy(Canvas.gameObject);
        }

        this.remoteUserID = remoteUserID;
        this.userPos = userPos;
        this.userID = userID;
        Canvas = (GameObject)Instantiate(Resources.Load("Canvas_game"));
        num = 1; // room
        setRoomUI();
    }

    // 게임 방에서 게임 나가기 시 접속 종료 -> UI 변경
    public void exitUIChange()
    {
        if (num == 1)
        {
            Destroy(Canvas.gameObject);

            createLoginUI();
        }
        else
        {
            return;
        }
    }

    private void setRoomUI()
    {
        // HI, 인삿말, 흑, 백
        if (num != 1 || userPos == -1)
        {
            return;
        }

        GameObject roomIntro = Canvas.transform.GetChild(1).transform.GetChild(0).gameObject;
        roomIntro.transform.GetChild(0).gameObject.GetComponent<Text>().text = "HI " + userID;

        roomIntro.transform.GetChild(1).gameObject.GetComponent<Text>().text = introStr[userPos];

        setPlayerUI();
    }

    private void setPlayerUI()
    {
        GameObject[] userList = new GameObject[remoteUserID.Count];

        for (int i = 0; i < remoteUserID.Count; i++)
        {
            userList[i] = GameObject.Find("player" + i).gameObject;
            if (i == 0)
            {
                userList[i].GetComponent<Text>().text = "흑 : " + remoteUserID[i];
            } else if (i == 1)
            {
                userList[i].GetComponent<Text>().text = "백 : " + remoteUserID[i];
                ready = userList[i].transform.GetChild(0).gameObject;
                Debug.Log(ready.name);
            }
        }
    }

    public void setPlayerList(bool isAdd, string userID)
    {
        if (isAdd) // 유저 추가
        {
            remoteUserID.Add(userID);
            setPlayerUI();
            return;
        }
        
        // 유저 삭제
        foreach (string user in remoteUserID)
        {
            if (user == userID)
            {
                remoteUserID.Remove(user);
                break;
            }
        }
        
        setPlayerUI();
    }

    public void createNotice(ERROR_CODE errorCode)
    {
        Notice = (GameObject) Instantiate(Resources.Load("panel_notice"));
        Notice.transform.parent = Canvas.transform;
        
        Notice.GetComponent<Notice>().init(errorCode);
        Notice = null;
    }

    public void createNotice(string str)
    {
        Notice = (GameObject) Instantiate(Resources.Load("panel_notice"));
        Notice.transform.parent = Canvas.transform;
        
        Notice.GetComponent<Notice>().init(str);
        Notice = null;
    }
}
