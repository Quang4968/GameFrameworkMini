using System.Collections.Generic;
using System.Linq;
using MoodBoost.Sdk.Ads;
using MoodBoost.Sdk;
using MoodBoost.Sdk.Unity;
using Game.Core.Services;
using Game.Game.Boot;
using Game.Game.Services;
using VContainer;
using VContainer.Unity;

namespace Game.Game.Installers
{
    // Phần đăng ký SDK dùng chung cho mọi scene, gom lại đúng như LifeSystemInstaller và vì cùng
    // một lý do: Boot scope bị unload khi đổi scene nên đăng ký một lần ở đó là không đủ.
    //
    // Bản thân SDK vẫn chỉ dựng MỘT lần cho cả phiên chơi (MoodBoostSdk.GetOrCreate); cái được
    // đăng ký lại ở mỗi scene chỉ là các tham chiếu tới nó.
    //
    // Đây cũng là chỗ duy nhất quyết định IRewardedAdService lấy từ đâu — trước kia dòng đó nằm
    // trong LifeSystemInstaller.
    public static class SdkSystemInstaller
    {
        // Nhãn placement của game, gửi lên analytics để biết người chơi xem quảng cáo ở đâu.
        // Không mặc nhiên là placement khai trên dashboard của mediation — bật gửi xuống mạng
        // bằng key ads_forward_placement_to_network trên Remote Config.
        public const string ExtraLifePlacement = "extra_life";

        // Rewarded ad behind the revive button of the lose popup.
        public const string RevivePlacement = "revive";

        // Rewarded ad behind the x2 coin button of the win popup.
        public const string WinDoubleRewardPlacement = "win_x2";

        // Interstitial fired from the Win popup's claim button. How often it actually shows is
        // AdPolicySettings' call, not the call site's.
        public const string LevelCompletePlacement = "level_complete";

        // A TikTok Mini Game has no IAP, notifications or consent popup: the SDK in Assets/_Dev/Sdk carries none.
        public static void RegisterSdkSystem(this IContainerBuilder builder, SdkSettings sdkSettings)
        {
            RemoteConfigDefaults.Set(sdkSettings != null
                ? sdkSettings.RemoteConfigDefaults.Select(entry => new KeyValuePair<string, string>(entry.key, entry.value))
                : null);

            builder.RegisterMoodBoostSdk(sdkSettings);

            builder.Register<GameRemoteConfig>(Lifetime.Singleton);

            // Chưa nối mediation nào (không bật define symbol) thì IAdService vẫn tồn tại, chỉ là
            // IsReady luôn false — nút "Free" trong popup tim hiện ra ở trạng thái mờ, đúng bằng
            // cái người chơi thấy khi quảng cáo chưa tải xong. Không có nhánh UI riêng nào.
            builder.Register<IRewardedAdService>(
                resolver => new SdkRewardedAdService(resolver.Resolve<IAdService>()),
                Lifetime.Singleton);

            builder.Register<I_InterstitialAdService>(
                resolver => new SdkInterstitialAdService(resolver.Resolve<IAdService>(), LevelCompletePlacement),
                Lifetime.Singleton);

            // Ads only exist once the SDK has initialized, and in the Editor a scene is often entered without Boot.
            builder.RegisterEntryPoint<SdkSceneStartup>();
        }
    }
}
