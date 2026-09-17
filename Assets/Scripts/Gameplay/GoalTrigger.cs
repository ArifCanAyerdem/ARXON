using UnityEngine;
using Unity.Netcode;

namespace Arixon.Gameplay
{
    public class GoalTrigger : NetworkBehaviour
    {
        [SerializeField] private int _teamId; // 1: Mavi, 2: Kırmızı
        
        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return; // Golleri sadece sunucu kontrol eder

            if (other.TryGetComponent(out GameBall ball))
            {
                Debug.Log($"[Gameplay] [GoalTrigger] -> GOL! Takım {_teamId} kalesine gol atıldı!");
                
                // Topu sıfırla
                ball.ResetBall();

                // İleride buraya skor güncelleme eventi eklenecek
                // ScoreManager.Instance.AddScore(_teamId == 1 ? 2 : 1);
            }
        }
    }
}
