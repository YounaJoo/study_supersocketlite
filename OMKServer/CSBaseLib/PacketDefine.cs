﻿namespace CSBaseLib
{
    // 0 ~ 9999
    public enum ERROR_CODE : short
    {
        NONE                        = 0, // 에러가 아니다

        // 서버 초기화 에라
        REDIS_INIT_FAIL             = 1,    // Redis 초기화 에러

        // 로그인 
        LOGIN_INVALID_AUTHTOKEN             = 1001, // 로그인 실패: 잘못된 인증 토큰
        ADD_USER_DUPLICATION                = 1002,
        REMOVE_USER_SEARCH_FAILURE_USER_ID  = 1003,
        USER_AUTH_SEARCH_FAILURE_USER_ID    = 1004,
        USER_AUTH_ALREADY_SET_AUTH          = 1005,
        LOGIN_ALREADY_WORKING = 1006,
        LOGIN_FULL_USER_COUNT = 1007,
        LOGIN_ALREADY_SERVER = 1008,

        DB_LOGIN_INVALID_PASSWORD   = 1011,
        DB_LOGIN_EMPTY_USER         = 1012,
        DB_LOGIN_EXCEPTION          = 1013,

        ROOM_ENTER_INVALID_STATE = 1021,
        ROOM_ENTER_INVALID_USER = 1022,
        ROOM_ENTER_ERROR_SYSTEM = 1023,
        ROOM_ENTER_INVALID_ROOM_NUMBER = 1024,
        ROOM_ENTER_FAIL_ADD_USER = 1025,
        
        GAME_READY_INVALID_CHECK_OTHER_USER = 1031,
        GAME_READY_INVALID_STATE = 1032,
        GAME_READY_INVALIED_USER = 1033,
        
        OMOK_GAME_RESULT_WIN = 1034,
        OMOK_GAME_RESULT_LOSS = 1035,
        OMOK_GAME_RESULT_TIE = 1038,
        
        OMOK_GAME_INVALIED_POSITION = 1036,
        OMOK_GAME_INVALIED_PACKET = 1037,
        OMOK_GAME_ALREADY_OMOK = 1038,
    }

    // 1 ~ 10000
    public enum PACKETID : int
    {
        REQ_RES_TEST_ECHO = 101,
        
               
        // 클라이언트
        CS_BEGIN        = 1001,

        REQ_LOGIN       = 1002,
        RES_LOGIN       = 1003,
        NTF_MUST_CLOSE       = 1005,

        REQ_ROOM_ENTER = 1015,
        RES_ROOM_ENTER = 1016,
        NTF_ROOM_USER_LIST = 1017,
        NTF_ROOM_NEW_USER = 1018,

        REQ_ROOM_LEAVE = 1021,
        RES_ROOM_LEAVE = 1022,
        NTF_ROOM_LEAVE_USER = 1023,

        REQ_ROOM_CHAT = 1026,
        NTF_ROOM_CHAT = 1027,
        
        REQ_GAME_READY = 1028,
        RES_GAME_READY = 1029,
        NTF_GAME_READY = 1030,
        
        REQ_OMOK_TURN = 1031,
        RES_OMOK_TURN = 1032,
        NTF_OMOK_TURN = 1034,
        NTF_OMOK_GAME_RES = 1035,

        CS_END          = 1100,


        // 시스템, 서버 - 서버
        SS_START    = 8001,

        NTF_IN_CONNECT_CLIENT = 8011,
        NTF_IN_DISCONNECT_CLIENT = 8012,

        REQ_SS_SERVERINFO = 8021,
        RES_SS_SERVERINFO = 8023,

        REQ_IN_ROOM_ENTER = 8031,
        RES_IN_ROOM_ENTER = 8032,

        NTF_IN_ROOM_LEAVE = 8036,


        // DB 8101 ~ 9000
        REQ_DB_LOGIN = 8101,
        RES_DB_LOGIN = 8102,
    }
}