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

        [Header("Takım ve Renk Yönetimi")]
        public NetworkVariable<int> TeamColorID = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private Renderer _playerRenderer;
        private Material _originalMaterial;

        private CharacterController _characterController;
        private Transform _mainCameraTransform;
        private Vector3 _velocity;
        private bool _isGrounded;
        private float _jumpBufferTimer = 0f; // Zıplama gecikmesini önlemek için hafıza süresi

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            Debug.Log($"[Gameplay] [PlayerController.Awake] -> CharacterController bileşeni alındı. (Obje: {gameObject.name})");

            // Görsel kapsülün Renderer'ını bul
            Transform visuals = transform.Find("VisualsHolder");
            if (visuals != null)
            {
                Transform model = visuals.Find("DefaultModel_Temporary");
                if (model != null)
                {
                    _playerRenderer = model.GetComponent<Renderer>();
                    if (_playerRenderer != null)
                    {
                        _originalMaterial = new Material(_playerRenderer.sharedMaterial);
                        _playerRenderer.material = _originalMaterial;
                    }
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                Transform camTarget = transform.Find("CameraTarget");
                if (camTarget == null)
                {
                    GameObject ctObj = new GameObject("CameraTarget");
                    ctObj.transform.SetParent(transform);
                    ctObj.transform.localPosition = new Vector3(0, 2f, 0);
                    camTarget = ctObj.transform;
                }

                PlayerCameraFollow camFollow = FindFirstObjectByType<PlayerCameraFollow>();
                if (camFollow != null)
                {
                    camFollow.SetTarget(camTarget);
                }
            }

            // Renk senkronizasyonu
            TeamColorID.OnValueChanged += OnTeamColorChanged;

            // TrailRenderer Kurulumu (Neon Işık İzi)
            _sprintTrail = gameObject.AddComponent<TrailRenderer>();
            _sprintTrail.time = 0.3f; // Daha kısa bir iz
            _sprintTrail.startWidth = 0.8f; // Daha ince
            _sprintTrail.endWidth = 0f;
            _sprintTrail.material = new Material(Shader.Find("Sprites/Default"));
            _sprintTrail.emitting = IsSprinting.Value;

            IsSprinting.OnValueChanged += OnSprintStateChanged;

            ApplyTeamColor(TeamColorID.Value);
        }

        public override void OnNetworkDespawn()
        {
            TeamColorID.OnValueChanged -= OnTeamColorChanged;
            IsSprinting.OnValueChanged -= OnSprintStateChanged;
        }

        private void OnSprintStateChanged(bool oldVal, bool newVal)
        {
            if (_sprintTrail != null)
            {
                _sprintTrail.emitting = newVal;
            }
        }

        private void OnTeamColorChanged(int previousValue, int newValue)
        {
            ApplyTeamColor(newValue);
        }

        private void ApplyTeamColor(int teamId)
        {
            if (_playerRenderer == null || _originalMaterial == null) return;

            if (teamId == 1) // MAVİ
            {
                _originalMaterial.color = Color.blue;
                _originalMaterial.EnableKeyword("_EMISSION");
                _originalMaterial.SetColor("_EmissionColor", Color.blue * 1.5f);

                if (_sprintTrail != null)
                {
                    _sprintTrail.startColor = new Color(0, 0.8f, 1f, 0.4f); // Neon Cyan/Blue (Yumuşatılmış Opaklık)
                    _sprintTrail.endColor = new Color(0, 0.2f, 1f, 0f);
                }
            }
            else if (teamId == 2) // KIRMIZI
            {
                _originalMaterial.color = Color.red;
                _originalMaterial.EnableKeyword("_EMISSION");
                _originalMaterial.SetColor("_EmissionColor", Color.red * 1.5f);

                if (_sprintTrail != null)
                {
                    _sprintTrail.startColor = new Color(1f, 0.2f, 0f, 0.4f); // Neon Red/Orange (Yumuşatılmış Opaklık)
                    _sprintTrail.endColor = new Color(1f, 0, 0, 0f);
                }
            }
            else
            {
                _originalMaterial.color = Color.white;
                _originalMaterial.DisableKeyword("_EMISSION");
            }
        }

        [Header("Interaction Settings")]
        [SerializeField] private float _hitForce = 15f;
        [SerializeField] private float _dashForceMultiplier = 3f;
        [Header("Stamina & Boost Settings")]
        public float MaxStamina = 100f;
        public NetworkVariable<float> CurrentStamina = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        [SerializeField] private float _sprintDrainRate = 25f;
        [SerializeField] private float _staminaRegenRate = 15f;
        [SerializeField] private float _sprintSpeedMultiplier = 1.8f;
        [SerializeField] private float _boostJumpForce = 25f;

        public NetworkVariable<bool> IsSprinting = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private TrailRenderer _sprintTrail;

        private void Update()
        {
            if (!IsOwner) return;

            EnsureCameraFollows();
            
            // Eğer maç başlamadıysa (ışınlanma veya 3, 2, 1 geri sayımı) karakteri DONDUR.
            if (MatchManager.Instance != null && !MatchManager.Instance.IsPlaying) return;
            
            // Enerji (Stamina) ve Sprint kontrolü
            HandleStamina();
            
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

        private Arixon.UI.GameUIController _cachedUIController;

        private bool _isExhausted = false;

        private void HandleStamina()
        {
            if (Keyboard.current == null) return;

            bool shiftPressed = Keyboard.current.shiftKey.isPressed;

            // Eğer tuşu bırakırsak yorgunluk hissi kalkar (tekrar basabilmek için)
            if (!shiftPressed)
            {
                _isExhausted = false;
            }

            // Stamina sıfırlanırsa (0.1'in altına düşerse) zorunlu olarak tüketildi say, uçmayı bırak
            if (CurrentStamina.Value <= 0.1f)
            {
                _isExhausted = true;
            }

            // SHIFT'e basılıysa ve tükenmemişsek (Stamina > 0 ise) sprint/boost yap
            if (shiftPressed && !_isExhausted)
            {
                IsSprinting.Value = true;
                float newVal = CurrentStamina.Value - (_sprintDrainRate * Time.deltaTime);
                CurrentStamina.Value = Mathf.Clamp(newVal, 0f, MaxStamina);
            }
            else
            {
                IsSprinting.Value = false;
                // Şarj olma durumu
                float newVal = CurrentStamina.Value + (_staminaRegenRate * Time.deltaTime);
                CurrentStamina.Value = Mathf.Clamp(newVal, 0f, MaxStamina);
            }

            // Sadece yerel oyuncu kendi UI'ını günceller
            if (IsOwner)
            {
                if (_cachedUIController == null)
                    _cachedUIController = FindFirstObjectByType<Arixon.UI.GameUIController>();
                
                if (_cachedUIController != null)
                    _cachedUIController.UpdateStamina(CurrentStamina.Value / MaxStamina);
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
                
                // Eğer Sprint yapılıyorsa ve yerdeysek hızı katla
                float currentSpeed = (IsSprinting.Value && _isGrounded) ? _moveSpeed * _sprintSpeedMultiplier : _moveSpeed;

                _characterController.Move(moveDirection * (currentSpeed * Time.deltaTime));
            }
        }

        private void HandleGravityAndJump()
        {
            _isGrounded = _characterController.isGrounded;

            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }

            // Zıplama tuşuna basıldığını hafızaya al (0.2 saniye boyunca hatırla)
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                _jumpBufferTimer = 0.2f;
            }
            else if (_jumpBufferTimer > 0)
            {
                _jumpBufferTimer -= Time.deltaTime;
            }

            // Hafızada zıplama komutu varsa ve yere değiyorsa anında zıpla (Gecikmeyi ve bekleme hissini yok eder)
            if (_jumpBufferTimer > 0 && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                _jumpBufferTimer = 0f; // Zıpladık, hafızayı sıfırla
            }

            // Havada süzülme (Jetpack/Boost Jump) mekaniği
            if (!_isGrounded && IsSprinting.Value)
            {
                // Yukarı doğru ivme ver (yerçekimini yenmek için)
                _velocity.y += _boostJumpForce * Time.deltaTime;
            }

            _velocity.y += _gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
        }

        private float _lastHitTime = 0f;
        private const float HIT_COOLDOWN = 0.2f;

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!IsOwner) return; // Sadece kendi vuruşlarımızı sunucuya iletelim

            // Çok sık (Spam) RPC göndermeyi engelle ki top saçma sapan hızlanmasın
            if (Time.time - _lastHitTime < HIT_COOLDOWN) return;

            // Çarptığımız obje top mu?
            if (hit.gameObject.TryGetComponent(out GameBall ball))
            {
                NetworkObject ballNetObj = ball.GetComponent<NetworkObject>();
                if (ballNetObj != null)
                {
                    _lastHitTime = Time.time;

                    // Vuruş yönü: Karakterden topa doğru yatay (Y eksenini hafif yukarı verelim ki havalansın)
                    Vector3 forceDirection = hit.gameObject.transform.position - transform.position;
                    forceDirection.y = 0.5f; // Topu hafif havaya kaldır
                    forceDirection.Normalize();

                    // Eğer atılma (Sprint) yapılıyorsa çok daha güçlü vur!
                    float finalForce = IsSprinting.Value ? _hitForce * _dashForceMultiplier : _hitForce;

                    // Sunucuya topa vurmasını söyle, kimin vurduğunu da ilet
                    HitBallServerRpc(ballNetObj, forceDirection, finalForce, OwnerClientId);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void HitBallServerRpc(NetworkObjectReference ballRef, Vector3 direction, float force, ulong hitterId, ServerRpcParams rpcParams = default)
        {
            if (ballRef.TryGet(out NetworkObject ballNetObj))
            {
                GameBall ball = ballNetObj.GetComponent<GameBall>();
                if (ball != null)
                {
                    ball.HitBall(direction * force, hitterId);
                }
            }
        }

        [ClientRpc]
        public void TargetTeleportClientRpc(Vector3 position, Quaternion rotation, ClientRpcParams clientRpcParams = default)
        {
            // CharacterController, transform.position atamalarını engeller, bu yüzden kapat-ata-aç yapmalıyız
            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            transform.position = position;
            transform.rotation = rotation;

            if (_characterController != null)
            {
                _characterController.enabled = true;
            }

            Debug.Log($"[Gameplay] [PlayerController] -> Sunucu komutuyla Spawn noktasına ışınlanıldı: {position}");
        }
    }
}
