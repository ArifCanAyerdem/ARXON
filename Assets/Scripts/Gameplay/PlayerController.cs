using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Arixon.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 8f;
        [SerializeField] private float _rotationSpeed = 15f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _jumpHeight = 2f;

        private CharacterController _characterController;
        private Transform _mainCameraTransform;
        private Vector3 _velocity;
        private bool _isGrounded;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            Debug.Log($"[Gameplay] [PlayerController.Awake] -> CharacterController bileşeni alındı. (Obje: {gameObject.name})");
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log($"[Network] [PlayerController.OnNetworkSpawn] -> Oyuncu sahneye doğdu. (IsOwner: {IsOwner})");

            if (IsOwner)
            {
                Debug.Log("[Gameplay] [PlayerController.OnNetworkSpawn] -> Yerel oyuncu (LocalPlayer) tespit edildi, kamera aranıyor...");
                // Main Camera'yı bul
                if (Camera.main != null)
                {
                    _mainCameraTransform = Camera.main.transform;
                    Debug.Log("[Gameplay] [PlayerController.OnNetworkSpawn] -> Main Camera bulundu, PlayerCameraFollow atanıyor.");
                    
                    // Kameraya bu oyuncuyu takip etmesini söyle
                    var camFollow = Camera.main.GetComponent<PlayerCameraFollow>();
                    if (camFollow == null)
                    {
                        Debug.Log("[Gameplay] [PlayerController.OnNetworkSpawn] -> Kamerada PlayerCameraFollow yoktu, koda eklendi.");
                        camFollow = Camera.main.gameObject.AddComponent<PlayerCameraFollow>();
                    }
                    camFollow.SetTarget(this.transform);
                    Debug.Log($"[Gameplay] [PlayerController.OnNetworkSpawn] -> Kamera takibi başlatıldı. (Hedef: {this.transform.name})");
                }
                else
                {
                    Debug.LogWarning("[Gameplay] [PlayerController.OnNetworkSpawn] -> SAHNEDE MAIN CAMERA BULUNAMADI! Kamera takibi çalışmayacak.");
                }
            }
        }

        [Header("Interaction Settings")]
        [SerializeField] private float _hitForce = 15f;
        [SerializeField] private float _dashForceMultiplier = 3f;
        [SerializeField] private float _dashSpeed = 25f;
        [SerializeField] private float _dashDuration = 0.2f;
        [SerializeField] private float _dashCooldown = 1.0f;

        private bool _isDashing = false;
        private float _dashTimer = 0f;
        private float _dashCooldownTimer = 0f;

        private void Update()
        {
            if (!IsOwner) return;

            EnsureCameraFollows();
            
            // Kullanıcının kararına göre Dash özelliği şimdilik kapalı, ileri sürümlerde açılacak
            // HandleDash();
            
            HandleMovement();
            HandleGravityAndJump();
        }

        private float _cameraSearchTimer = 0f;

        private void EnsureCameraFollows()
        {
            if (_mainCameraTransform != null) return;

            _cameraSearchTimer -= Time.deltaTime;
            if (_cameraSearchTimer > 0f) return;
            
            _cameraSearchTimer = 1f;

            if (Camera.main != null)
            {
                _mainCameraTransform = Camera.main.transform;
                var camFollow = Camera.main.GetComponent<PlayerCameraFollow>();
                if (camFollow == null)
                {
                    camFollow = Camera.main.gameObject.AddComponent<PlayerCameraFollow>();
                }
                camFollow.SetTarget(this.transform);
                Debug.Log($"[Gameplay] [PlayerController.EnsureCameraFollows] -> Kamera yeniden bulundu ve takibe bağlandı.");
            }
        }

        private void HandleDash()
        {
            if (_dashCooldownTimer > 0) _dashCooldownTimer -= Time.deltaTime;

            if (Keyboard.current != null && Keyboard.current.shiftKey.wasPressedThisFrame && _dashCooldownTimer <= 0 && !_isDashing)
            {
                _isDashing = true;
                _dashTimer = _dashDuration;
                _dashCooldownTimer = _dashCooldown;
                Debug.Log("[Gameplay] [PlayerController.HandleDash] -> Oyuncu ileri atıldı (Dash)!");
            }

            if (_isDashing)
            {
                _dashTimer -= Time.deltaTime;
                _characterController.Move(transform.forward * (_dashSpeed * Time.deltaTime));

                if (_dashTimer <= 0)
                {
                    _isDashing = false;
                }
            }
        }

        private void HandleMovement()
        {
            float moveX = 0f;
            float moveZ = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.dKey.isPressed) moveX += 1f;
                if (Keyboard.current.aKey.isPressed) moveX -= 1f;
                if (Keyboard.current.wKey.isPressed) moveZ += 1f;
                if (Keyboard.current.sKey.isPressed) moveZ -= 1f;
            }

            // Karakteri her zaman farenin (kameranın) baktığı yöne döndür
            if (_mainCameraTransform != null)
            {
                Quaternion targetRotation = Quaternion.Euler(0f, _mainCameraTransform.eulerAngles.y, 0f);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }

            Vector3 inputDirection = new Vector3(moveX, 0f, moveZ).normalized;

            if (inputDirection.magnitude >= 0.1f)
            {
                // W/A/S/D tuşlarını karakterin mevcut yönüne (kameranın yönüne) göre uygula
                Vector3 moveDirection = (transform.right * inputDirection.x + transform.forward * inputDirection.z).normalized;
                _characterController.Move(moveDirection * (_moveSpeed * Time.deltaTime));
            }
        }

        private void HandleGravityAndJump()
        {
            _isGrounded = _characterController.isGrounded;

            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }

            bool jumpPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

            if (jumpPressed && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }

            _velocity.y += _gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!IsOwner) return; // Sadece kendi vuruşlarımızı sunucuya iletelim

            // Çarptığımız obje top mu?
            if (hit.gameObject.TryGetComponent(out GameBall ball))
            {
                NetworkObject ballNetObj = ball.GetComponent<NetworkObject>();
                if (ballNetObj != null)
                {
                    // Vuruş yönü: Karakterden topa doğru yatay (Y eksenini hafif yukarı verelim ki havalansın)
                    Vector3 forceDirection = hit.gameObject.transform.position - transform.position;
                    forceDirection.y = 0.5f; // Topu hafif havaya kaldır
                    forceDirection.Normalize();

                    // Eğer atılma (Dash) yapılıyorsa çok daha güçlü vur!
                    float finalForce = _isDashing ? _hitForce * _dashForceMultiplier : _hitForce;

                    // Sunucuya topa vurmasını söyle
                    HitBallServerRpc(ballNetObj, forceDirection, finalForce);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void HitBallServerRpc(NetworkObjectReference ballRef, Vector3 direction, float force, ServerRpcParams rpcParams = default)
        {
            if (ballRef.TryGet(out NetworkObject ballNetObj))
            {
                GameBall ball = ballNetObj.GetComponent<GameBall>();
                if (ball != null)
                {
                    ball.HitBall(direction * force);
                }
            }
        }
    }
}
