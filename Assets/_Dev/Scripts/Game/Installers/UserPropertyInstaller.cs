using Game.Core.Services;
using Game.Game.Boot;
using Game.Game.Services;
using VContainer;
using VContainer.Unity;

namespace Game.Game.Installers
{
    // Phần đăng ký hệ user property dùng chung cho Home và MainGame. Gom lại vì hai scene phải khai
    // y hệt nhau và Boot scope bị unload khi đổi scene — cùng lý do với LifeSystemInstaller.
    //
    // LevelStreakPresenter KHÔNG nằm ở đây: nó cần ILevelOutcomeSource, thứ chỉ MainGame có trong
    // hierarchy. BoosterBalancePublisher cũng vậy, nó ở BoosterSystemInstaller cạnh nguồn của nó.
    public static class UserPropertyInstaller
    {
        // Game chưa có hệ chế độ chơi; mọi phiên đều là "normal" cho tới khi có.
        public const string DefaultMode = "normal";

        public static void RegisterUserProperties(this IContainerBuilder builder)
        {
            builder.Register<I_LevelStreakService, PlayerPrefsLevelStreakService>(Lifetime.Singleton);

            builder.Register<I_ActiveDayService>(
                resolver => new PlayerPrefsActiveDayService(resolver.Resolve<IClockService>()), Lifetime.Singleton);

            // Home ghi play_type trước khi đổi scene, MainGame đọc lại — nên cả hai scene đều cần.
            builder.Register<I_LevelSessionService, PlayerPrefsLevelSessionService>(Lifetime.Singleton);

            builder.RegisterEntryPoint<UserPropertyPublisher>();
        }
    }
}
