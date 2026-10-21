# Stage `smoke` - the whole flow in Play mode

Bracket Play mode as `/playtest` does: snapshot the card's play-testing PlayerPrefs, restore them
afterwards, revert the TMP atlas.

## Path

Fresh state (PlayerPrefs cleared for the card's keys) → Splash → Boot → Home → Play → level 1 with
the HowToPlay lesson → win → Win popup → next → level 2 → lose → Lose popup → revive (simulated ad) →
lose again → Home → out of lives (set lives to 0 through the debug console) → Out of lives popup →
refill → Play.

Drive it with `eval` calls on public entry points (`HomeBootstrap.RequestPlay`, the bridge's
`ReportWon/ReportLost`, popup buttons' `onClick.Invoke()`), capture each screen with
`capture_game_view --source screen` at 1080x1920, and read `console --level error` after each step.

## Done when

No error in the console along the path, every screen captured (kept under
`Library/Toolkit/minigame/smoke/` for the report), PlayerPrefs restored.
