using Game.Core.Services;
using UnityEngine;
using VContainer.Unity;

namespace Game.Game.Boot
{
    // Nhịp tim của scene: gọi ILifeService.Refresh() mỗi giây để bắt lúc tim hồi xong trong khi
    // người chơi đứng yên.
    //
    // One ticker instead of each life view refreshing in Update: views only listen to LivesChanged
    // and draw, and the domain is recomputed once a second rather than every frame.
    //
    // Dùng unscaledDeltaTime vì tim hồi theo thời gian thật, không liên quan tới Time.timeScale
    // (popup có thể dừng game).
    public sealed class LifeTicker : ITickable
    {
        const float IntervalSeconds = 1f;

        readonly ILifeService lives;

        float untilNextTick;

        public LifeTicker(ILifeService lives)
        {
            this.lives = lives;
        }

        public void Tick()
        {
            untilNextTick -= Time.unscaledDeltaTime;
            if (untilNextTick > 0f) return;

            untilNextTick = IntervalSeconds;
            lives.Refresh();
        }
    }
}
