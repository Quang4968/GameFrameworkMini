using System;
using Game.Core.Services;
using Game.Game.Boot;
using Game.Game.Data;
using Game.Game.Services;
using VContainer;
using VContainer.Unity;

namespace Game.Game.Installers
{
    // Phần đăng ký hệ tim dùng chung cho mọi scene. Gom lại vì Home và MainGame phải khai y hệt
    // nhau — Boot scope bị unload khi đổi scene nên không đăng ký một lần ở đó được (xem "Đổi scene
    // và DI theo scene" trong ARCHITECTURE.md).
    //
    // Trước khi gom, hai installer chép tay cùng một khối và đã trôi khỏi nhau một lần. Chỉ phần
    // RIÊNG của từng scene mới ở lại installer của scene đó: Home có cổng chặn nút Play, MainGame
    // có trừ/hoàn tim theo màn chơi.
    public static class LifeSystemInstaller
    {
        // Dùng khi scene quên gán LifeSettings — vẫn chạy được chứ không văng, cùng lối xử lý với
        // coinSettings.
        const int DefaultMaxLives = 5;
        static readonly TimeSpan DefaultRefillInterval = TimeSpan.FromMinutes(30);

        public static void RegisterLifeSystem(this IContainerBuilder builder, LifeSettingsSO settings)
        {
            // Đồng hồ đi qua interface như mọi service khác: luật hồi tim nhờ vậy test được bằng
            // giờ giả, và chỉ có đúng MỘT chỗ trong game đọc DateTime.UtcNow.
            builder.Register<IClockService, SystemClockService>(Lifetime.Singleton);

            // Mọi scene dùng chung PlayerPrefs key nên số tim liền mạch khi đổi màn.
            builder.Register<ILifeService>(
                resolver =>
                {
                    var config = resolver.Resolve<GameRemoteConfig>();
                    return new PlayerPrefsLifeService(
                        resolver.Resolve<IClockService>(),
                        config != null ? config.MaxLives : settings != null ? settings.MaxLives : DefaultMaxLives,
                        config != null ? TimeSpan.FromMinutes(config.LifeRefillMinutes) :
                            settings != null ? settings.RefillInterval : DefaultRefillInterval);
                },
                Lifetime.Singleton);

            // Nhịp làm mới mỗi giây, để view không phải tự poll.
            builder.RegisterEntryPoint<LifeTicker>();

            // Quảng cáo có thưởng KHÔNG đăng ký ở đây nữa: nó đã thành một mảng của bộ SDK, và
            // scene nào cũng gọi builder.RegisterSdkSystem(sdkSettings) để lấy nó. Để lại ở đây thì
            // hai chỗ cùng đăng ký một interface, và cái thắng là cái đăng ký sau — một thứ tự ngầm
            // không ai đọc ra được từ code.

            // Popup Hết tim đọc CoinCostPerRefill từ chính asset này.
            if (settings != null) builder.RegisterInstance(settings);
        }
    }
}
