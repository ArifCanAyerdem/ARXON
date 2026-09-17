using UnityEngine;
using Unity.Netcode;

namespace Arixon.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class GameBall : NetworkBehaviour
    {
        private Rigidbody _rb;
        private Vector3 _startPosition;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public override void OnNetworkSpawn()
        {
            _startPosition = transform.position;

            // Sadece sunucu fizik kurallarını hesaplayıp güncelleyecek
            if (!IsServer)
            {
                _rb.isKinematic = true;
            }
        }

        public void HitBall(Vector3 force)
        {
            if (IsServer)
            {
                _rb.AddForce(force, ForceMode.Impulse);
                Debug.Log($"[Gameplay] [GameBall.HitBall] -> Topa vuruldu! (Force: {force})");
            }
        }

        public void ResetBall()
        {
            if (IsServer)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                transform.position = _startPosition;
                Debug.Log("[Gameplay] [GameBall.ResetBall] -> Top merkeze sıfırlandı.");
            }
        }
    }
}
