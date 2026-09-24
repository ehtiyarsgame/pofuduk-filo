using System;
using PofudukFilo.Bullets;
using PofudukFilo.Core;
using PofudukFilo.Enemies;
using PofudukFilo.Player;
using PofudukFilo.Progression;
using PofudukFilo.Weapons;
using UnityEngine;

namespace PofudukFilo.Audio
{
    /// <summary>
    /// Plays the synthesized sound set. Rules from art-bible §5.1: pitch varies ±8 % so repeats
    /// don't grate, and each sound plays at most 4 times per frame however many enemies pop.
    /// Listens to gameplay events; nothing else needs to know audio exists.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private RunController run;
        [SerializeField] private WeaponInventory inventory;
        [SerializeField] private PickupSystem pickups;
        [SerializeField] private WaveDirector waveDirector;
        [SerializeField] private int voices = 12;
        [SerializeField] private int maxPerFramePerSound = 4;
        [SerializeField, Range(0f, 0.2f)] private float pitchVariance = 0.08f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;

        private AudioClip[] _clips;
        private AudioSource[] _voices;
        private AudioSource _music;
        private int _nextVoice;
        private int[] _playedThisFrame;
        private int _frame = -1;

        private void Awake()
        {
            Instance = this;

            var ids = (SfxId[])Enum.GetValues(typeof(SfxId));
            _clips = new AudioClip[ids.Length];
            _playedThisFrame = new int[ids.Length];
            foreach (SfxId id in ids) _clips[(int)id] = MakeClip(id.ToString(), SfxSynth.Build(id));

            _voices = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                _voices[i] = gameObject.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
            }

            _music = gameObject.AddComponent<AudioSource>();
            _music.clip = MakeClip("MusicLoop", SfxSynth.BuildMusicLoop());
            _music.loop = true;
            _music.volume = musicVolume;
            _music.ignoreListenerPause = true;

            GameSettings.Changed += ApplySettings;
        }

        private void Start()
        {
            EnemyManager.Instance.EnemyKilled += e => Play(e.IsElite ? SfxId.BigPop : SfxId.Pop);
            if (PlayerHealth.Instance != null) PlayerHealth.Instance.Damaged += _ => Play(SfxId.Hurt, 0f);
            if (BulletSystem.Instance != null) BulletSystem.Instance.Grazed += _ => Play(SfxId.Graze);
            if (pickups != null) pickups.Collected += OnCollected;
            if (inventory != null)
            {
                inventory.WeaponEvolved += (_, _) => Play(SfxId.Evolution, 0f);
                inventory.WeaponFused += _ => Play(SfxId.Evolution, 0f);
            }
            if (waveDirector != null)
            {
                waveDirector.BossSpawned += _ => Play(SfxId.BossWarning, 0f);
                waveDirector.BossDefeated += (_, _) => Play(SfxId.BigPop, 0f);
            }
            if (run != null) run.LevelUpOffered += (_, _, _) => Play(SfxId.LevelUp, 0f);

            ApplySettings();
        }

        private void OnDestroy()
        {
            GameSettings.Changed -= ApplySettings;
            if (Instance == this) Instance = null;
        }

        private void OnCollected(PickupKind kind, int value, Vector2 position)
        {
            switch (kind)
            {
                case PickupKind.Gold: Play(SfxId.Coin); break;
                case PickupKind.Heart: Play(SfxId.Heal, 0f); break;
                case PickupKind.Bomb: Play(SfxId.BigPop, 0f); break;
                case PickupKind.Magnet: Play(SfxId.LevelUp, 0f); break;
                default: Play(SfxId.Gem); break;
            }
        }

        /// <param name="variance">Pitch variance; pass 0 for musical cues that must stay in tune.</param>
        public void Play(SfxId id, float variance = -1f)
        {
            if (!GameSettings.Sfx) return;

            if (_frame != Time.frameCount)
            {
                _frame = Time.frameCount;
                Array.Clear(_playedThisFrame, 0, _playedThisFrame.Length);
            }
            if (_playedThisFrame[(int)id]++ >= maxPerFramePerSound) return;

            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            float v = variance < 0f ? pitchVariance : variance;
            voice.pitch = 1f + UnityEngine.Random.Range(-v, v);
            voice.PlayOneShot(_clips[(int)id], sfxVolume);
        }

        private void ApplySettings()
        {
            if (GameSettings.Music && !_music.isPlaying) _music.Play();
            else if (!GameSettings.Music && _music.isPlaying) _music.Stop();
        }

        private static AudioClip MakeClip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SfxSynth.SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
