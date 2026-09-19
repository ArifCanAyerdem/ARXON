using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arixon.Audio
{
    /// <summary>
    /// Sadece Ana Menü ve Lobide müziği çalan, oyun içi sahneye geçildiğinde 
    /// müziği durduran kesintisiz (DontDestroyOnLoad) ses yöneticisi.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource _audioSource;

        // Oyun başladığında otomatik olarak tetiklenir (Sahne yüklenmeden önce)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Instance == null)
            {
                GameObject audioManagerObj = new GameObject("AudioManager");
                Instance = audioManagerObj.AddComponent<AudioManager>();
                DontDestroyOnLoad(audioManagerObj);

                Debug.Log("[Audio] [AudioManager.Initialize] -> AudioManager otomatik olarak oluşturuldu ve DontDestroyOnLoad ayarlandı.");
            }
        }

        private void Awake()
        {
            // Singleton kontrolü - Eğer sahnede zaten bir AudioManager varsa bu kopyayı sil
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _audioSource = gameObject.AddComponent<AudioSource>();
            
            // AudioSource temel ayarları
            _audioSource.loop = true;         // Müzik bitince başa sarsın
            _audioSource.playOnAwake = false; // Biz kodla başlatacağız
            _audioSource.volume = 0.15f;       // Ses seviyesi %15

            // Sahne değişimlerini dinlemeye başla
            SceneManager.sceneLoaded += OnSceneLoaded;

            LoadMusic();
            
            // Müzik yüklendiğinde, bulunduğumuz sahneye göre çalmaya karar ver
            CheckAndPlayMusic(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            // Script yok edildiğinde dinlemeyi bırak ki hafıza sızıntısı (memory leak) olmasın
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CheckAndPlayMusic(scene.name);
        }

        private void CheckAndPlayMusic(string sceneName)
        {
            // Eğer oyun sahnemize ("SampleScene") girersek müziği durdur
            if (sceneName == "SampleScene")
            {
                if (_audioSource.isPlaying)
                {
                    _audioSource.Stop();
                    Debug.Log($"[Audio] [AudioManager.CheckAndPlayMusic] -> Sahne: {sceneName} | Oyun alanına girildi, lobi müziği durduruldu.");
                }
            }
            // Aksi takdirde (MainMenu / Lobi) müziği başlat
            else
            {
                if (!_audioSource.isPlaying && _audioSource.clip != null)
                {
                    _audioSource.Play();
                    Debug.Log($"[Audio] [AudioManager.CheckAndPlayMusic] -> Sahne: {sceneName} | Lobiye girildi, müzik oynatılıyor.");
                }
            }
        }

        private void LoadMusic()
        {
            // Resources/Audio/Music/TRACK_01 içerisindeki ses dosyasını yükle
            AudioClip bgm = Resources.Load<AudioClip>("Audio/Music/TRACK_01");
            
            if (bgm != null)
            {
                _audioSource.clip = bgm;
                Debug.Log($"[Audio] [AudioManager.LoadMusic] -> Müzik dosyası başarıyla yüklendi (Dosya: {bgm.name})");
            }
            else
            {
                Debug.LogError("[Audio] [AudioManager.LoadMusic] -> HATA: 'Audio/Music/TRACK_01' konumunda müzik dosyası bulunamadı!");
            }
        }
    }
}
