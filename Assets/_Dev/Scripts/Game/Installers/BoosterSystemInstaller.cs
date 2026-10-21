using Game.Core.Services;
using Game.Game.Boot;
using Game.Game.Data;
using Game.Game.Services;
using VContainer;
using VContainer.Unity;

namespace Game.Game.Installers
{
    // Phần đăng ký hệ booster dùng chung cho mọi scene. Gom lại vì gameplay dùng kho để tiêu, còn
    // Home (shop) sẽ dùng chính kho đó để bán — hai nơi phải khai y hệt nhau, và Boot scope bị
    // unload khi đổi scene nên không đăng ký một lần ở đó được. Cùng lý do với LifeSystemInstaller.
    public static class BoosterSystemInstaller
    {
        public static void RegisterBoosterSystem(this IContainerBuilder builder, BoosterSettingsSO settings)
        {
            // Mọi scene dùng chung PlayerPrefs key nên kho liền mạch khi đổi màn.
            builder.Register<IBoosterInventoryService>(
                _ => new PlayerPrefsBoosterInventoryService(settings), Lifetime.Singleton);

            // Số dư từng loại booster đi lên analytics từ đây, cạnh nguồn của nó — kho chỉ đăng ký
            // ở scene nào gọi hàm này, nên publisher cũng chỉ sống đúng ở đó.
            builder.RegisterEntryPoint<BoosterBalancePublisher>();

            // Nút shuffle và popup mua booster đọc giá từ chính asset này.
            if (settings != null) builder.RegisterInstance(settings);
        }
    }
}
