using UnityEngine;

namespace Picomeow.Apartment
{
    // 公寓装修数据层：房间装修状态与奖励（视觉放置由 UIManager 负责）
    public static class DecorationManager
    {
        public static bool IsDecorated(int roomId)
        {
            return SaveSystem.IsRoomDecorated(roomId);
        }

        // 完成一个房间的装修：记录状态并发放 1 颗星星
        public static void Decorate(int roomId)
        {
            if (SaveSystem.IsRoomDecorated(roomId)) return;
            SaveSystem.MarkRoomDecorated(roomId);
            if (Economy.Instance != null) Economy.Instance.AddStars(1);
        }

        // 当前应装修的房间：第一个尚未装修的（默认 1、2）
        public static int CurrentRoom()
        {
            if (!IsDecorated(1)) return 1;
            if (!IsDecorated(2)) return 2;
            return 1;
        }
    }
}
