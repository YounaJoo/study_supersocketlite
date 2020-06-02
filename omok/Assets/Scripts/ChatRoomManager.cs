using System;
using System.Collections;
using System.Collections.Generic;
using ConnectToServer;
using UnityEngine;
using UnityEngine.UI;

/// <summary> 채팅 방 & UI Manager
/// 1. 방장, 상대방 결정 (Server가 Response 내려주는 대로?) 
/// 2. 결정한 상태값에 따라 UI 변경
/// 3. 게임 시작/준비 완료 btn
/// 4. 게임 나가기
/// </summary>

public class ChatRoomManager : MonoBehaviour
{
    // playerID
    public static string playerID;
    private int playerState = (int)PLAYER.player1;
    private string[] intro = new[] { "당신은 방장입니다.\n상대방이 준비 완료되면 게임 시작해주세요.", "준비 완료 버튼을 눌러주세요." };

    private MainClient mainClient = null;

    // hi, 자기소개
    public Text[] txtObj;

    // plyaerList 
    public Text[] playerList;
    
    enum PLAYER : int
    {
        player1 = 0, // 방장 (흑)
        player2 = 1, // (백)
    }
    
    void Awake() 
    {
        mainClient = new MainClient();
        // 로그인 시 셋팅 필요
        if (playerID == null)
        {
            playerID = "gkdld299";
        }
        
        // 방에 접속한 유저의 상태값 설정
        
        // GetObject
        
        // 서버가 내려줬을 때 방에 혼자라면 유저는 방장
        if (playerState == (int)PLAYER.player1)
        {
            //init(playerState);
        }
        else if (playerState == (int)PLAYER.player2)
        {
            //init(playerState);
        }
    }

    private void init(int state)
    {
        string playerid = playerID;
        // hi, 자기소개 설정
        txtObj[0].text = "hi " + playerid;
        txtObj[1].text = intro[state];

        // playerList 설정 --> 수정 요소 : 만약 플레이어가 1이라면 흑에도 상태값이 있어야 함.
        if (state == 0)
        {
            playerList[state].text = "흑 : " + playerid;
        }
        else
        {
            playerList[state].text = "백 : " + playerid;
        }
        
    }

    // 준비 완료
    
    // 게임 시작
    public void gameStart()
    {
        // 수정할 예정
        GameObject bg = this.gameObject.gameObject;
        bg.SetActive(false);
    }

    // 게임 (방) 나가기
    public void exit()
    {
    }
}
