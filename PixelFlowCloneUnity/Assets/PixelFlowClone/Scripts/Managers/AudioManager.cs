using System.Collections.Generic;
using PixelFlowClone.Core;
using PixelFlowClone.Data;
using PixelFlowClone.Entities;
using UnityEngine;

namespace PixelFlowClone.Managers
{
    /// <summary>
    /// Global SFX player (P3-19): tap, consume, win, lose, reject via an AudioSource pool.
    /// Optional Inspector clips; missing clips fall back to short procedural tones.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        public enum SfxId
        {
            Tap,
            Consume,
            Win,
            Lose,
            Reject
        }

        [Header("Clips (optional — procedural fallback if null)")]
        [SerializeField] private AudioClip _tapClip;
        [SerializeField] private AudioClip _consumeClip;
        [SerializeField] private AudioClip _winClip;
        [SerializeField] private AudioClip _loseClip;
        [SerializeField] private AudioClip _rejectClip;

        [Header("Pool")]
        [SerializeField] private int _sourcePoolSize = 8;
        [SerializeField] [Range(0f, 1f)] private float _sfxVolume = 0.85f;

        private readonly Queue<AudioSource> _available = new();
        private readonly List<AudioSource> _all = new();
        private readonly Dictionary<SfxId, AudioClip> _resolved = new();

        protected override void OnSingletonAwake()
        {
            MakePersistent();
            EnsureClips();
            EnsureSourcePool();
        }

        private void OnEnable()
        {
            GameEvents.OnBlockConsumed -= HandleBlockConsumed;
            GameEvents.OnBlockConsumed += HandleBlockConsumed;
            GameEvents.OnVictory -= HandleVictory;
            GameEvents.OnVictory += HandleVictory;
            GameEvents.OnDefeat -= HandleDefeat;
            GameEvents.OnDefeat += HandleDefeat;
            GameEvents.OnConveyorDispatchRejected -= HandleReject;
            GameEvents.OnConveyorDispatchRejected += HandleReject;
        }

        private void Start()
        {
            // Re-bind if GameEvents were cleared during bootstrap ordering.
            OnEnable();
        }

        private void OnDisable()
        {
            GameEvents.OnBlockConsumed -= HandleBlockConsumed;
            GameEvents.OnVictory -= HandleVictory;
            GameEvents.OnDefeat -= HandleDefeat;
            GameEvents.OnConveyorDispatchRejected -= HandleReject;
        }

        protected override void OnDestroy()
        {
            OnDisable();
            base.OnDestroy();
        }

        public void PlayTap() => Play(SfxId.Tap);

        public void PlayConsume() => Play(SfxId.Consume);

        public void PlayWin() => Play(SfxId.Win);

        public void PlayLose() => Play(SfxId.Lose);

        public void PlayReject() => Play(SfxId.Reject);

        public void Play(SfxId id)
        {
            if (!GameSettings.AudioEnabled)
                return;

            EnsureClips();
            if (!_resolved.TryGetValue(id, out AudioClip clip) || clip == null)
                return;

            AudioSource source = RentSource();
            if (source == null)
                return;

            source.pitch = 1f;
            source.volume = _sfxVolume;
            source.PlayOneShot(clip, _sfxVolume);
            StartCoroutine(ReturnWhenDone(source, clip.length / Mathf.Max(0.01f, source.pitch)));
        }

        private void HandleBlockConsumed(Vector3 worldPosition, ColorId color) => PlayConsume();

        private void HandleVictory() => PlayWin();

        private void HandleDefeat() => PlayLose();

        private void HandleReject(CollectorUnit unit) => PlayReject();

        private void EnsureClips()
        {
            ResolveClip(SfxId.Tap, _tapClip);
            ResolveClip(SfxId.Consume, _consumeClip);
            ResolveClip(SfxId.Win, _winClip);
            ResolveClip(SfxId.Lose, _loseClip);
            ResolveClip(SfxId.Reject, _rejectClip);
        }

        private void ResolveClip(SfxId id, AudioClip authored)
        {
            if (authored != null)
            {
                _resolved[id] = authored;
                return;
            }

            if (_resolved.TryGetValue(id, out AudioClip cached) && cached != null)
                return;

            _resolved[id] = CreateProcedural(id);
        }

        private static AudioClip CreateProcedural(SfxId id)
        {
            string key = $"PFC_Procedural_{id}";
            return id switch
            {
                SfxId.Tap => SynthesizeTone(key, 880f, 0.05f, 0.35f, decay: true),
                SfxId.Consume => SynthesizeTone(key, 520f, 0.08f, 0.4f, decay: true),
                SfxId.Win => SynthesizeArpeggio(key, new[] { 523f, 659f, 784f }, 0.09f, 0.45f),
                SfxId.Lose => SynthesizeTone(key, 160f, 0.28f, 0.5f, decay: true),
                SfxId.Reject => SynthesizeTone(key, 140f, 0.12f, 0.55f, decay: false, square: true),
                _ => SynthesizeTone(key, 440f, 0.08f, 0.3f, decay: true)
            };
        }

        private void EnsureSourcePool()
        {
            _sourcePoolSize = Mathf.Max(2, _sourcePoolSize);
            while (_all.Count < _sourcePoolSize)
            {
                var go = new GameObject($"SfxSource_{_all.Count}");
                go.transform.SetParent(transform, false);
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.loop = false;
                _all.Add(source);
                _available.Enqueue(source);
            }
        }

        private AudioSource RentSource()
        {
            EnsureSourcePool();

            if (_available.Count > 0)
                return _available.Dequeue();

            // Steal the first source if all busy.
            AudioSource busy = _all[0];
            busy.Stop();
            return busy;
        }

        private System.Collections.IEnumerator ReturnWhenDone(AudioSource source, float seconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, seconds + 0.02f));
            if (source == null)
                yield break;

            source.Stop();
            if (!_available.Contains(source))
                _available.Enqueue(source);
        }

        private static AudioClip SynthesizeTone(
            string name,
            float frequencyHz,
            float durationSeconds,
            float amplitude,
            bool decay,
            bool square = false)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.Max(64, Mathf.RoundToInt(sampleRate * durationSeconds));
            var samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float phase = t * frequencyHz * Mathf.PI * 2f;
                float wave = square
                    ? (Mathf.Sin(phase) >= 0f ? 1f : -1f) * 0.55f
                    : Mathf.Sin(phase);

                float envelope = 1f;
                if (decay)
                {
                    float norm = i / (float)(sampleCount - 1);
                    envelope = 1f - norm;
                    envelope *= envelope;
                }
                else
                {
                    // Soft attack / release for square reject.
                    float norm = i / (float)(sampleCount - 1);
                    envelope = norm < 0.1f ? norm / 0.1f : (norm > 0.85f ? (1f - norm) / 0.15f : 1f);
                }

                samples[i] = wave * amplitude * envelope;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip SynthesizeArpeggio(
            string name,
            float[] frequenciesHz,
            float noteSeconds,
            float amplitude)
        {
            const int sampleRate = 22050;
            int noteSamples = Mathf.Max(32, Mathf.RoundToInt(sampleRate * noteSeconds));
            int sampleCount = noteSamples * frequenciesHz.Length;
            var samples = new float[sampleCount];

            for (int n = 0; n < frequenciesHz.Length; n++)
            {
                float freq = frequenciesHz[n];
                int offset = n * noteSamples;
                for (int i = 0; i < noteSamples; i++)
                {
                    float t = i / (float)sampleRate;
                    float wave = Mathf.Sin(t * freq * Mathf.PI * 2f);
                    float norm = i / (float)(noteSamples - 1);
                    float envelope = (1f - norm) * (1f - norm);
                    samples[offset + i] = wave * amplitude * envelope;
                }
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
