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
        public void RegistPacketHandler(Dictionary<int, Action<ServerPacketData>> packetHandlerMap)
        {
            // omok Game Request or 일시정지 or timeout
            packetHandlerMap.Add((int)PACKETID.REQ_OMOK_GAME, requestOmokGame);
        }
        
        public void SetOmokList(List<Omok> omokList, int omokCount)
        {
            OmokList = omokList;
            OmokCount = omokCount;
        }

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
            var reqOmk = new Omok(reqData.X, reqData.Y);
            
            MainServer.MainLogger.Info($"reqOmok X : {reqOmk.x} Y : {reqOmk.y}");
            MainServer.MainLogger.Info($"OmokList X : {OmokList[0].x} Y : {OmokList[0].y}");

            // 해당 패킷에 있는 x와 y에서 가장 가까운 점을 찾고 isActivity 체크
            responseOmokGameToClinet(ERROR_CODE.NONE, sessionID);
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