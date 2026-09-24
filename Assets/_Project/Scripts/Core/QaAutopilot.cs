using System;
using System.Collections;
using System.IO;
using System.Text;
using PofudukFilo.Enemies;
using PofudukFilo.Player;
using PofudukFilo.Progression;
using PofudukFilo.Weapons;
using UnityEngine;

namespace PofudukFilo.Core
{
    /// <summary>
    /// Headless QA bot for CI (run-and-observe evidence without a device). Active only when the
    /// player is launched with <c>-qa-shots &lt;dir&gt;</c>: starts chapter 1, weaves the ship,
    /// takes the first card of every draft, revives up to <see cref="MaxRevives"/> times, and writes
    /// timed screenshots plus a telemetry log (level, kills, HP, enemies, fps) for balance passes.
    /// Optional <c>-qa-seconds &lt;n&gt;</c> sets the run length in game seconds.
    /// </summary>
    public sealed class QaAutopilot : MonoBehaviour
    {
        private const int MaxRevives = 3;
        private static readonly float[] ShotTimes = { 4f, 12f, 25f, 45f, 70f, 100f, 140f, 185f, 230f, 280f };

        private string _dir;
        private float _runSeconds = 300f;
        private readonly StringBuilder _log = new();
        private int _shotIndex;
        private int _levelUpShots;
        private int _deaths;
        private float _fpsAccum;
        private int _fpsFrames;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string dir = Arg("-qa-shots");
            if (dir == null) return;
            var go = new GameObject("QaAutopilot");
            DontDestroyOnLoad(go);
            var bot = go.AddComponent<QaAutopilot>();
            bot._dir = dir;
            if (float.TryParse(Arg("-qa-seconds"), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float s)) bot._runSeconds = s;
        }

        private void Start()
        {
            Directory.CreateDirectory(_dir);
            Application.logMessageReceived += OnLog;
            StartCoroutine(Drive());
        }

        private void OnDestroy() => Application.logMessageReceived -= OnLog;

        private void OnLog(string message, string stack, LogType type)
        {
            if (type is LogType.Error or LogType.Exception or LogType.Assert)
                Line($"[{type}] {message}\n{stack}");
        }

        private IEnumerator Drive()
        {
            yield return new WaitForSecondsRealtime(2f);
            yield return Shot("00_menu");

            RunController run = RunController.Instance;
            if (run == null)
            {
                Line("[QA] No RunController in the scene.");
                Finish();
                yield break;
            }

            run.StartRun(0);
            float gameTime = 0f, nextTelemetry = 0f;
            Line("t(s)\tlvl\tkills\thp\tenemies\tfps\tstate\tweapons");

            while (gameTime < _runSeconds)
            {
                _fpsAccum += Time.unscaledDeltaTime;
                _fpsFrames++;

                switch (run.State)
                {
                    case GameState.Playing:
                        gameTime += Time.deltaTime;
                        Weave(gameTime);
                        if (_shotIndex < ShotTimes.Length && gameTime >= ShotTimes[_shotIndex])
                            yield return Shot($"{10 + _shotIndex:00}_t{Mathf.RoundToInt(gameTime):000}");
                        break;

                    case GameState.LevelUp:
                        yield return new WaitForSecondsRealtime(0.4f);
                        if (_levelUpShots++ < 2) yield return Shot($"05_levelup_{_levelUpShots}");
                        if (run.State == GameState.LevelUp) run.ChooseUpgrade(0);
                        break;

                    case GameState.Dead:
                        _deaths++;
                        Line($"[QA] Died at {gameTime:0}s (death {_deaths}).");
                        if (_deaths == 1) yield return Shot("90_dead");
                        yield return new WaitForSecondsRealtime(0.5f);
                        if (_deaths <= MaxRevives) run.Revive(true);
                        else
                        {
                            run.GiveUp();
                            yield return new WaitForSecondsRealtime(1f);
                            yield return Shot("95_run_end");
                            Finish();
                            yield break;
                        }
                        break;

                    case GameState.RunEnd:
                        yield return new WaitForSecondsRealtime(1f);
                        yield return Shot("95_run_end");
                        Finish();
                        yield break;
                }

                if (gameTime >= nextTelemetry)
                {
                    nextTelemetry += 10f;
                    Telemetry(gameTime, run);
                }
                yield return null;
            }

            Telemetry(gameTime, run);
            Finish();
        }

        /// <summary>A lazy figure-eight: a fair stand-in for a casual player who never aims.</summary>
        private static void Weave(float t)
        {
            PlayerHealth hp = PlayerHealth.Instance;
            if (hp == null) return;
            hp.transform.position = new Vector3(3.2f * Mathf.Sin(t * 0.55f), -6.5f + 1.2f * Mathf.Sin(t * 1.1f), 0f);
        }

        private void Telemetry(float t, RunController run)
        {
            var xp = FindAnyObjectByType<XpSystem>();
            var inv = FindAnyObjectByType<WeaponInventory>();
            PlayerHealth hp = PlayerHealth.Instance;
            float fps = _fpsFrames > 0 ? _fpsFrames / Mathf.Max(0.001f, _fpsAccum) : 0f;
            _fpsAccum = 0f;
            _fpsFrames = 0;

            var weapons = new StringBuilder();
            if (inv != null)
                foreach (WeaponBehaviour w in inv.Weapons)
                    weapons.Append(w.Definition != null ? w.Definition.name : "?").Append(':').Append(w.Level).Append(' ');

            Line($"{t:0}\t{(xp != null ? xp.Level : 0)}\t{run.Kills}\t{(hp != null ? hp.CurrentHp : 0):0}/{(hp != null ? hp.MaxHp : 0):0}\t" +
                 $"{(EnemyManager.Instance != null ? EnemyManager.Instance.ActiveCount : 0)}\t{fps:0}\t{run.State}\t{weapons}");
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(_dir, name + ".png"));
            yield return null;
        }

        private void Line(string s)
        {
            _log.AppendLine(s);
            Debug.Log("[QA] " + s);
        }

        private void Finish()
        {
            File.WriteAllText(Path.Combine(_dir, "telemetry.txt"), _log.ToString());
            Application.Quit(0);
        }

        private static string Arg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
