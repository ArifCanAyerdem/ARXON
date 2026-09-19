using UnityEngine;

namespace Arixon.Gameplay
{
    /// <summary>
    /// Bu script, StarterAssets animasyonlarının (Walk, JumpLand vs.) fırlattığı 
    /// OnFootstep ve OnLand Animation Event'lerini yakalayarak konsolda beliren 
    /// 'has no receiver' hatasını engeller.
    /// </summary>
    public class StarterAssetsEventReceiver : MonoBehaviour
    {
        private void OnFootstep(AnimationEvent animationEvent)
        {
            // İsteğe bağlı olarak buraya ayak sesi efekti eklenebilir.
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            // İsteğe bağlı olarak buraya yere düşme/inme sesi efekti eklenebilir.
        }
    }
}
