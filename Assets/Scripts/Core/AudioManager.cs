using UnityEngine;

namespace FPSGame
{
    public enum SoundType
    {
        PistolShot,
        RifleShot,
        EnemyShot,
        Reload,
        Empty,
        Hit,
        EnemyDeath,
        PlayerHurt,
        Pickup,
        Click,
        Win,
        Lose,
        Footstep,
        EnemyGrowl
    }

    /// <summary>
    /// Builds every sound effect in code with AudioClip.Create (no audio files) and plays
    /// them through a small pool of AudioSources. Master volume is AudioListener.volume,
    /// which SaveManager sets.
    /// </summary>
    [DefaultExecutionOrder(-700)]
    public class AudioManager : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private const int PoolSize = 12;
        private const float TwoPi = Mathf.PI * 2f;

        public static AudioManager Instance { get; private set; }

        private AudioClip[] clips;
        private AudioSource[] sources;
        private AudioSource footstepsSource;
        private int nextSource;
        private System.Random noiseRandom;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            noiseRandom = new System.Random(1337);
            CreateSources();
            GenerateClips();
            ConfigureFootsteps();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void PlaySound(SoundType type, float volume = 1f)
        {
            if (Instance != null) Instance.Play(type, volume, false, Vector3.zero);
        }

        public static void PlaySoundAt(SoundType type, Vector3 position, float volume = 1f)
        {
            if (Instance != null) Instance.Play(type, volume, true, position);
        }

        public static void SetFootsteps(bool moving, float runAmount)
        {
            if (Instance == null || Instance.footstepsSource == null) return;
            AudioSource source = Instance.footstepsSource;
            if (!moving)
            {
                if (source.isPlaying) source.Pause();
                return;
            }

            source.pitch = Mathf.Lerp(0.92f, 1.28f, Mathf.Clamp01(runAmount));
            source.volume = Mathf.Lerp(0.72f, 0.92f, Mathf.Clamp01(runAmount));
            if (!source.isPlaying) source.UnPause();
            if (!source.isPlaying) source.Play();
        }

        private void Play(SoundType type, float volume, bool spatial, Vector3 position)
        {
            int index = (int)type;
            if (clips == null || index < 0 || index >= clips.Length) return;
            AudioClip clip = clips[index];
            if (clip == null) return;

            AudioSource source = sources[nextSource];
            nextSource = (nextSource + 1) % sources.Length;

            source.transform.position = position;
            source.spatialBlend = spatial ? 1f : 0f;
            source.pitch = Random.Range(0.94f, 1.06f);
            source.volume = volume;
            source.clip = clip;
            source.Play();
        }

        private void CreateSources()
        {
            sources = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                GameObject go = new GameObject("Sfx" + i);
                go.transform.SetParent(transform, false);
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 3f;
                source.maxDistance = 60f;
                source.dopplerLevel = 0f;
                sources[i] = source;
            }
        }

        private void ConfigureFootsteps()
        {
            GameObject go = new GameObject("FootstepsLoop");
            go.transform.SetParent(transform, false);
            footstepsSource = go.AddComponent<AudioSource>();
            footstepsSource.playOnAwake = false;
            footstepsSource.loop = true;
            footstepsSource.spatialBlend = 0f;
            footstepsSource.clip = clips[(int)SoundType.Footstep];
        }

        // ---------------- Procedural synthesis ----------------

        private void GenerateClips()
        {
            clips = new AudioClip[System.Enum.GetValues(typeof(SoundType)).Length];

            clips[(int)SoundType.PistolShot] = CreateClip("PistolShot", 0.22f,
                t => Noise() * Decay(t, 22f) * 0.75f + Sine(t, 120f) * Decay(t, 28f) * 0.6f);

            clips[(int)SoundType.RifleShot] = CreateClip("RifleShot", 0.14f,
                t => Noise() * Decay(t, 32f) * 0.7f + Sine(t, 180f) * Decay(t, 40f) * 0.4f);

            clips[(int)SoundType.EnemyShot] = CreateClip("EnemyShot", 0.18f,
                t => Noise() * Decay(t, 26f) * 0.55f + Square(t, 90f) * Decay(t, 35f) * 0.25f);

            clips[(int)SoundType.Reload] = CreateClip("Reload", 0.5f,
                t => Click(t, 0.02f, 1600f) + Click(t, 0.3f, 2200f) + Sine(t, 70f) * Window(t, 0.28f, 0.36f) * 0.3f);

            clips[(int)SoundType.Empty] = CreateClip("Empty", 0.06f,
                t => Click(t, 0f, 2600f));

            clips[(int)SoundType.Hit] = CreateClip("Hit", 0.08f,
                t => Sine(t, 1400f) * Decay(t, 45f) * 0.5f);

            clips[(int)SoundType.EnemyDeath] = CreateClip("EnemyDeath", 0.55f,
                t => Sweep(t, 0.55f, 420f, 70f) * Decay(t, 5f) * 0.6f + Noise() * Decay(t, 12f) * 0.15f);

            clips[(int)SoundType.PlayerHurt] = CreateClip("PlayerHurt", 0.25f,
                t => Square(t, 140f) * Decay(t, 12f) * 0.35f + Noise() * Decay(t, 20f) * 0.2f);

            clips[(int)SoundType.Pickup] = CreateClip("Pickup", 0.22f,
                t => Sweep(t, 0.22f, 600f, 1300f) * Decay(t, 8f) * 0.45f);

            clips[(int)SoundType.Click] = CreateClip("Click", 0.05f,
                t => Sine(t, 900f) * Decay(t, 60f) * 0.4f);

            clips[(int)SoundType.Win] = CreateClip("Win", 0.75f,
                t => Arpeggio(t, 0.18f, 523.25f, 659.25f, 783.99f, 1046.5f));

            clips[(int)SoundType.Lose] = CreateClip("Lose", 0.9f,
                t => Arpeggio(t, 0.25f, 392f, 329.63f, 261.63f, 196f));

            clips[(int)SoundType.Footstep] = Resources.Load<AudioClip>("Audio/FootstepsWalkingBoots");
            if (clips[(int)SoundType.Footstep] == null)
            {
                clips[(int)SoundType.Footstep] = CreateClip("FootstepFallback", 0.16f,
                    t => Noise() * Decay(t, 20f) * 0.52f + Sine(t, 68f) * Decay(t, 15f) * 0.62f);
            }

            clips[(int)SoundType.EnemyGrowl] = CreateClip("EnemyGrowl", 1.15f,
                t => (Sine(t, 52f + Mathf.Sin(t * 7f) * 7f) * 0.52f +
                      Square(t, 26f) * 0.12f + Noise() * 0.13f) *
                     Mathf.Sin(Mathf.Clamp01(t / 0.09f) * Mathf.PI * 0.5f) * Decay(t, 0.75f));
        }

        private AudioClip CreateClip(string clipName, float duration, System.Func<float, float> sampler)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            float[] data = new float[sampleCount];
            int fadeSamples = Mathf.Min(sampleCount, SampleRate / 200);
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float sample = Mathf.Clamp(sampler(t), -1f, 1f);
                int remaining = sampleCount - i;
                if (remaining < fadeSamples) sample *= (float)remaining / fadeSamples;
                data[i] = sample;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private float Noise()
        {
            return (float)(noiseRandom.NextDouble() * 2.0 - 1.0);
        }

        private static float Sine(float t, float frequency)
        {
            return Mathf.Sin(TwoPi * frequency * t);
        }

        private static float Square(float t, float frequency)
        {
            return Mathf.Sin(TwoPi * frequency * t) >= 0f ? 1f : -1f;
        }

        private static float Decay(float t, float rate)
        {
            return Mathf.Exp(-rate * t);
        }

        private static float Window(float t, float start, float end)
        {
            return t >= start && t < end ? 1f : 0f;
        }

        private static float Sweep(float t, float duration, float startFrequency, float endFrequency)
        {
            float phase = startFrequency * t + (endFrequency - startFrequency) * t * t / (2f * duration);
            return Mathf.Sin(TwoPi * phase);
        }

        private static float Click(float t, float start, float frequency)
        {
            float local = t - start;
            if (local < 0f || local > 0.04f) return 0f;
            return Square(local, frequency) * Decay(local, 110f) * 0.55f;
        }

        private static float Arpeggio(float t, float noteLength, float f0, float f1, float f2, float f3)
        {
            int note = Mathf.Min(3, (int)(t / noteLength));
            float frequency = note == 0 ? f0 : note == 1 ? f1 : note == 2 ? f2 : f3;
            float local = t - note * noteLength;
            return (Sine(t, frequency) * 0.7f + Sine(t, frequency * 2f) * 0.15f) * Decay(local, 5f) * 0.5f;
        }
    }
}
