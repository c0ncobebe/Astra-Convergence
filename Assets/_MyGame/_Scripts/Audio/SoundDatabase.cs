using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AstraNexus.Audio
{
    [CreateAssetMenu(fileName = "SoundDatabase", menuName = "Astra Nexus/Audio/Sound Database")]
    public class SoundDatabase : ScriptableObject
    {
        [Header("Sound Effects")]
        [SerializeField] private List<SoundData> soundEffects = new List<SoundData>();

        [Header("Music Tracks")]
        [SerializeField] private List<MusicData> menuMusicTracks = new List<MusicData>();
        [SerializeField] private List<MusicData> ingameMusicTracks = new List<MusicData>();

        private Dictionary<SoundType, List<SoundData>> soundDictionary;
        private Dictionary<MusicType, MusicData> musicDictionary;

        public void Initialize()
        {
            // Build sound effects dictionary
            soundDictionary = new Dictionary<SoundType, List<SoundData>>();
            foreach (var sound in soundEffects)
            {
                if (sound.soundType != SoundType.None)
                {
                    if (!soundDictionary.ContainsKey(sound.soundType))
                    {
                        soundDictionary.Add(sound.soundType, new List<SoundData>());
                    }
                    soundDictionary[sound.soundType].Add(sound);
                }
            }

            // Build music dictionary from both menu and ingame tracks
            musicDictionary = new Dictionary<MusicType, MusicData>();
            var allMusic = menuMusicTracks.Concat(ingameMusicTracks);
            foreach (var music in allMusic)
            {
                if (music.musicType != MusicType.None && !musicDictionary.ContainsKey(music.musicType))
                {
                    musicDictionary.Add(music.musicType, music);
                }
            }
        }

        public SoundData GetSound(SoundType soundType)
        {
            if (soundDictionary == null)
            {
                Initialize();
            }

            if (soundDictionary.TryGetValue(soundType, out List<SoundData> soundList) && soundList.Count > 0)
            {
                // Return first sound for backward compatibility
                return soundList[0];
            }

            Debug.LogWarning($"Sound {soundType} not found in database!");
            return null;
        }

        public SoundData GetRandomSound(SoundType soundType)
        {
            if (soundDictionary == null)
            {
                Initialize();
            }

            if (soundDictionary.TryGetValue(soundType, out List<SoundData> soundList) && soundList.Count > 0)
            {
                // Randomly select one sound from the list
                int randomIndex = Random.Range(0, soundList.Count);
                return soundList[randomIndex];
            }

            Debug.LogWarning($"Sound {soundType} not found in database!");
            return null;
        }

        public MusicData GetMusic(MusicType musicType)
        {
            if (musicDictionary == null)
            {
                Initialize();
            }

            if (musicDictionary.TryGetValue(musicType, out MusicData musicData))
            {
                return musicData;
            }

            Debug.LogWarning($"Music {musicType} not found in database!");
            return null;
        }

        public MusicData GetRandomMenuMusic()
        {
            if (menuMusicTracks.Count == 0)
            {
                Debug.LogWarning("No menu music tracks available!");
                return null;
            }

            int randomIndex = Random.Range(0, menuMusicTracks.Count);
            return menuMusicTracks[randomIndex];
        }

        public MusicData GetRandomIngameMusic()
        {
            if (ingameMusicTracks.Count == 0)
            {
                Debug.LogWarning("No ingame music tracks available!");
                return null;
            }

            int randomIndex = Random.Range(0, ingameMusicTracks.Count);
            return ingameMusicTracks[randomIndex];
        }

        public List<MusicData> GetAllMenuMusic()
        {
            return new List<MusicData>(menuMusicTracks);
        }

        public List<MusicData> GetAllIngameMusic()
        {
            return new List<MusicData>(ingameMusicTracks);
        }
    }
}
