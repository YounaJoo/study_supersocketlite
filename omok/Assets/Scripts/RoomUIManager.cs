using System;
using System.Collections;
using System.Collections.Generic;
using CSBaseLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

/// <summary> RoomUIManager 
/// 1. 
/// </summary>
public class RoomUIManager : MonoBehaviour
{
    private const int remoteUserCount = 2;

    private int num;
    
    private GameObject Canvas;
    private GameObject Notice;
    private GameObject OmokGame;

    private Image ready;
    private GameMenu gameMenu;
    
    private string[] remoteUserID = new string[remoteUserCount];
    private string userID;
    
    private short userPos = -1;
    private string[] introStr = new[] { "당신은 방장입니다.\n상대방이 준비 완료되면 게임 시작해주세요", "게임 준비 버튼을 눌러주세요.\n방장만이 게임 시작이 가능합니다." };

    public RoomUIManager()
    {
        for (int i = 0; i < remoteUserCount; i++)
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

    public void roomEnterUIChange(string userID, string[] remoteUserID, short userPos)
    {
        if (num == 0) Destroy(Canvas.gameObject);
        else if (num == 1) return;

        this.remoteUserID = remoteUserID;
        this.userPos = userPos;
        this.userID = userID;
        
        Canvas = (GameObject)Instantiate(Resources.Load("Canvas_game"));
        
        num = 1; // room
        
        setRoomUI();
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

        ready = GameObject.Find("ready").gameObject.GetComponent<Image>();
        
        setPlayerUI();
    }

    private void setPlayerUI()
    {
        GameObject[] userList = new GameObject[remoteUserCount];

        for (int i = 0; i < remoteUserCount; i++)
        {
            userList[i] = GameObject.Find("ready_player" + i).gameObject;
            if (i == 0)
            {
                userList[i].GetComponent<Text>().text = "흑 : " + remoteUserID[i];
            } else if (i == 1)
            {
                userList[i].GetComponent<Text>().text = "백 : " + remoteUserID[i];
            }
        }
    }

    public void setPlayerList(bool isAdd, string userID)
    {
        if (isAdd) // 유저 추가
        {
            for (int i = 0; i < remoteUserCount; i++)
            {
                if (remoteUserID[i] == null)
                {
                    remoteUserID[i] = userID;
                    break;
                }
            }
            
            setPlayerUI();
            return;
        }
        
        // 유저 삭제
        for (int i = 0; i < remoteUserCount; i++)
        {
            if (userID == remoteUserID[i])
            {
                remoteUserID[i] = null;
                break;
            }
        }
        
        setPlayerUI();
    }

    public void getGameReady(bool isReady)
    {
        if (num != 1)
        {
            return;
        }

        var tempColor = ready.color;
        if (isReady) tempColor.a = 255.0f;
        else tempColor.a = 0.0f;
        
        ready.color = tempColor;
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
    
    // 게임 종료
    public void gameOver(string reason, bool isWin)
    {
        Notice = (GameObject) Instantiate(Resources.Load("panel_notice"));
        Notice.transform.parent = Canvas.transform;

        Notice.GetComponent<Notice>().init(reason, isWin);
        Notice = null;
    }
    
    // 게임 시작
    public void gameStart()
    {
        Debug.Log("GameStart");

        OmokGame = Instantiate(Resources.Load("Game_obj")) as GameObject;

        GameObject.Find("Room").gameObject.SetActive(false);

        gameMenu = GameObject.Find("Menu").GetComponent<GameMenu>();
        gameMenu.setPlayer(remoteUserID);
        
        
    }

    // 게임 방에서 게임 나가기 시 접속 종료 -> UI 변경
    public void exitUIChange()
    {
        if (num == 1)
        {
            Destroy(Canvas.gameObject);

            if (OmokGame != null)
            {
                Destroy(OmokGame.gameObject);
            }
            
            createLoginUI();
        }
    }

    public void SetOmokCursor(bool isActivity)
    {
        //OmokGame.GetComponent<Omok>().setOmokCurr(isActivity, userPos);
        Omok.Instance.setOmokCurr(isActivity, userPos);
    }

    public void CreateOmok(short userPos, float x, float y)
    {
        Vector2 omokPos = new Vector2(x, y);
        //OmokGame.GetComponent<Omok>().createOmok(userPos, omokPos);
        Omok.Instance.createOmok(userPos, omokPos);
    }
}
