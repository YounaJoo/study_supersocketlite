using CSBaseLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using MessagePack;
using MessagePack.Resolvers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ConnectToServer
{
    /// <summary> Server 연결을 위한 Main Core
    /// 1. 접속 - network 사용
    /// 2. 로그인
    /// 3. 접속 해제(방 나가기 -> server 에서 방에 들어간 채 로그인을 하면 방 나가기, 로그인 해제 --> 서버차원에서 로그인 시 방 접속으로 변경 필요)
    /// 3-1. 일단 테스트 용으로 방 번호도 써서 입장할 수 있는 것으로 변경 --> 로그인 시 방 접속으로 변경
    /// 4. 채팅 : send
    /// 5. 채팅 : receive
    /// 6. 오목 : send
    /// 7. 오목 : receive
    /// 7. 오목 : timeout
    /// </summary>
    public class MainClient : MonoBehaviour
    {
        #region Init
        // 싱글 톤 사용
        private static MainClient instance = null; 
        
        private bool serializerRegistered = false;
        public short userPos = -1;

        private NetworkManager networkManager;
        private RoomUIManager roomUIManager;

        private PLAYER_STATE p_state = PLAYER_STATE.NONE;

        [HideInInspector] public string ip = "127.0.0.1";
        [HideInInspector] public int port = 12021;
        public string id = "";
        [HideInInspector] public string pass = "";

        private string sendComment = "";
        private string prevCommenct = "";
        private bool isTurn;
        public string[] remoteUserID;

        private List<string> msg;

        enum PLAYER_STATE
        {
            NONE = 0, // 아무런 상태 X
            REQ_LOGIN, // 로그인 요청
            RES_LOGIN,
            ROOMENTER, // 방 입장
            IN_ROOM, // 채팅 & 게임 레디
            GAME, // 게임 턴
            IDLE, // 쉬어가는 턴
            LEAVE, // 방 나가기
            GAMEOVER,
            ERROR // 에러
        }

        #endregion

        #region Unity

        public static MainClient Instance
        {
            get => instance;
            set => instance = value;
        }

        private void Awake()
        {
            networkManager = new NetworkManager();
            networkManager.debugPrintFunc = Debug.Log;

            msg = new List<string>();

            roomUIManager = new RoomUIManager();
            p_state = PLAYER_STATE.NONE;
            
            userPos = -1;
            remoteUserID = null;
            isTurn = false;

            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Destroy(this.gameObject);
            }
        }

        private void Start()
        {
            // Init MessagePack
            if (!serializerRegistered)
            {
                StaticCompositeResolver.Instance.Register(GeneratedResolver.Instance, StandardResolver.Instance );

                var option = MessagePackSerializerOptions.Standard.WithResolver(StaticCompositeResolver.Instance);
                MessagePackSerializer.DefaultOptions = option;
                serializerRegistered = true;
            }
        }

        private void Update()
        {
            switch (p_state)
            {
                case PLAYER_STATE.NONE :
                    // 만약 서버가 있지 않는데 접속되어져 있을 경우 오류
                    isDisconnected();
                    break;
                    
                case PLAYER_STATE.REQ_LOGIN : 
                    requestLogin();
                    break;
                
                case PLAYER_STATE.RES_LOGIN :
                    receiveLogin();
                    break;
                
                case PLAYER_STATE.ROOMENTER :
                    receiveRoomEnter();
                    break;
                
                case PLAYER_STATE.IN_ROOM : // Ready, Chatting
                    // 상대방, 유저 둘 다 게임 준비 완료를 하면 Game 시작
                    receiveGameRoom();
                    break;
                
                case PLAYER_STATE.GAME : // 게임 턴
                    requestOmok();
                    receiveOmok();
                    break;

                case PLAYER_STATE.IDLE : // 쉬어가기
                    receiveOmok();
                    break;
                
                case PLAYER_STATE.LEAVE : // 해당 방 나가기
                    Disconnected();
                    break;
                
                case PLAYER_STATE.GAMEOVER :
                    break;

                case PLAYER_STATE.ERROR :
                    Debug.Log("Error 발생");
                    p_state = PLAYER_STATE.NONE;
                    break;
            }
        }

        #endregion

        #region Request
        private void requestLogin()
        {
            var reqLogin = new OMKReqLogin() {UserID = id, AuthToken = pass};

            // 2020.06.02 : MessagePack Error!!
            var body = MessagePackSerializer.Serialize(reqLogin);
            var sendData = PacketToBytes.Make(PACKETID.REQ_LOGIN, body);

            PostSendPacket(sendData);

            p_state = PLAYER_STATE.RES_LOGIN;
        }

        public void requestRoomEnter()
        {
            var reqRoomEnter = new PKTReqRoomEnter()
            {
                RoomNumber = -1
            };

            var body = MessagePackSerializer.Serialize(reqRoomEnter);
            var sendData = PacketToBytes.Make(PACKETID.REQ_ROOM_ENTER, body);
            
            PostSendPacket(sendData);
            
            p_state = PLAYER_STATE.ROOMENTER;
        }

        public void requestChatting(string message)
        {
            if (p_state != PLAYER_STATE.IN_ROOM)
            {
                roomUIManager.createNotice("방 입장 오류\n연결을 끊습니다.");
                p_state = PLAYER_STATE.LEAVE;
                return;
            }
            
            var request = new OMKReqRoomChat()
            {
                ChatMessage =  message
            };
            
            var Body = MessagePackSerializer.Serialize(request);
            var sendData = CSBaseLib.PacketToBytes.Make(CSBaseLib.PACKETID.REQ_ROOM_CHAT, Body);
            PostSendPacket(sendData);
        }

        public void requestGameReady(short userPos)
        {
            var request = new OMKReqGameReady()
            {
                UserPos = userPos
            };
            
            var Body = MessagePackSerializer.Serialize(request);
            var sendData = CSBaseLib.PacketToBytes.Make(PACKETID.REQ_GAME_READY, Body);
            
            PostSendPacket(sendData);
        }

        public void requestOmok()
        {
            if (Input.GetMouseButtonDown(0) && !isTurn)
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                
                Debug.Log($"Mouse Position X {mousePos.x} Y {mousePos.y}");

                if (!Omok.Instance.ChkMouseOn(mousePos.x, mousePos.y))
                {
                    return;
                }

                Tuple<int, int> index = Omok.Instance.GetOmokIndex(mousePos.x, mousePos.y);
                
                var request = new OMKReqOmokGame()
                {
                    X = index.Item1,
                    Y = index.Item2
                };

                var Body = MessagePackSerializer.Serialize(request);
                var sendData = CSBaseLib.PacketToBytes.Make(PACKETID.REQ_OMOK_TURN, Body);
                
                PostSendPacket(sendData);
            }
        }
        #endregion

        #region receive

        public void receiveLogin()
        {
            var packet = networkManager.getPacket();
            Debug.Log($"receiveLogin PACKETID : {(ERROR_CODE)packet.PacketID}");
            if (packet.PacketID == (UInt16) PACKETID.RES_LOGIN)
            {
                var resData = MessagePackSerializer.Deserialize<OMKResLogin>(packet.BodyData);
                if (resData.Result == (short)ERROR_CODE.NONE)
                {
                    requestRoomEnter();
                }
                else
                {
                    roomUIManager.createNotice((ERROR_CODE)resData.Result);
                    p_state = PLAYER_STATE.NONE;
                }
            }
            else if (packet.PacketID == PacketDef.SYS_PACKET_ID_DISCONNECT_FROM_SERVER)
            {
                roomUIManager.createNotice("문제 발생\n연결을 끊습니다.");
                p_state = PLAYER_STATE.LEAVE;
            }
        }

        public void receiveGameRoom()
        { 
            var packetList = networkManager.GetPacket();
            
            foreach (var packet in packetList)
            {
                
                if (packet.PacketID == (UInt16) PACKETID.NTF_ROOM_CHAT) // 채팅 중
                {
                    var message = MessagePackSerializer.Deserialize<OMKResRoomChat>(packet.BodyData);
                    GameObject.Find("Canvas_game(Clone)").GetComponent<ChattingRoom>().chatting(message.UserID, message.ChatMessage);
                    
                } else if (packet.PacketID == (UInt16) PACKETID.NTF_ROOM_NEW_USER) // 새로운 유저 입장
                {
                    var reqData = MessagePackSerializer.Deserialize<OMKRoomNewUser>(packet.BodyData);
                    roomUIManager.setPlayerList(true, reqData.UserID);
                    
                } else if (packet.PacketID == (UInt16) PACKETID.NTF_ROOM_LEAVE_USER) // 유저가 나감 
                {
                    var reqData = MessagePackSerializer.Deserialize<OMKRoomLeaveUser>(packet.BodyData);
                    roomUIManager.setPlayerList(false, reqData.UserID);
                    roomUIManager.getGameReady(false);
                    
                } else if (packet.PacketID == (UInt16) PACKETID.RES_GAME_READY) // 게임 레디 None 일 때에는 player 2가 ready 했을 뿐
                {
                    var resData = MessagePackSerializer.Deserialize<OMKResGameReady>(packet.BodyData);
                    
                    if (resData.Result == (UInt16) ERROR_CODE.NONE)
                    {
                        roomUIManager.getGameReady(true);
                        continue;
                    }
                    roomUIManager.createNotice((ERROR_CODE)resData.Result);
                }
                else if (packet.PacketID == (UInt16) PACKETID.NTF_GAME_READY) // 게임 시작
                {
                    var resData = MessagePackSerializer.Deserialize<OMKNtfGameReady>(packet.BodyData);
                    if (resData.Result != (UInt16) ERROR_CODE.NONE)
                    {
                        roomUIManager.createNotice((ERROR_CODE)resData.Result);
                        continue;
                    }
                    
                    roomUIManager.gameStart();

                    switch (userPos)
                    {
                        case -1 :
                            roomUIManager.createNotice("문제 발생\n연결을 끊습니다.");
                            p_state = PLAYER_STATE.NONE;
                            return;
                        case 0 : // 선
                            roomUIManager.SetOmokCursor(true);
                            p_state = PLAYER_STATE.GAME;
                            break; 
                        case 1 : // 후
                            roomUIManager.SetOmokCursor(false);
                            p_state = PLAYER_STATE.IDLE;
                            break;
                    }
                }
                else if (packet.PacketID == PacketDef.SYS_PACKET_ID_DISCONNECT_FROM_SERVER)
                {
                    roomUIManager.createNotice("문제 발생\n연결을 끊습니다.");
                    p_state = PLAYER_STATE.LEAVE;
                }
            }
        }

        public void receiveRoomEnter()
        {
            var packetList = networkManager.GetPacket();
            
            foreach (var packet in packetList)
            {
                if (packet.PacketID == (UInt16) PACKETID.NTF_ROOM_USER_LIST)
                {
                    var resData = MessagePackSerializer.Deserialize<OMKRoomUserList>(packet.BodyData);
                    
                    remoteUserID = resData.UserIDList;
                    Debug.Log($"NTF_ROOM_USER_LIST remoteUserID1 {remoteUserID[0]} remoteUserID2 {remoteUserID[1]}");
                } 
                else if (packet.PacketID == (UInt16) PACKETID.RES_ROOM_ENTER) // 룸에 있는 유저 정보
                {
                    var resData = MessagePackSerializer.Deserialize<OMKResRoomEnter>(packet.BodyData);

                    if (resData.Result != (short) ERROR_CODE.NONE)
                    {
                        roomUIManager.createNotice((ERROR_CODE)resData.Result);
                        p_state = PLAYER_STATE.LEAVE;
                        return;
                    }
                    
                    this.userPos = resData.UserPos;

                    string[] tempRemoteUserId = remoteUserID;
                    int otherUserPos = (userPos == 1) ? 0 : 1;
                    
                    // temp
                    Debug.Log($"tempID1 {tempRemoteUserId[0]} tempID2 {tempRemoteUserId[1]}");

                    for (int i = 0; i < tempRemoteUserId.Length; i++)
                    {
                        if (id == tempRemoteUserId[i])
                        {
                            remoteUserID[userPos] = tempRemoteUserId[i];
                            Debug.Log($"sameUser otherUserPos : {otherUserPos} user : {tempRemoteUserId[i]}");
                            continue;
                        }
                        Debug.Log($"otherUser otherUserPos : {otherUserPos} user : {tempRemoteUserId[i]}");
                        remoteUserID[otherUserPos] = tempRemoteUserId[i];
                    }
                    Debug.Log($"결과 : remoteUserID1 {remoteUserID[0]} remoteUserID2 {remoteUserID[1]}");

                    roomUIManager.roomEnterUIChange(id, remoteUserID, userPos);
                    
                    p_state = PLAYER_STATE.IN_ROOM;
                }
                else if (packet.PacketID == PacketDef.SYS_PACKET_ID_DISCONNECT_FROM_SERVER)
                {
                    roomUIManager.createNotice("문제 발생\n연결을 끊습니다.");
                    p_state = PLAYER_STATE.LEAVE;
                }
            }
        }

        private void receiveOmok()
        {
            var packetList = networkManager.GetPacket();

            foreach (var packet in packetList)
            {
                if (packet.PacketID == (UInt16) PACKETID.RES_OMOK_TURN)
                {
                    var resData = MessagePackSerializer.Deserialize<OMKResOmokTurn>(packet.BodyData);
                    if (resData.Result != (short) ERROR_CODE.NONE)
                    {
                        //roomUIManager.createNotice((ERROR_CODE) resData.Result);
                        return;
                    }

                    isTurn = false;

                } else if (packet.PacketID == (UInt16) PACKETID.NTF_OMOK_TURN && !isTurn)
                {
                    var resData = MessagePackSerializer.Deserialize<OMKNtfOmokTurn>(packet.BodyData);
                    //roomUIManager.createNotice($"X : {resData.X} Y : {resData.Y}");
                    Debug.Log($"response omok position {resData.X} {resData.Y}");

                    roomUIManager.CreateOmok(resData.UserPos, resData.X, resData.Y);

                    if (p_state == PLAYER_STATE.GAME)
                    {
                        roomUIManager.SetOmokCursor(false);
                        p_state = PLAYER_STATE.IDLE;
                    }
                    else if (p_state == PLAYER_STATE.IDLE)
                    {
                        roomUIManager.SetOmokCursor(true);
                        p_state = PLAYER_STATE.GAME;
                    }
                }else if (packet.PacketID == (UInt16) PACKETID.NTF_ROOM_LEAVE_USER)
                {
                    var resData = MessagePackSerializer.Deserialize<OMKRoomLeaveUser>(packet.BodyData);
                    //roomUIManager.createNotice($"Leave {resData.UserID}\nYou Win!"); // 이렇게 하니 Login이 끊기고, createLoginUI로 돌아감
                    roomUIManager.gameOver(resData.UserID, true);
                }else if (packet.PacketID == (UInt16) PACKETID.NTF_OMOK_GAME_RES)
                {
                    var resData = MessagePackSerializer.Deserialize<OMKNtfOmokGameRes>(packet.BodyData);
                    if (resData.userPos != userPos)
                    {
                        Debug.Log("You Lose");
                        roomUIManager.gameOver("GAME OVER", false);
                        p_state = PLAYER_STATE.GAMEOVER;
                    }
                    else
                    {
                        Debug.Log("You Win!");
                        roomUIManager.gameOver("GAME OVER", true);
                        p_state = PLAYER_STATE.GAMEOVER;
                    }
                }
            }
        }

        #endregion
        
        #region Network
        public void PostSendPacket(byte[] sendData)
        {
            if (networkManager.IsConnected == false)
            {
                roomUIManager.createNotice("서버 연결이 되어 있지 않습니다.");
                p_state = PLAYER_STATE.NONE;
                return;
            }

            // 패킷 감싸서 network.Send(packetData)
            networkManager.Send(sendData);
        }

        public void Disconnected()
        {
            restartGame();
            networkManager.disconnect();
            roomUIManager.exitUIChange();
            p_state = PLAYER_STATE.NONE;
        }

        public void isDisconnected()
        {
            if (networkManager.IsConnected == false)
            {
                networkManager.disconnect();
            }
        }
        #endregion

        #region Game

        public void restartGame()
        {
            userPos = -1;
            id = null;
            remoteUserID = null;
            isTurn = false;
        }
        
        public void gameReady()
        {
            if (userPos == -1)
            {
                roomUIManager.createNotice("알 수 없는 오류 발생");
                p_state = PLAYER_STATE.ERROR;
                return;
            }

            requestGameReady(userPos);
        }
        #endregion
        
        #region UI
        public void loginBtn()
        {
            // get : input Id / pass
            GameObject input_id = GameObject.Find("input_id").gameObject;
            GameObject input_pass = GameObject.Find("input_pass").gameObject;
            GameObject input_ip = GameObject.Find("input_ip").gameObject;
            GameObject input_port = GameObject.Find("input_port").gameObject;
            pass = input_pass.GetComponentInChildren<Text>().gameObject.GetComponent <Text>().text;
            id = input_id.GetComponentInChildren<Text>().gameObject.GetComponent<Text>().text;
            ip = input_ip.GetComponentInChildren<Text>().gameObject.GetComponent<Text>().text; 
            port = Int32.Parse(input_port.GetComponentInChildren<Text>().gameObject.GetComponent<Text>().text);

            Debug.Log("ID : " + id);
            Debug.Log("PASS : " + pass);
            Debug.Log("IP : " + ip);
        
            if ((id != "") && (pass != "") && (ip != "") && (port != null))
            {
                bool isConnected = networkManager.connect(ip, port);
                if (isConnected)
                {
                    p_state = PLAYER_STATE.REQ_LOGIN;
                }
                else
                {
                    roomUIManager.createNotice("서버 연결 실패\n서버 상태를 확인해주세요.");
                    p_state = PLAYER_STATE.LEAVE;
                }
            }
            else
            {
                roomUIManager.createNotice("ID, PASS, IP를\n확인해주세요.");
                p_state = PLAYER_STATE.NONE;
            }
        }
        #endregion

        #region Test
        public void exitBtn()
        {
            if (isTurn) roomUIManager.SetOmokCursor(false);
            p_state = PLAYER_STATE.LEAVE;
            Debug.Log("게임 나가기");
        }
        #endregion

    }
}