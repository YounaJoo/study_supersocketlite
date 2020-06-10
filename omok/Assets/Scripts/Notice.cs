using System;
using System.Collections;
using System.Collections.Generic;
using CSBaseLib;
using UnityEngine;
using UnityEngine.UI;

public class Notice : MonoBehaviour
{
    private GameObject noticePanel;
    private Button btn_exit;
    private Text text_notice;

    public void init(ERROR_CODE error_code)
    {
        noticePanel.GetComponent<RectTransform>().localPosition = Vector3.zero;
        
        string str = errorMsg((short)error_code);

        text_notice.text = str;
    }

    public void init(string str)
    {
        noticePanel.GetComponent<RectTransform>().localPosition = Vector3.zero;

        text_notice.text = str;
    }
    
    private void Awake()
    {
        noticePanel = this.gameObject;

        btn_exit = noticePanel.transform.GetChild(2).GetComponent<Button>();
        btn_exit.onClick.AddListener(this.exitBtn);

        text_notice = noticePanel.transform.GetChild(1).GetComponentInChildren<Text>();
    }

    private void exitBtn()
    {
        Destroy(this.gameObject);
    }

    private string errorMsg(short error_code)
    {
        string str = null;
        switch (error_code)
        {
            case (short)ERROR_CODE.NONE :
                return str = "null";
            case (short) ERROR_CODE.LOGIN_INVALID_AUTHTOKEN :
                str = "LOGIN_INVALID_AUTHTOKEN";
                break;
            case (short) ERROR_CODE.ADD_USER_DUPLICATION :
                str = "ADD_USER_DUPLICATION";
                break;
            case (short) ERROR_CODE.REMOVE_USER_SEARCH_FAILURE_USER_ID :
                str = "REMOVE_USER_SEARCH_FAILURE_USER_ID";
                break;
            case (short) ERROR_CODE.USER_AUTH_ALREADY_SET_AUTH :
                str = "USER_AUTH_ALREADY_SET_AUTH";
                break;
            case (short) ERROR_CODE.USER_AUTH_SEARCH_FAILURE_USER_ID :
                str = "USER_AUTH_SEARCH_FAILURE_USER_ID";
                break;
            case (short) ERROR_CODE.LOGIN_ALREADY_SERVER :
                str = "LOGIN_ALREADY_SERVER";
                break;
            case (short) ERROR_CODE.LOGIN_ALREADY_WORKING :
                str = "LOGIN_ALREADY_WORKING";
                break;
            case (short) ERROR_CODE.LOGIN_FULL_USER_COUNT :
                str = "LOGIN_FULL_USER_COUNT";
                break;
            case (short) ERROR_CODE.ROOM_ENTER_INVALID_STATE :
                str = "ROOM_ENTER_INVALID_STATE";
                break;
            case (short) ERROR_CODE.ROOM_ENTER_INVALID_USER :
                str = "ROOM_ENTER_INVALID_USER";
                break;
            case (short) ERROR_CODE.ROOM_ENTER_ERROR_SYSTEM :
                str = "ROOM_ENTER_ERROR_SYSTEM";
                break;
            case (short) ERROR_CODE.ROOM_ENTER_INVALID_ROOM_NUMBER :
                str = "ROOM_ENTER_INVALID_ROOM_NUMBER";
                break;
            case (short) ERROR_CODE.ROOM_ENTER_FAIL_ADD_USER :
                str = "ROOM_ENTER_FAIL_ADD_USER";
                break;
            case (short) ERROR_CODE.GAME_READY_INVALID_STATE :
                str = "GAME_READY_INVALID_STATE";
                break;
            case (short) ERROR_CODE.GAME_READY_INVALIED_USER :
                str = "GAME_READY_INVALIED_USER";
                break;
            case (short) ERROR_CODE.GAME_READY_INVALID_CHECK_OTHER_USER :
                str = "GAME_READY_INVALID_CHECK_OTHER_USER";
                break;
            case (short) ERROR_CODE.REDIS_INIT_FAIL :
                str = "REDIS_INIT_FAIL";
                break;
        }

        return str;
    }
}
