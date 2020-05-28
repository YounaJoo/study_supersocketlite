using System.Collections.Concurrent;
using SuperSocket.SocketBase;

namespace ChatServer
{
    // AppServer --> AppSession
    public class ClientSession : AppSession<ClientSession, EFBinaryRequestInfo>
    {
        static public int MaxSessionCount { get; private set; } = 0;
        
        // ConcurrentBag Class : 스레드로부터 안전한 정렬되지 않은 개체 컬렉션을 나타냄.
        // 한 스레드에서 항목의 추가와 제거를 모두 수행할 때 추가하고 제거하는 속도가 빠르다는 장점을 갖고 있다. (중복 허용)
        // 이는 모든 공용 및 보호된 멤버는 스레드로부터 안전하며 여러 스레드에서 동시에 사용할 수 있다. (동기화 해야할 수도 있음)
        // Session에 대한 IndexPool 
        static ConcurrentBag<int> IndexPool = new ConcurrentBag<int>();

        public int SessionIndex { get; private set; } = -1;

        // Index Pool 생성
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