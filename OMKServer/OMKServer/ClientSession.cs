﻿using System.Collections.Concurrent;
using SuperSocket.SocketBase;

namespace OMKServer
{
    public class ClientSession : AppSession<ClientSession, OMKBinaryRequestInfo>
    {
        static public int MaxSessionCount { get; private set; } = 0;
        
        static ConcurrentBag<int> IndexPool = new ConcurrentBag<int>();

        public int SessionIndex { get; private set; } = -1;

        public static void CreateIndexPool(int maxCount)
        {
            for (int i = 0; i < maxCount; ++i)
            {
                IndexPool.Add(i);
            }

            MaxSessionCount = maxCount;
        }
        
        public static int PopIndex()
        {
            // TryTake : ConcurrentBag<T> 의 개체를 제거하고 반환 시도 (가장 마지막에 Insert 한 데이터 Pop)
            if (IndexPool.TryTake(out var result))
            {
                return result;
            }

            return -1;
        }
        
        public static void PushIndex(int index)
        {
            if (index >= 0)
            {
                IndexPool.Add(index);
            }
        }

        // Alloc : 메모리 할당
        public void AllocSessionIndex()
        {
            SessionIndex = PopIndex();
        }

        // Free : 메모리 해제
        public void FreeSessionIndex(int index)
        {
            PushIndex(index);
        }
    }
}