using UnityEngine;
using Unity.Netcode;

namespace Arixon.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class GameBall : NetworkBehaviour
    {
        private Rigidbody _rb;
        private Vector3 _startPosition;
        private bool _isWaitingForFirstTouch = true;
        
        // Gol olduğunda kime yazılacağını bilmek için
        public ulong LastTouchedPlayerId { get; private set; } = 999;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public override void OnNetworkSpawn()
        {
            _startPosition = transform.position;
            _isWaitingForFirstTouch = true;

            // Sadece sunucu fizik kurallarını hesaplayıp güncelleyecek
            if (!IsServer)
            {
                _rb.isKinematic = true;
            }
        }

        private void FixedUpdate()
        {
            if (IsServer && _isWaitingForFirstTouch)
            {
                // Topun sadece dikey (y) ekseninde zıplamasına izin ver, yatay hareketleri ve dönmeyi sıfırla
                _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
                _rb.angularVelocity = Vector3.zero;
                
                // Kesinlikle kaymaması için başlangıç x, z koordinatına hapsedelim
                transform.position = new Vector3(_startPosition.x, transform.position.y, _startPosition.z);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsServer) return;

            // Eğer oyun henüz "Playing" (Oynanıyor) durumunda değilse, kimse topa dokunamaz
            if (MatchManager.Instance != null && MatchManager.Instance.CurrentState.Value != MatchState.Playing)
                return;

            // Oyuncu (PlayerController) çarptıysa topu serbest bırak ve son dokunanı kaydet
            if (collision.gameObject.TryGetComponent(out PlayerController pc) || collision.gameObject.GetComponentInParent<PlayerController>() != null)
            {
                if (pc == null) pc = collision.gameObject.GetComponentInParent<PlayerController>();

                if (pc != null && pc.NetworkObject != null)
                {
                    LastTouchedPlayerId = pc.OwnerClientId;
                }

                if (_isWaitingForFirstTouch)
                {
                    _isWaitingForFirstTouch = false;
                    Debug.Log("[Gameplay] [GameBall.OnCollisionEnter] -> Oyuncu topa dokundu, top serbest bırakıldı.");
                }
            }
        }

        public void HitBall(Vector3 force, ulong hitterId)
        {
            if (IsServer)
            {
                if (MatchManager.Instance != null && MatchManager.Instance.CurrentState.Value != MatchState.Playing)
                    return;

                _isWaitingForFirstTouch = false;
                LastTouchedPlayerId = hitterId;
                _rb.AddForce(force, ForceMode.Impulse);
                Debug.Log($"[Gameplay] [GameBall.HitBall] -> Topa vuruldu! (Hitter: {hitterId}, Force: {force})");
            }
        }

        public void ResetBall()
        {
            if (IsServer)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                transform.position = _startPosition;
                _isWaitingForFirstTouch = true;
                LastTouchedPlayerId = 999; // Sıfırla
                Debug.Log("[Gameplay] [GameBall.ResetBall] -> Top merkeze sıfırlandı ve ilk vuruş için kilitlendi.");
            }
        }
    }
}
