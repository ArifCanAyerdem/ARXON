using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arixon.Core
{
    public static class SceneBootstrapper
    {
        private const int BOOTSTRAP_SCENE_INDEX = 0; // MainMenu

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            // Eğer oyun başlatıldığında bulunduğumuz sahne MainMenu değilse ve NetworkManager yoksa,
            // zorla MainMenu'ye dön. (Multiplayer Play Mode clone'larının yanlış sahnede başlamasını önler)
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            
            // Editörde bağımsız sahneleri test edebilmek için küçük bir kontrol:
            // Eğer sahnede zaten bir NetworkManager VEYA ArixonNetworkManager varsa (örn. MainMenu) geç.
            // Ama SampleScene gibi içinde NetworkManager barındırmayan bir sahne doğrudan açıldıysa,
            // Bootstrapper onu MainMenu'ye yönlendirecek.
            
            if (currentSceneIndex != BOOTSTRAP_SCENE_INDEX)
            {
                Debug.Log($"[Bootstrapper] Oyun SceneIndex:{currentSceneIndex} üzerinden başlatıldı. MainMenu'ye (0) yönlendiriliyor...");
                SceneManager.LoadScene(BOOTSTRAP_SCENE_INDEX);
            }
        }
    }
}
