using System;
using UnityEngine;

namespace AstraNexus.Audio
{
    [Serializable]
    public class SoundData
    {
        public SoundType soundType;
        public AudioClip audioClip;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
        public bool loop = false;
    }

    [Serializable]
    public class MusicData
    {
        public MusicType musicType;
        public AudioClip audioClip;
        [Range(0f, 1f)]
        public float volume = 0.5f;
    }
}
