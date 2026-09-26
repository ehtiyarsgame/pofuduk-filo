using PofudukFilo.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PofudukFilo.UI
{
    /// <summary>
    /// First-run coach marks (design/ux/tutorial.md): three one-line hints during the very first run — how to move,
    /// what a leak costs, and the Sugar Bomb button — each shown once, then never again on this device.
    /// </summary>
    public sealed partial class GameUI
    {
        private enum TutorialStep { Move, Leaks, Bomb, Done }

        /// <summary>Seconds of finger-down movement that count as "has learned to steer".</summary>
        private const float MoveLearnedSeconds = 1.5f;
        /// <summary>How long the leak hint stays up.</summary>
        private const float LeakHintSeconds = 5f;

        private Text _tutorialText;
        private TutorialStep _tutorialStep = TutorialStep.Done;
        private float _tutorialTimer;

        private void BuildTutorial(Transform hud)
        {
            _tutorialText = _ui.Label(hud, "", 50, Palette.Cream);
            UIFactory.Place(_tutorialText, 0.06f, 0.32f, 0.94f, 0.4f); // above the ship, below the swarm
            _tutorialText.resizeTextForBestFit = true;
            _tutorialText.resizeTextMinSize = 28;
            _tutorialText.resizeTextMaxSize = 50;
            _tutorialText.raycastTarget = false;
            _tutorialText.gameObject.SetActive(false);
            run.StateChanged += s =>
            {
                // A fresh run on a device that never finished the coach marks starts them from the top.
                if (s == GameState.Playing && _tutorialStep == TutorialStep.Done && !GameSettings.TutorialDone)
                    SetTutorialStep(TutorialStep.Move);
            };
            if (_rush != null) _rush.RushReady += () =>
            {
                if (_tutorialStep == TutorialStep.Bomb) ShowTutorialText("Şeker Bombası hazır! Sol alttaki ŞEKER! tuşuna bas");
            };
        }

        private void SetTutorialStep(TutorialStep step)
        {
            _tutorialStep = step;
            _tutorialTimer = 0f;
            switch (step)
            {
                case TutorialStep.Move:
                    ShowTutorialText("Parmağını ekranda sürükle — gemi seni takip eder");
                    break;
                case TutorialStep.Leaks:
                    ShowTutorialText("Aşağıdan kaçan canavarlar can yakar — hepsini vur!");
                    break;
                case TutorialStep.Bomb:
                    _tutorialText.gameObject.SetActive(false); // waits for the first full sugar meter
                    break;
                case TutorialStep.Done:
                    _tutorialText.gameObject.SetActive(false);
                    GameSettings.TutorialDone = true;
                    break;
            }
        }

        private void ShowTutorialText(string text)
        {
            _tutorialText.text = Loc.T(text);
            _tutorialText.gameObject.SetActive(true);
            Feel.Juice.PopIn(_tutorialText.transform);
        }

        private void UpdateTutorial()
        {
            if (_tutorialText == null || _tutorialStep == TutorialStep.Done) return;
            if (run.State != GameState.Playing)
            {
                // Hidden over menus and cards. Back in the lobby: a player who got past the first two hints has
                // learned the basics (the bomb hint is a bonus); anyone else sees them again next run.
                if (_tutorialText.gameObject.activeSelf && run.State != GameState.Paused) _tutorialText.gameObject.SetActive(false);
                if (run.State == GameState.MainMenu)
                {
                    if (_tutorialStep == TutorialStep.Bomb) SetTutorialStep(TutorialStep.Done);
                    else _tutorialStep = TutorialStep.Done;
                }
                return;
            }

            float dt = Time.unscaledDeltaTime;
            // Gentle breathing so the hint reads as a hint, not as a HUD label.
            float a = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 3f);
            Color c = _tutorialText.color;
            _tutorialText.color = new Color(c.r, c.g, c.b, a);

            switch (_tutorialStep)
            {
                case TutorialStep.Move:
                    Pointer pointer = Pointer.current;
                    if (pointer != null && pointer.press.isPressed) _tutorialTimer += dt;
                    if (_tutorialTimer >= MoveLearnedSeconds) SetTutorialStep(TutorialStep.Leaks);
                    break;
                case TutorialStep.Leaks:
                    _tutorialTimer += dt;
                    if (_tutorialTimer >= LeakHintSeconds) SetTutorialStep(TutorialStep.Bomb);
                    break;
                case TutorialStep.Bomb:
                    // Shown by RushReady; finished once the bomb has been used.
                    if (_rush != null && _rush.Active) SetTutorialStep(TutorialStep.Done);
                    break;
            }
        }
    }
}
