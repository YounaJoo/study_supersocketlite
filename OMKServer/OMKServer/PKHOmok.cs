using System;
using System.Collections.Generic;
using System.Diagnostics;
using CSBaseLib;
using MessagePack;

namespace OMKServer
{
    public class PKHOmok : PKHandler
    {
        private List<Omok> OmokList = new List<Omok>();
        private int OmokCount;
        private float Dis;
        private OmokManager OmokManager = new OmokManager();
        
        public void RegistPacketHandler(Dictionary<int, Action<ServerPacketData>> packetHandlerMap)
        {
            // omok Game Request or 일시정지 or timeout
            packetHandlerMap.Add((int)PACKETID.REQ_OMOK_GAME, requestOmokGame);
        }
        
        /*public void SetOmokList(List<Omok> omokList, int omokCount)
        {
            OmokList = omokList;
            OmokCount = omokCount;
        }*/

        public void requestOmokGame(ServerPacketData packetData)
        {
            var sessionIndex = packetData.SessionIndex;
            var sessionID = packetData.SessionID;

            var user = UserMgr.GetUser(sessionIndex);

            if (user == null) // 유저가 없을 경우 에러
            {
                responseOmokGameToClinet(ERROR_CODE.OMOK_GAME_INVALIED_PACKET, sessionID);
                return;
            }
            
            var reqData = MessagePackSerializer.Deserialize<OMKReqOmokGame>(packetData.BodyData);
            var reqOmok = new Omok(reqData.X, reqData.Y);
            
            // min과 max 를 넘어서면 error
            if (reqOmok.x < OmokManager.MIN_X || reqOmok.x > OmokManager.MAX_X ||
                reqOmok.y < OmokManager.MIN_Y || reqOmok.y > OmokManager.MAX_Y)
            {
                responseOmokGameToClinet(ERROR_CODE.OMOK_GAME_INVALIED_POSITION, sessionID);
                return;
            }

            // 해당 패킷에 있는 x와 y에서 가장 가까운 점을 찾고(근사값) isActivity 체크
            float tempX = (float)Math.Round(reqOmok.x - (reqOmok.x % OmokManager.DIS), 2);
            float tempY = (float)Math.Round(reqOmok.y - (reqOmok.y % OmokManager.DIS), 2);
            
            var newOmok = new Omok(tempX, tempY);
            
            // Omok 있는지 찾기
            if (OmokList.IsEmpty()) // 
            {
                newOmok.setActivity(true);
                OmokList.Add(newOmok);
            }
            else
            {
                foreach (var omok in OmokList)
                {
                    if (omok.x == newOmok.x && omok.y == newOmok.y && omok.isActivity)
                    {
                        responseOmokGameToClinet(ERROR_CODE.OMOK_GAME_ALREADY_OMOK, sessionID);
                        return;
                    }
                }
                newOmok.setActivity(true);
                OmokList.Add(newOmok);
            }
            
            responseOmokGameToClinet(ERROR_CODE.NONE, sessionID);
            MainServer.MainLogger.Info($"reqOmok X : {reqOmok.x} Y : {reqOmok.y}");
            MainServer.MainLogger.Info($"NewOmok X : {newOmok.x} Y : {newOmok.y}");
            // Notify
        }

        public void responseOmokGameToClinet(ERROR_CODE errorCode, string sessionID)
        {
            var resOmokGame = new OMKResOmokGame()
            {
                Result = (short)errorCode
            };

            var bodyData = MessagePackSerializer.Serialize(resOmokGame);
            var sendData = PacketToBytes.Make(PACKETID.RES_LOGIN, bodyData);

            ServerNetwork.SendData(sessionID, sendData);
            
            MainServer.MainLogger.Info($"RequestOmok, Send Response Omok Message : {resOmokGame.Result}");
        }
    }
}