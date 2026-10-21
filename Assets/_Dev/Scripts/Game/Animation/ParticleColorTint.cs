using UnityEngine;

namespace Game.Game.Animation
{
    // Đặt lên prefab hiệu ứng cần đổi màu theo gameplay - an effect that must carry the colour of the piece it hits
    // vừa chém. Prefab chỉ có MỘT màu vẽ sẵn; component này nhân màu đó với màu được chỉ định.
    //
    // Nhân chứ không gán đè: start color của prefab thường là trắng HDR (độ sáng > 1) để hiệu ứng
    // ăn bloom. Gán đè màu gameplay vào đó là mất luôn phần sáng, hiệu ứng xỉn hẳn đi.
    //
    // Chỉ đụng vào start color. Color over Lifetime vẫn là của artist — đó mới là chỗ giữ nhịp
    // loé rồi tắt của hiệu ứng, nhuộm vào đấy là hỏng.
    public sealed class ParticleColorTint : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [Tooltip("Nhuộm thêm _BaseColor của material qua MaterialPropertyBlock. Bật khi hiệu ứng " +
                 "vẽ bằng material THƯỜNG - a toon material of a block, say: that kind of shader " +
                 "không đọc màu hạt nên chỉ đổi start color thì hiệu ứng ra nguyên màu vẽ sẵn. " +
                 "Tắt với shader họ Particles, chúng đã tự nhân màu hạt vào rồi.")]
        [SerializeField] bool tintRendererColor;

        ParticleSystem[] systems;
        ParticleSystemRenderer[] renderers;
        MaterialPropertyBlock propertyBlock;

        // Màu gốc của prefab, chụp lại một lần. Object lấy từ pool mang theo màu của lần dùng
        // trước, nên mỗi lần nhuộm phải tính lại từ màu gốc chứ không nhân dồn vào màu đang có.
        ParticleSystem.MinMaxGradient[] baseColors;

        void Awake()
        {
            // Gồm cả object đang tắt: prefab hiệu ứng hay tắt sẵn vài nhánh biến thể.
            systems = GetComponentsInChildren<ParticleSystem>(true);
            baseColors = new ParticleSystem.MinMaxGradient[systems.Length];

            for (int i = 0; i < systems.Length; i++) baseColors[i] = systems[i].main.startColor;

            if (!tintRendererColor) return;

            renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
            propertyBlock = new MaterialPropertyBlock();
        }

        public void Apply(Color tint)
        {
            if (systems == null) return;

            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null) continue;

                var main = systems[i].main;
                main.startColor = Tinted(baseColors[i], tint);
            }

            ApplyToRenderers(tint);
        }

        // Đường dành cho material thường: màu đi qua MaterialPropertyBlock nên mọi bản lấy từ pool
        // vẫn chung một material asset, không sinh thêm material cho từng màu.
        //
        // Both property names are set: URP shaders use _BaseColor, the
        // shader cũ dùng _Color, và material của khối có thể là bất kỳ cái nào trong hai.
        void ApplyToRenderers(Color tint)
        {
            if (renderers == null) return;

            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;

                // Index 0 only: a particle system with trails keeps its trail material at index 1,
                // and that one is meant to stay the colour the artist drew it - usually plain white.
                renderer.GetPropertyBlock(propertyBlock, 0);
                propertyBlock.SetColor(BaseColorId, tint);
                propertyBlock.SetColor(ColorId, tint);
                renderer.SetPropertyBlock(propertyBlock, 0);
            }
        }

        // Hai chế độ gradient bị bỏ qua: nhuộm chúng phải dựng Gradient mới mỗi nhát chém, mà đây
        // là đường chạy vài chục lần một giây. Hiệu ứng muốn nhuộm được thì để start color là màu
        // hằng — biến thiên theo thời gian đã có Color over Lifetime lo.
        static ParticleSystem.MinMaxGradient Tinted(ParticleSystem.MinMaxGradient source, Color tint)
        {
            switch (source.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(source.color * tint);
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(source.colorMin * tint, source.colorMax * tint);
                default:
                    return source;
            }
        }
    }
}
