﻿using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using CSBaseLib;
using MessagePack;

namespace ChatServer
{
    public class PKHRoom : PKHandler
    {
        List<Room> RoomList = new List<Room>();
        private int StartRoomNumber;

        public void SetRoomList(List<Room> roomList)
        {
            RoomList = roomList;
            StartRoomNumber = roomList[0].Number;
        }

        public void RegistPacketHandler(Dictionary<int, Action<ServerPacketData>> pakcetHandlerMap)
        {
            pakcetHandlerMap.Add((int)PACKETID.REQ_ROOM_ENTER, RequestRoomEnter);
            pakcetHandlerMap.Add((int)PACKETID.REQ_ROOM_LEAVE, RequestLeave);
            pakcetHandlerMap.Add((int)PACKETID.NTF_IN_ROOM_LEAVE, NotifyLeaveInternal);
            pakcetHandlerMap.Add((int)PACKETID.REQ_ROOM_CHAT, RequestChat);
        }

        Room GetRoom(int roomNumber)
        {
            var index = roomNumber - StartRoomNumber;

            // RoomList.Count 는 System.Linq Enumerable의 static Method
            if (index < 0 || index >= RoomList.Count())
            {
                return null;
            }

            return RoomList[index];
        }

        // tuple 사용
        (bool, Room, RoomUser) CheckRoomAndRoomUser(int userNetSessionIndex)
        {
            var user = UserMgr.GetUser(userNetSessionIndex);
            if (user == null)
            {
                return (false, null, null);
            }

            var roomNumber = user.RoomNumber;
            var room = GetRoom(roomNumber);

            if (room == null)
            {
                return (false, null, null);
            }

            var roomUser = room.GetUser(userNetSessionIndex);

            if (roomUser == null)
            {
                return (false, room, null);
            }

            return (true, room, roomUser);
        }

        public void RequestRoomEnter(ServerPacketData packetData)
        {
            var sessionID = packetData.SessionID;
            var sessionIndex = packetData.SessionIndex;
            MainServer.MainLogger.Debug("RequestRoonEnter");

            try
            {
                var user = UserMgr.GetUser(sessionIndex);

                if (user == null || user.IsConfirm(sessionID) == false)
                {
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_USER, sessionID);
                    return;
                }

                if (user.IsStateRoom())
                {
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_STATE, sessionID);
                    return;
                }

                var reqData = MessagePackSerializer.Deserialize<PKTReqRoomEnter>(packetData.BodyData);
                var room = GetRoom(reqData.RoomNumber);

                if (room == null)
                {
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_ROOM_NUMBER, sessionID);
                    return;
                }

                if (room.AddUser(user.ID(), sessionIndex, sessionID) == false)
                {
                    ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_FAIL_ADD_USER, sessionID);
                    return;
                }
                
                user.EnteredRoom(reqData.RoomNumber);
                room.NotifyPacketUserList(sessionID);
                room.NofifyPacketNewUser(sessionIndex, user.ID());

                ResponseEnterRoomToClient(ERROR_CODE.NONE, sessionID);
                
                MainServer.MainLogger.Debug("RequestEnterInternal = Success");
            }
            catch (Exception e)
            {
                MainServer.MainLogger.Error(e.ToString());
            }
        }

        void ResponseEnterRoomToClient(ERROR_CODE errorCode, string sessionID)
        {
            var resRoomEnter = new PKTResRoomEnter()
            {
                Result = (short)errorCode
            };

            var bodyData = MessagePackSerializer.Serialize(resRoomEnter);
            var sendData = PacketToBytes.Make(PACKETID.RES_ROOM_ENTER, bodyData);

            ServerNetwork.SendData(sessionID, sendData);
        }

        public void RequestLeave(ServerPacketData packetData)
        {
            var sessionID = packetData.SessionID;
            var sessionIndex = packetData.SessionIndex;
            MainServer.MainLogger.Debug("로그인 요청 받음");

            try
            {
                var user = UserMgr.GetUser(sessionIndex);
                if (user == null)
                {
                    return;
                }

                if (LeaveRoomUser(sessionIndex, user.RoomNumber) == false)
                {
                    return;
                }
                
                user.LeaveRoom();
                ResponseLeaveRoomToClient(sessionID);
                MainServer.MainLogger.Debug("Room RequestLeave - Success");
            }
            catch (Exception e)
            {
                MainServer.MainLogger.Error(e.ToString());
            }
        }

        bool LeaveRoomUser(int sessionIndex, int roomNumber)
        {
            MainServer.MainLogger.Debug($"LeaveRoomUser. SessionIndex:{sessionIndex}");

            var room = GetRoom(roomNumber);
            if (room == null)
            {
                return false;
            }

            var roomUser = room.GetUser(sessionIndex);
            if (roomUser == null)
            {
                return false;
            }

            var userID = roomUser.UserID;
            room.RemoveUser(roomUser);
            
            room.NofifyPacketLeaveUser(userID);
            return true;
        }

        void ResponseLeaveRoomToClient(string sessionID)
        {
            var resRoomLeave = new PKTResRoomLeave()
            {
                Result = (short)ERROR_CODE.NONE
            };

            var bodyData = MessagePackSerializer.Serialize(resRoomLeave);
            var sendData = PacketToBytes.Make(PACKETID.RES_ROOM_LEAVE, bodyData);

            ServerNetwork.SendData(sessionID, sendData);
        }

        public void NotifyLeaveInternal(ServerPacketData packetData)
        {
            var sessionIndex = packetData.SessionIndex;
            MainServer.MainLogger.Debug($"NotifyLeaveInternal. SessionIndex:{sessionIndex}");

            var reqData = MessagePackSerializer.Deserialize<PKTInternalNTFRoomLeave>(packetData.BodyData);
            LeaveRoomUser(sessionIndex, reqData.RoomNumber);
        }

        public void RequestChat(ServerPacketData packetData)
        {
            var sessionID = packetData.SessionID;
            var sessionIndex = packetData.SessionIndex;
            MainServer.MainLogger.Debug("Room RequestChat");

            try
            {
                var roomObject = CheckRoomAndRoomUser(sessionIndex);

                if (roomObject.Item1 == false)
                {
                    return;
                }

                var reqData = MessagePackSerializer.Deserialize<PKTReqRoomChat>(packetData.BodyData);
                
                var notifyPacket = new PKTNtfRoomChat()
                {
                    UserID = roomObject.Item3.UserID,
                    ChatMessage = reqData.ChatMessage
                };

                var body = MessagePackSerializer.Serialize(notifyPacket);
                var sendData = PacketToBytes.Make(PACKETID.NTF_ROOM_CHAT, body);
                
                roomObject.Item2.Broadcast(-1, sendData);
                
                MainServer.MainLogger.Debug("Room RequestChat - Success");
            }
            catch (Exception e)
            {
                MainServer.MainLogger.Error(e.ToString());
            }
        }
    }
}