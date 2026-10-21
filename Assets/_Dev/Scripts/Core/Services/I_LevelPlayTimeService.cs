namespace Game.Core.Services
{
    // Milliseconds the player actually spent playing the current level, for the duration_* tracking params. Ads and
    // popups are not play, so whatever puts them on screen pauses this.
    public interface I_LevelPlayTimeService
    {
        long PlayedMs { get; }

        // Restarts the count from zero; pauses already held stay held.
        void Begin();

        // Counted, not a flag: every Pause needs its own Resume.
        void Pause();

        void Resume();
    }
}
