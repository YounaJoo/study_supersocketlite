using System;
using System.Collections.Generic;
using System.Linq;
using CSBaseLib;

namespace ChatServer
{
    // CommandLine에 입력한 MaxUserCount 저장 및 관리 && 유저 별 SequenceNumber 부여하는 관리자
    public class UserManager
    {
        private int MaxUserCount;
        private UInt32 UserSequenceNumber = 0;
        
        // 유저를 구분할 Map (sessionIndex, User 객체)
        Dictionary<int, User> UserMap = new Dictionary<int, User>();
        Dictionary<string, int> UserIdMap = new Dictionary<string, int>(); 

        public void Init(int maxUserCount)
        {
            this.MaxUserCount = maxUserCount;
        }

        // 유저추가(시퀀스 넘버, 세션 ID, 세션 인덱스 부여) 
        public ERROR_CODE AddUser(string userID, string sessionID, int sessionIndex)
        {
            // 유저 수가 꽉 찬 경우 에러 코드 발생
            if (IsFullUserCount())
            {
                return ERROR_CODE.LOGIN_FULL_USER_COUNT;
            }

            // 해당 세션 인덱스가 존재할 때, 에러 코드 발생
            if (UserMap.ContainsKey(sessionIndex))
            {
                return ERROR_CODE.ADD_USER_DUPLICATION;
            }
            
            // userID가 존재할 때 fail
            if (UserIdMap.ContainsKey(userID))
            {
                return ERROR_CODE.LOGIN_ALREADY_WORKING;
            }
            
            UserIdMap.Add(userID, sessionIndex);

            ++UserSequenceNumber;
            
            var user = new User();
            user.Set(UserSequenceNumber, sessionID, sessionIndex, userID);
            UserMap.Add(sessionIndex, user);

            return ERROR_CODE.NONE;
        }
        
        // 유저 제거
        public ERROR_CODE RemoveUser(int sessionIndex)
        {
            // 유저 삭제 실패 시 에러 코드 발생
            if (UserMap.Remove(sessionIndex) == false || UserIdMap.Remove(GetUser(sessionIndex).ID()) == false)
            {
                return ERROR_CODE.REMOVE_USER_SEARCH_FAILURE_USER_ID;
            }

            return ERROR_CODE.NONE;
        }

        // 해당 sessionID에 해당되는 유저를 Get 하여 return
        public User GetUser(int sessionIndex)
        {
            // null 값을 가리키는 객체 선언
            User user = null;
            // sessionID에 해당하는 유저를 Map에서 찾아 user에게 할당
            // (user에게 Cal By Reference 형식으로 참조에 의한 전달)
            UserMap.TryGetValue(sessionIndex, out user);
            return user;
        }

        bool IsFullUserCount()
        {
            // 현재 유저 수는 설정한 최대 유저 수보다 크면 안 됨. 
            return MaxUserCount <= UserMap.Count();
        }
    }
    
    // User 객체
    public class User
    {
        private UInt32 SequenceNumber = 0;
        private string SessionID;
        private int SessionIndex = -1;
        public int RoomNumber { get; private set; } = -1;
        private int userPos;
        private string UserID;

        public void Set(UInt32 sequence, string sessionID, int sessionIndex, string userID)
        {
            this.SequenceNumber = sequence;
            this.SessionID = sessionID;
            this.SessionIndex = sessionIndex;
            this.UserID = userID;
            this.userPos = -1;
        }

        public bool IsConfirm(string netSessionID)
        {
            return this.SessionID == netSessionID;
        }

        public string ID()
        {
            return this.UserID;
        }

        public void setUserPos(int pos)
        {
            this.userPos = pos; // 0 : 흑(방장), 1 : 백(일반), -1 : none
        }

        public void EnteredRoom(int roomNumber)
        {
            this.RoomNumber = roomNumber;
        }

        public void LeaveRoom()
        {
            RoomNumber = -1;
        }

        public bool IsStateLogin()
        {
            return this.SessionIndex != -1;
        }

        public bool IsStateRoom()
        {
            return this.RoomNumber != -1;
        }
    } 
}