using UnityEngine;
using Unity.Netcode;

namespace Arixon.Gameplay
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("SFX Library")]
        [SerializeField] private AudioClip _kickLightClip;
        [SerializeField] private AudioClip _kickHeavyClip;
        [SerializeField] private AudioClip _ballBounceClip;
        [SerializeField] private AudioClip _goalScoreClip;

        private AudioSource _sfxSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.spatialBlend = 0f; // 2D Ses (Herkes duysun)
        }

        public void PlayKickLight()
        {
            if (_kickLightClip != null) _sfxSource.PlayOneShot(_kickLightClip, 0.6f);
        }

        public void PlayKickHeavy()
        {
            if (_kickHeavyClip != null) _sfxSource.PlayOneShot(_kickHeavyClip, 1f);
        }

        public void PlayBallBounce(float speed)
        {
            if (_ballBounceClip != null && speed > 2f)
            {
                float volume = Mathf.Clamp01(speed / 15f);
                _sfxSource.PlayOneShot(_ballBounceClip, volume);
            }
        }

        public void PlayGoalScore()
        {
            if (_goalScoreClip != null) _sfxSource.PlayOneShot(_goalScoreClip, 1f);
        }
    }
}
