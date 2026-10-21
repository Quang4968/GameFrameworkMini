using System;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Data
{
    // Cảm giác rung của từng event trong game, khai một chỗ cho cả game — chỉnh ở asset là mọi
    // nơi đổi theo, không scene nào được tự khai con số riêng.
    //
    // Hai thứ đi cùng nhau ở đây: cường độ (cue) và khoảng nghỉ. Tách ra là vô nghĩa, vì rung
    // mạnh mà không có khoảng nghỉ thì máy rung dính thành một khối, còn khoảng nghỉ chỉ có ý
    // nghĩa khi biết mỗi lần rung mạnh cỡ nào.
    [CreateAssetMenu(menuName = "Game/Haptic Profile Settings", fileName = "HapticProfileSettings")]
    public sealed class HapticProfileSettingsSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public HapticProfile Profile;

            [Tooltip("Cường độ rung. Soft/Light nhẹ nhất, Heavy/Rigid nặng nhất. Mức người chơi " +
                     "chọn trong Settings vẫn được cộng thêm lên trên cue này.")]
            public HapticCue Cue;

            [Tooltip("Minimum seconds between two pulses of the SAME profile. Five hits at " +
                     "lúc thì máy rung thành nhịp đều theo con số này thay vì dính thành một khối.")]
            [Min(0f)] public float MinInterval;

            [Tooltip("Tắt riêng profile này mà không phải tắt rung toàn game trong Settings.")]
            public bool Enabled;
        }

        [Tooltip("Mỗi profile khai đúng một lần. Profile không có mặt ở đây thì im lặng không rung.")]
        [SerializeField] Entry[] entries = new Entry[0];

        // Duyệt thẳng mảng: danh sách chỉ vài dòng nên Dictionary không đáng, mà lại phải dựng
        // lại mỗi lần designer sửa asset trong lúc đang Play.
        public bool TryGet(HapticProfile profile, out Entry entry)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].Profile != profile) continue;

                    entry = entries[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }
    }
}
