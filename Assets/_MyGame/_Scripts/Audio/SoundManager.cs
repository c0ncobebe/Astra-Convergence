using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AstraNexus.Audio
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private SoundDatabase soundDatabase;
        [SerializeField] private int audioSourcePoolSize = 10;
        [SerializeField] private float crossFadeDuration = 1.5f;

        [Header("Volume Settings")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;

        // Audio Source Pools
        private List<AudioSource> sfxAudioSourcePool;
        private AudioSource musicAudioSource1;
        private AudioSource musicAudioSource2;
        private AudioSource currentMusicSource;
        private AudioSource nextMusicSource;

        // Current Music State
        private MusicData currentMusic;
        private bool isCrossFading = false;
        private List<MusicData> currentPlaylist;
        private int currentPlaylistIndex = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            
            if (soundDatabase != null)
            {
                soundDatabase.Initialize();
            }
            else
            {
                Debug.LogError("SoundDatabase is not assigned to SoundManager!");
            }
        }

        private void InitializeAudioSources()
        {
            // Initialize SFX pool
            sfxAudioSourcePool = new List<AudioSource>();
            for (int i = 0; i < audioSourcePoolSize; i++)
            {
                AudioSource audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                sfxAudioSourcePool.Add(audioSource);
            }

            // Initialize Music sources for cross-fading
            musicAudioSource1 = gameObject.AddComponent<AudioSource>();
            musicAudioSource1.playOnAwake = false;
            musicAudioSource1.loop = true;

            musicAudioSource2 = gameObject.AddComponent<AudioSource>();
            musicAudioSource2.playOnAwake = false;
            musicAudioSource2.loop = true;

            currentMusicSource = musicAudioSource1;
            nextMusicSource = musicAudioSource2;
        }

        #region Sound Effects

        /// <summary>
        /// Play a sound effect by enum type
        /// </summary>
        public void PlaySound(SoundType soundType)
        {
            if (soundDatabase == null) return;

            SoundData soundData = soundDatabase.GetSound(soundType);
            if (soundData == null || soundData.audioClip == null) return;

            AudioSource availableSource = GetAvailableAudioSource();
            if (availableSource != null)
            {
                availableSource.clip = soundData.audioClip;
                availableSource.volume = soundData.volume * sfxVolume * masterVolume;
                availableSource.pitch = soundData.pitch;
                availableSource.loop = soundData.loop;
                availableSource.Play();
            }
        }

        /// <summary>
        /// Play a sound effect with custom volume
        /// </summary>
        public void PlaySound(SoundType soundType, float volumeMultiplier)
        {
            if (soundDatabase == null) return;

            SoundData soundData = soundDatabase.GetSound(soundType);
            if (soundData == null || soundData.audioClip == null) return;

            AudioSource availableSource = GetAvailableAudioSource();
            if (availableSource != null)
            {
                availableSource.clip = soundData.audioClip;
                availableSource.volume = soundData.volume * volumeMultiplier * sfxVolume * masterVolume;
                availableSource.pitch = soundData.pitch;
                availableSource.loop = soundData.loop;
                availableSource.Play();
            }
        }

        /// <summary>
        /// Play a sound effect at a specific position (3D sound)
        /// </summary>
        public void PlaySoundAtPosition(SoundType soundType, Vector3 position)
        {
            if (soundDatabase == null) return;

            SoundData soundData = soundDatabase.GetSound(soundType);
            if (soundData == null || soundData.audioClip == null) return;

            AudioSource.PlayClipAtPoint(soundData.audioClip, position, soundData.volume * sfxVolume * masterVolume);
        }

        private AudioSource GetAvailableAudioSource()
        {
            // Find an available audio source that's not playing
            foreach (var source in sfxAudioSourcePool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            // If all are busy, return the first one (will interrupt it)
            return sfxAudioSourcePool[0];
        }

        #endregion

        #region Background Music

        /// <summary>
        /// Play a specific music track with cross-fade
        /// </summary>
        public void PlayMusic(MusicType musicType, bool useCrossFade = true)
        {
            if (soundDatabase == null) return;

            MusicData musicData = soundDatabase.GetMusic(musicType);
            if (musicData == null || musicData.audioClip == null) return;

            if (useCrossFade && currentMusicSource.isPlaying)
            {
                StartCoroutine(CrossFadeMusic(musicData));
            }
            else
            {
                PlayMusicImmediate(musicData);
            }
        }

        /// <summary>
        /// Play random menu music with cross-fade
        /// </summary>
        public void PlayRandomMenuMusic(bool useCrossFade = true)
        {
            if (soundDatabase == null) return;

            MusicData musicData = soundDatabase.GetRandomMenuMusic();
            if (musicData == null) return;

            currentPlaylist = soundDatabase.GetAllMenuMusic();
            currentPlaylistIndex = currentPlaylist.IndexOf(musicData);

            if (useCrossFade && currentMusicSource.isPlaying)
            {
                StartCoroutine(CrossFadeMusic(musicData));
            }
            else
            {
                PlayMusicImmediate(musicData);
            }
        }

        /// <summary>
        /// Play random ingame music with cross-fade
        /// </summary>
        public void PlayRandomIngameMusic(bool useCrossFade = true)
        {
            if (soundDatabase == null) return;

            MusicData musicData = soundDatabase.GetRandomIngameMusic();
            if (musicData == null) return;

            currentPlaylist = soundDatabase.GetAllIngameMusic();
            currentPlaylistIndex = currentPlaylist.IndexOf(musicData);

            if (useCrossFade && currentMusicSource.isPlaying)
            {
                StartCoroutine(CrossFadeMusic(musicData));
            }
            else
            {
                PlayMusicImmediate(musicData);
            }
        }

        /// <summary>
        /// Start playing menu music playlist in shuffle mode
        /// </summary>
        public void StartMenuMusicPlaylist()
        {
            if (soundDatabase == null) return;

            currentPlaylist = soundDatabase.GetAllMenuMusic();
            ShufflePlaylist();
            
            if (currentPlaylist.Count > 0)
            {
                currentPlaylistIndex = 0;
                PlayMusicImmediate(currentPlaylist[0]);
                StartCoroutine(PlaylistManager());
            }
        }

        /// <summary>
        /// Start playing ingame music playlist in shuffle mode
        /// </summary>
        public void StartIngameMusicPlaylist()
        {
            if (soundDatabase == null) return;

            currentPlaylist = soundDatabase.GetAllIngameMusic();
            ShufflePlaylist();
            
            if (currentPlaylist.Count > 0)
            {
                currentPlaylistIndex = 0;
                PlayMusicImmediate(currentPlaylist[0]);
                StartCoroutine(PlaylistManager());
            }
        }

        private void ShufflePlaylist()
        {
            if (currentPlaylist == null || currentPlaylist.Count <= 1) return;

            for (int i = 0; i < currentPlaylist.Count; i++)
            {
                MusicData temp = currentPlaylist[i];
                int randomIndex = Random.Range(i, currentPlaylist.Count);
                currentPlaylist[i] = currentPlaylist[randomIndex];
                currentPlaylist[randomIndex] = temp;
            }
        }

        private IEnumerator PlaylistManager()
        {
            while (currentPlaylist != null && currentPlaylist.Count > 0)
            {
                // Wait for current track to finish
                while (currentMusicSource.isPlaying)
                {
                    yield return new WaitForSeconds(0.5f);
                }

                // Move to next track
                currentPlaylistIndex = (currentPlaylistIndex + 1) % currentPlaylist.Count;
                
                // Reshuffle if we've completed the playlist
                if (currentPlaylistIndex == 0)
                {
                    ShufflePlaylist();
                }

                StartCoroutine(CrossFadeMusic(currentPlaylist[currentPlaylistIndex]));
            }
        }

        private void PlayMusicImmediate(MusicData musicData)
        {
            currentMusicSource.Stop();
            currentMusicSource.clip = musicData.audioClip;
            currentMusicSource.volume = musicData.volume * musicVolume * masterVolume;
            currentMusicSource.Play();
            currentMusic = musicData;
        }

        private IEnumerator CrossFadeMusic(MusicData newMusicData)
        {
            if (isCrossFading) yield break;

            isCrossFading = true;

            // Setup next music source
            nextMusicSource.clip = newMusicData.audioClip;
            nextMusicSource.volume = 0f;
            nextMusicSource.Play();

            float elapsed = 0f;
            float startVolume = currentMusicSource.volume;
            float targetVolume = newMusicData.volume * musicVolume * masterVolume;

            // Cross-fade
            while (elapsed < crossFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / crossFadeDuration;

                currentMusicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                nextMusicSource.volume = Mathf.Lerp(0f, targetVolume, t);

                yield return null;
            }

            // Finalize
            currentMusicSource.Stop();
            currentMusicSource.volume = targetVolume;

            // Swap sources
            AudioSource temp = currentMusicSource;
            currentMusicSource = nextMusicSource;
            nextMusicSource = temp;

            currentMusic = newMusicData;
            isCrossFading = false;
        }

        /// <summary>
        /// Stop all music
        /// </summary>
        public void StopMusic(bool useFadeOut = true)
        {
            if (useFadeOut)
            {
                StartCoroutine(FadeOutMusic());
            }
            else
            {
                currentMusicSource.Stop();
                nextMusicSource.Stop();
            }
        }

        private IEnumerator FadeOutMusic()
        {
            float startVolume = currentMusicSource.volume;
            float elapsed = 0f;

            while (elapsed < crossFadeDuration)
            {
                elapsed += Time.deltaTime;
                currentMusicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / crossFadeDuration);
                yield return null;
            }

            currentMusicSource.Stop();
            currentMusicSource.volume = startVolume;
        }

        /// <summary>
        /// Pause music
        /// </summary>
        public void PauseMusic()
        {
            currentMusicSource.Pause();
        }

        /// <summary>
        /// Resume music
        /// </summary>
        public void ResumeMusic()
        {
            currentMusicSource.UnPause();
        }

        #endregion

        #region Volume Controls

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateMusicVolume();
        }

        private void UpdateAllVolumes()
        {
            UpdateMusicVolume();
        }

        private void UpdateMusicVolume()
        {
            if (currentMusic != null)
            {
                currentMusicSource.volume = currentMusic.volume * musicVolume * masterVolume;
            }
        }

        public float GetMasterVolume() => masterVolume;
        public float GetSFXVolume() => sfxVolume;
        public float GetMusicVolume() => musicVolume;

        #endregion
    }
}
