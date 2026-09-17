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

        private void Update()
        {
            if (!IsOwner) return;

            HandleMovement();
            HandleGravityAndJump();
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

            Vector3 inputDirection = new Vector3(moveX, 0f, moveZ).normalized;

            if (inputDirection.magnitude >= 0.1f)
            {
                // Kamera açısına göre yönü hesapla
                float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
                
                if (_mainCameraTransform != null)
                {
                    targetAngle += _mainCameraTransform.eulerAngles.y;
                }

                // Yumuşak ama çok hızlı rotasyon (Çevik his için)
                Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);

                // İleri yönde hareket
                Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                _characterController.Move(moveDirection.normalized * (_moveSpeed * Time.deltaTime));
            }
        }

        private void HandleGravityAndJump()
        {
            _isGrounded = _characterController.isGrounded;

            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Yere tam basması için hafif bir negatif değer
            }

            bool jumpPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

            if (jumpPressed && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                Debug.Log($"[Gameplay] [PlayerController.HandleGravityAndJump] -> Zıplama tetiklendi. (Zıplama Gücü: {_velocity.y})");
            }

            _velocity.y += _gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
        }
    }
}
