using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary> RoomUIManager 
/// 1. 
/// </summary>
public class RoomUIManager : MonoBehaviour
{
    private GameObject Canvas;
    private int num;

    public RoomUIManager()
    {
        createLoginUI();
    }

    public void createLoginUI()
    {
        Canvas = (GameObject) Instantiate(Resources.Load("Canvas_login"));
        num = 0; // Login
    }

    public void roomEnterUIChange()
    {

        if (num == 1)
        {
            return;
        } 
        
        if(num == 0)   
        {
            Destroy(Canvas.gameObject);
        }
        
        Canvas = (GameObject)Instantiate(Resources.Load("Canvas_game"));
        num = 1; // room
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
}
