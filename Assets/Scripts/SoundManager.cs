using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField] private AudioClip[] musicClips;
    [SerializeField] private AudioClip[] winClips;
    [SerializeField] private AudioClip[] loseClips;
    [SerializeField] private AudioClip[] bonusClips;

    [Range(0f, 1f)]
    [SerializeField] private float musicVolum = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float fxVolum = 1f;

    [SerializeField] float lowPitch = 0.95f;
    [SerializeField] float highPitch = 1.05f;

    private void Start()
    {
        PlayRandomMusic();
    }

    public AudioSource PlayClipAtPoint(AudioClip clip, Vector3 position, float volum = 1f)
    {
        if (clip != null)
        {
            GameObject go = new GameObject("SoundFX" + clip.name);
            go.transform.position = position;

            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = clip;

            float randomPitch = Random.Range(lowPitch, highPitch);
            source.pitch = randomPitch;

            source.volume = volum;

            source.Play();
            Destroy(go, clip.length);
            return source;
        }

        return null;
    }

    public AudioSource PlayRandom(AudioClip[] clips, Vector3 position, float volum = 1f)
    {
        if (clips != null)
        {
            if (clips.Length != 0)
            {
                int randomIndex = Random.Range(0, clips.Length);

                if (clips[randomIndex] != null)
                {
                    AudioSource source = PlayClipAtPoint(clips[randomIndex], position, volum);
                    return source;
                }
            }
        }

        return null;
    }

    public void PlayRandomMusic()
    {
        PlayRandom(musicClips, Vector3.zero, musicVolum);
    }

    public void PlayLoseSound()
    {
        PlayRandom(loseClips, Vector3.zero, musicVolum);
    }

    public void PlayWinSound()
    {
        PlayRandom(winClips, Vector3.zero, musicVolum);
    }

    public void PlayBonusSound()
    {
        PlayRandom(bonusClips, Vector3.zero, musicVolum);
    }
}
