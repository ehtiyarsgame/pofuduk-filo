using PofudukFilo.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PofudukFilo.UI
{
    /// <summary>
    /// The Android back button (Escape in the Input System), design/ux/main-menu.md §8: it always does the one
    /// obvious thing for the screen on top — close a popup, pause or resume a run, leave the results, go home from a
    /// tab — and on the lobby it asks for a second press before quitting.
    /// </summary>
    public sealed partial class GameUI
    {
        /// <summary>Second back press on the lobby within this window quits the app.</summary>
        private const float QuitConfirmSeconds = 2f;
        private float _quitArmedUntil;

        private void UpdateBackButton()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            HandleBack();
        }

        private void HandleBack()
        {
            if (_settings != null && _settings.activeSelf)
            {
                _settings.SetActive(false);
                return;
            }
            if (_workshop != null && _workshop.activeSelf)
            {
                _workshop.SetActive(false);
                return;
            }

            switch (run.State)
            {
                case GameState.Playing:
                    run.Pause();
                    return;
                case GameState.Paused:
                    run.Resume();
                    return;
                case GameState.LevelUp:
                case GameState.Dead:
                    return; // a choice is required; back must not skip it
                case GameState.RunEnd:
                    AfterRunAd(run.EnterMenu);
                    return;
            }

            // Lobby: a tab screen goes home; home itself needs a confirming second press to quit.
            foreach (MetaScreen m in _metaScreens)
            {
                if (!m.Root.activeSelf) continue;
                GoHome();
                return;
            }
            if (Time.unscaledTime < _quitArmedUntil)
            {
                Application.Quit();
                return;
            }
            _quitArmedUntil = Time.unscaledTime + QuitConfirmSeconds;
            Toast("Çıkmak için tekrar bas", QuitConfirmSeconds);
        }
    }
}
