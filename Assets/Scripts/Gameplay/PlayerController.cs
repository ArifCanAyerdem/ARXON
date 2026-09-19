using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Arixon.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _normalSpeed = 8f; // Hızlandırıldı
        [SerializeField] private float _sprintSpeed = 16f; // Hızlandırıldı
        [SerializeField] private float _rotationSpeed = 12f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _jumpHeight = 2f;
        [SerializeField] private float _acceleration = 15f;
        [SerializeField] private float _deceleration = 20f;
        [SerializeField] private float _tiltAmount = 15f;
        [SerializeField] private float _tiltSpeed = 10f;

        private Vector3 _currentMoveVelocity;
        private Transform _visualsHolder;
        private ParticleSystem _dustParticles;

        public NetworkVariable<int> TeamColorID = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private Renderer _playerRenderer;
        private Material[] _playerMaterials;
        private int _bodyMaterialIndex = 0;

        private CharacterController _characterController;
        private Transform _mainCameraTransform;
        private Vector3 _velocity;
        private bool _isGrounded;
        private float _jumpBufferTimer = 0f; // Zıplama gecikmesini önlemek için hafıza süresi

        [Header("Şut ve Pas Mekanikleri")]
        public NetworkVariable<float> ChargeLevel = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private bool _isCharging = false;
        private ParticleSystem _chargeParticles;
        private float _dribbleForce = 6f; // Sadece çarpıp sürerkenki hafif güç

        private void Awake()
        {
            // Ölçeklendirme (Daha büyük ve heybetli karakter)
            transform.localScale = Vector3.one * 1.25f; // %25 büyüt

            _characterController = GetComponent<CharacterController>();
            _visualsHolder = transform.Find("VisualsHolder"); // Tilt için görsel taşıyıcı
            Debug.Log($"[Gameplay] [PlayerController.Awake] -> CharacterController alındı. (Obje: {gameObject.name})");

            // Karakterin asıl modelini bul (SkinnedMeshRenderer genellikle asıl karakterdir)
            _playerRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (_playerRenderer == null)
            {
                _playerRenderer = GetComponentInChildren<Renderer>(); // Fallback
            }

            if (_playerRenderer != null)
            {
                Material[] sharedMats = _playerRenderer.sharedMaterials;
                _playerMaterials = new Material[sharedMats.Length];
                for (int i = 0; i < sharedMats.Length; i++)
                {
                    _playerMaterials[i] = new Material(sharedMats[i]);
                    // Assuming the body material is the one that is NOT named "Face" or is index 0
                    if (sharedMats[i].name.Contains("Body") || (i == 0 && !sharedMats[i].name.Contains("Face")))
                    {
                        _bodyMaterialIndex = i;
                    }
                }
                _playerRenderer.materials = _playerMaterials;
            }
        }

                public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                EnsureCameraFollows();
            }

            // Renk senkronizasyonu
            TeamColorID.OnValueChanged += OnTeamColorChanged;
            IsKnockedDown.OnValueChanged += OnKnockdownStateChanged;
            IsExhausted.OnValueChanged += OnExhaustedStateChanged;

            // Sprint Rüzgar Efekti (Wind Lines) Kurulumu
            GameObject windObj = new GameObject("SprintWind");
            windObj.transform.SetParent(this.transform);
            windObj.transform.localPosition = new Vector3(0, 1f, 0); // Karakterin ortası

            _sprintWindParticles = windObj.AddComponent<ParticleSystem>();
            _sprintWindParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // HATA ÖNLEMİ
            var wMain = _sprintWindParticles.main;
            wMain.duration = 1f;
            wMain.loop = true;
            wMain.startLifetime = 0.3f;
            wMain.startSpeed = 20f; // Arkaya doğru hızlıca uçsun
            wMain.startSize = 0.2f;
            wMain.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var wShape = _sprintWindParticles.shape;
            wShape.shapeType = ParticleSystemShapeType.Cone;
            wShape.radius = 0.5f;
            wShape.angle = 0f;
            wShape.rotation = new Vector3(0, 180, 0); // Arkaya doğru
            
            var wEmission = _sprintWindParticles.emission;
            wEmission.rateOverTime = 0f; // Sadece koşarken aktif
            
            var wRenderer = windObj.GetComponent<ParticleSystemRenderer>();
            wRenderer.renderMode = ParticleSystemRenderMode.Stretch; // Hız çizgileri gibi uzat
            wRenderer.lengthScale = 4f;
            wRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            
            IsSprinting.OnValueChanged += OnSprintStateChanged;

            ApplyTeamColor(TeamColorID.Value);

            // Şarj Efekti (Particle System) Kurulumu
            CreateChargeParticles();
            InitializeDustParticles();

            // Animasyon Referansı
            _animator = GetComponentInChildren<Animator>();
        }

        private void InitializeDustParticles()
        {
            GameObject dustObj = new GameObject("DustParticles");
            dustObj.transform.SetParent(this.transform);
            dustObj.transform.localPosition = new Vector3(0, 0.1f, 0); // Ayak hizası

            _dustParticles = dustObj.AddComponent<ParticleSystem>();
            _dustParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Durdur ki hata vermesin
            var main = _dustParticles.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = 0.5f;
            main.startSpeed = 1f;
            main.startSize = 0.5f;
            main.startColor = new Color(0.8f, 0.8f, 0.8f, 0.5f); // Toz rengi
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            
            var emission = _dustParticles.emission;
            emission.rateOverTime = 0f; // Sadece hareket ederken açılacak

            var shape = _dustParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;
        }

        private void CreateChargeParticles()
        {
            GameObject psObj = new GameObject("ChargeParticles");
            psObj.transform.SetParent(transform);
            psObj.transform.localPosition = new Vector3(0, 1f, 0); // Karakterin bel hizası

            _chargeParticles = psObj.AddComponent<ParticleSystem>();
            _chargeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // YAPILANDIRMADAN ÖNCE DURDUR

            var main = _chargeParticles.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = 0.4f;
            main.startSpeed = 3f;
            main.startSize = 0.08f; // ÇOK DAHA KÜÇÜK, zarif partiküller
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = _chargeParticles.emission;
            emission.rateOverTime = 0f; // Başlangıçta görünmez

            _chargeParticles.Play(); // YAPILANDIRMA BİTİNCE BAŞLAT

            var shape = _chargeParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.8f; // Karakterin etrafında daha dar bir çember

            var velOverTime = _chargeParticles.velocityOverLifetime;
            velOverTime.enabled = true;
            velOverTime.orbitalY = 15f; // Çok daha hızlı, sarmal dönen elektrik hissi
            velOverTime.orbitalZ = 2f;
            velOverTime.orbitalX = 2f;

            var colOverTime = _chargeParticles.colorOverLifetime;
            colOverTime.enabled = true;

            var renderer = psObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            if (renderer.material.shader == null) renderer.material = new Material(Shader.Find("Sprites/Default"));
        }



                public override void OnNetworkDespawn()
        {
            TeamColorID.OnValueChanged -= OnTeamColorChanged;
            IsSprinting.OnValueChanged -= OnSprintStateChanged;
            IsKnockedDown.OnValueChanged -= OnKnockdownStateChanged;
            IsExhausted.OnValueChanged -= OnExhaustedStateChanged;
        }

        private void OnKnockdownStateChanged(bool prev, bool current)
        {
            UpdateRagdollState();
        }

        private void OnExhaustedStateChanged(bool prev, bool current)
        {
            UpdateRagdollState();
        }

        private void UpdateRagdollState()
        {
            bool state = IsKnockedDown.Value || IsExhausted.Value;
            SetRagdollState(state);
            if (_animator != null)
            {
                _animator.SetBool("FreeFall", state);
            }
        }

        private void OnSprintStateChanged(bool oldVal, bool newVal)
        {
            if (_sprintWindParticles != null)
            {
                var em = _sprintWindParticles.emission;
                em.rateOverTime = newVal ? 40f : 0f;
            }
        }

        private void OnTeamColorChanged(int previousValue, int newValue)
        {
            Debug.Log($"[Gameplay] [PlayerController.OnTeamColorChanged] -> Takım rengi değişti. (Eski: {previousValue}, Yeni: {newValue})");
            ApplyTeamColor(newValue);
        }

        private void ApplyTeamColor(int teamId)
        {
            if (_playerRenderer == null || _playerMaterials == null || _playerMaterials.Length == 0) return;

            Material bodyMat = _playerMaterials[_bodyMaterialIndex];

            // Kaskın siyah kalması için sadece Albedo rengini ayarlıyoruz. Emission verirsek siyah alanlar da parlar!
            bodyMat.DisableKeyword("_EMISSION");

            if (teamId == 1) // MAVİ
            {
                bodyMat.color = new Color(0.2f, 0.4f, 1f); // Daha hoş bir mavi
                
                if (_sprintWindParticles != null)
                {
                    var wMain = _sprintWindParticles.main;
                    wMain.startColor = new Color(0.5f, 0.8f, 1f, 0.3f); // Açık mavi rüzgar
                }
            }
            else if (teamId == 2) // KIRMIZI
            {
                bodyMat.color = new Color(1f, 0.2f, 0.2f); // Daha hoş bir kırmızı
                
                if (_sprintWindParticles != null)
                {
                    var wMain = _sprintWindParticles.main;
                    wMain.startColor = new Color(1f, 0.6f, 0.6f, 0.3f); // Açık kırmızı rüzgar
                }
            }
            else
            {
                bodyMat.color = Color.white;
            }

            // Kaskın her zaman siyah kalmasını garanti et (Eğer farklı materyaller varsa 1. index kask olabilir)
            if (_playerMaterials.Length > 1)
            {
                _playerMaterials[1].color = new Color(0.1f, 0.1f, 0.1f); // Koyu siyah
                _playerRenderer.materials = _playerMaterials; // ATAMAYI YENİLE!
            }
        }

        [Header("Stamina & Boost Settings")]
        public float MaxStamina = 100f;
        public NetworkVariable<float> CurrentStamina = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        [SerializeField] private float _sprintDrainRate = 25f;
        [SerializeField] private float _staminaRegenRate = 15f;
        [SerializeField] private float _sprintSpeedMultiplier = 1.8f;
        [SerializeField] private float _boostJumpForce = 25f;

        public NetworkVariable<bool> IsSprinting = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private ParticleSystem _sprintWindParticles;

        private Animator _animator;
        private float _currentAnimSpeed = 0f;

        private float _fallTimer = 0f;
        private float _spawnGraceTimer = 2f; // Oyun başında/ışınlanma sonrası 2 saniyelik koruma
        public NetworkVariable<bool> IsKnockedDown = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private void Start()
        {
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
        }

        private void SetRagdollState(bool state)
        {
            Debug.Log($"[Gameplay] [PlayerController.SetRagdollState] -> Ragdoll durumu değiştirildi. (Durum: {(state ? "Yere Düştü" : "Ayağa Kalktı")}, IsExhausted: {IsExhausted.Value})");

            // Gerçek Ragdoll fizikleri (CharacterJoint) kurulana kadar "Fall" animasyonunu kullanıyoruz.
            // Bu yüzden Animator'ı kapatmıyoruz, sadece state'i ayarlıyoruz.
            if (_animator != null)
            {
                _animator.SetBool("FreeFall", state);
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            EnsureCameraFollows();

            // Spawn koruması sayacını azalt
            if (_spawnGraceTimer > 0f)
            {
                _spawnGraceTimer -= Time.deltaTime;
            }
            
            // Eğer maç başlamadıysa (ışınlanma veya 3, 2, 1 geri sayımı) karakteri DONDUR.
            if (MatchManager.Instance != null && !MatchManager.Instance.IsPlaying) 
            {
                UpdateAnimator(0f);
                return;
            }
            
            // Enerji (Stamina) ve Sprint kontrolü
            HandleStamina();
            
            HandleMovement();
            HandleGravityAndJump();
            HandleKickInputs();
        }

        private void UpdateAnimator(float targetSpeed)
        {
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_animator == null) return;

            // Animasyonlar arası pürüzsüz geçiş (Smooth transition)
            _currentAnimSpeed = Mathf.Lerp(_currentAnimSpeed, targetSpeed, Time.deltaTime * 10f);

            _animator.SetFloat("Speed", _currentAnimSpeed, 0.1f, Time.deltaTime);
            _animator.SetFloat("MotionSpeed", 1f); // StarterAssets blend tree animasyon hızı için gerekli
            _animator.SetBool("Grounded", _isGrounded);
            // Düşme / Yere serilme efekti
            _animator.SetBool("FreeFall", IsExhausted.Value || IsKnockedDown.Value); 
        }

        private void LateUpdate()
        {
            // Görseller herkes için (Server+Client) her frame güncellenir
            UpdateChargeVisuals();
        }

        private void FixedUpdate()
        {
            if (IsServer)
            {
                HandleServerCharging();
            }
        }

        private bool _isLocalCharging = false;

        private void HandleKickInputs()
        {
            if (Mouse.current == null) return;

            // Pas (Sağ Tık)
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                Vector3 forward = _mainCameraTransform != null ? _mainCameraTransform.forward : transform.forward;
                forward.y = 0;
                forward.Normalize();
                TryKickServerRpc(false, forward); // false = Pass
            }

            // Şut (Sol Tık) - Şarj Başlat / Bitir (Sürekli durum kontrolü)
            if (Mouse.current.leftButton.isPressed)
            {
                if (!_isLocalCharging && CurrentStamina.Value > 1f)
                {
                    _isLocalCharging = true;
                    SetChargingServerRpc(true);
                }
            }
            else
            {
                if (_isLocalCharging)
                {
                    _isLocalCharging = false;
                    Vector3 forward = _mainCameraTransform != null ? _mainCameraTransform.forward : transform.forward;
                    forward.y = 0;
                    forward.Normalize();
                    TryKickServerRpc(true, forward); // true = Shoot
                }
            }
        }

        [ServerRpc]
        private void SetChargingServerRpc(bool charging)
        {
            Debug.Log($"[Gameplay] [PlayerController.SetChargingServerRpc] -> Şarj durumu sunucuya bildirildi. (ŞarjEdiliyor: {charging}, ClientId: {OwnerClientId})");
            _isCharging = charging;
            if (!charging) ChargeLevel.Value = 0f; // İptal veya bırakıldıysa sıfırla
        }

        private void HandleServerCharging()
        {
            if (_isCharging && ChargeLevel.Value < 1f)
            {
                // Yaklaşık 1.5 saniyede tam şarj olur (100 üzerinden 1.0)
                ChargeLevel.Value += Time.fixedDeltaTime / 1.5f; 
                if (ChargeLevel.Value > 1f) ChargeLevel.Value = 1f;
            }
        }

        [ServerRpc]
        private void TryKickServerRpc(bool isShoot, Vector3 cameraForward)
        {
            Debug.Log($"[Gameplay] [PlayerController.TryKickServerRpc] -> Vuruş denemesi. (ŞutMu: {isShoot}, ŞarjMiktarı: {ChargeLevel.Value})");
            _isCharging = false;
            float currentCharge = ChargeLevel.Value;
            ChargeLevel.Value = 0f;

            // Karakterin etrafındaki 4.5 metrelik alanda topu bul (Havadan geçerken topu rahat yakalayabilmesi için alan büyütüldü)
            Collider[] hits = Physics.OverlapSphere(transform.position, 4.5f);
            GameBall ball = null;
            foreach (var h in hits)
            {
                if (h.TryGetComponent(out GameBall b))
                {
                    ball = b;
                    break;
                }
            }

            if (ball != null)
            {
                Vector3 forceDir = cameraForward;
                
                if (isShoot)
                {
                    // Eğer karakter yere yakınsa ve düz bakıyorsa topu havalandır (Lob efekti).
                    // Rocket League tarzı vuruşlar için lift (yukarı güç) eklendi
                    if (transform.position.y < 2.5f && forceDir.y > -0.2f)
                    {
                        forceDir.y += 0.4f + (currentCharge * 0.6f);
                    }
                }
                else
                {
                    // Pas her zaman hafif yere doğru/paralel gitsin
                    forceDir.y = 0.1f;
                }
                
                forceDir.Normalize();

                float force = 0f;
                if (isShoot)
                {
                    // Şarj seviyesine göre 15 ile 55 arasında kontrollü bir şut gücü!
                    force = Mathf.Lerp(15f, 55f, currentCharge); 
                    Debug.Log($"[Gameplay] ŞUT ÇEKİLDİ! Şarj: %{(currentCharge*100):F0} | Kuvvet: {force:F1}");
                    PlayAnimationClientRpc("Kick");
                }
                else
                {
                    force = 12f; // Pas daha yumuşak
                    Debug.Log($"[Gameplay] PAS VERİLDİ!");
                    PlayAnimationClientRpc("Pass");
                }

                StartCoroutine(DelayedKickCoroutine(ball, forceDir, force, currentCharge, isShoot));
            }
        }

        private System.Collections.IEnumerator DelayedKickCoroutine(GameBall ball, Vector3 forceDir, float force, float charge, bool isShoot)
        {
            // Animasyon senkronizasyonu için küçük bir gecikme. Ağ (Network) gecikmelerinde hissi bozmaması için 0.05'e düşürüldü.
            yield return new WaitForSeconds(0.05f);

            if (ball != null)
            {
                ball.HitBall(forceDir * force, OwnerClientId);
                TriggerHitEffectsClientRpc(ball.transform.position, charge, isShoot);
            }
        }

        [ClientRpc]
        private void TriggerHitEffectsClientRpc(Vector3 hitPos, float charge, bool isShoot)
        {
            if (isShoot && charge > 0.8f)
            {
                // Güçlü Kamera Sarsıntısı
                if (Camera.main != null)
                {
                    var camFollow = Camera.main.GetComponent<PlayerCameraFollow>();
                    if (camFollow != null) camFollow.TriggerShake(0.8f, 0.2f);
                }
                
                // Patlama/Flaş Efekti (Basit Geometri ile Geçici VFX)
                GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flash.transform.position = hitPos;
                flash.transform.localScale = Vector3.one * 2f;
                Destroy(flash.GetComponent<Collider>());
                var mr = flash.GetComponent<MeshRenderer>();
                mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mr.material.color = Color.white;
                mr.material.EnableKeyword("_EMISSION");
                mr.material.SetColor("_EmissionColor", Color.white * 5f);
                Destroy(flash, 0.1f); // 0.1 saniyede yok olsun

                if (AudioManager.Instance != null) AudioManager.Instance.PlayKickHeavy();
            }
            else if (isShoot)
            {
                // Normal şutlarda hafif sarsıntı
                if (Camera.main != null)
                {
                    var camFollow = Camera.main.GetComponent<PlayerCameraFollow>();
                    if (camFollow != null) camFollow.TriggerShake(0.3f, 0.1f);
                }
                
                if (AudioManager.Instance != null) AudioManager.Instance.PlayKickLight();
            }
            else
            {
                // Pas
                if (AudioManager.Instance != null) AudioManager.Instance.PlayKickLight();
            }
        }

        [ClientRpc]
        private void PlayAnimationClientRpc(string triggerName)
        {
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                _animator.SetTrigger(triggerName);
            }
        }

        private void UpdateChargeVisuals()
        {
            if (_chargeParticles == null) return;

            float charge = ChargeLevel.Value;
            var em = _chargeParticles.emission;
            var main = _chargeParticles.main;
            
            if (charge > 0.02f) 
            {
                // Play() / Stop() metodlarını sürekli çağırmak yerine sadece emission'u (üretimi) açıp kapatıyoruz.
                // Bu, konsoldaki "Setting the duration while system is still playing" hatasını KÖKÜNDEN ÇÖZER.
                em.rateOverTime = Mathf.Lerp(20f, 150f, charge);
                main.startSpeed = Mathf.Lerp(3f, 8f, charge);

                Color particleColor = Color.green;
                if (charge < 0.5f) 
                    particleColor = Color.Lerp(Color.green, Color.yellow, charge * 2f);
                else 
                    particleColor = Color.Lerp(Color.yellow, new Color(1f, 0.2f, 0f), (charge - 0.5f) * 2f); 
                
                main.startColor = particleColor;
            }
            else
            {
                em.rateOverTime = 0f; // Şarj yoksa üretim yok
            }
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

        public NetworkVariable<bool> IsExhausted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private void HandleStamina()
        {
            if (Keyboard.current == null) return;

            bool shiftPressed = Keyboard.current.shiftKey.isPressed;

            if (CurrentStamina.Value <= 0.1f)
            {
                if (!IsExhausted.Value)
                {
                    IsExhausted.Value = true;
                    SetRagdollState(true); // Enerji bitince ragdoll'a dönüş (düşme)
                    Debug.Log("[Gameplay] Stamina bitti, karakter Ragdoll moduna geçti.");
                }

                // Eğer şarj ediyorken stamina bittiyse, oyuncu daha fazla tutamayıp otomatik şutu ateşler!
                if (_isLocalCharging)
                {
                    _isLocalCharging = false;
                    Vector3 forward = _mainCameraTransform != null ? _mainCameraTransform.forward : transform.forward;
                    forward.y = 0;
                    forward.Normalize();
                    TryKickServerRpc(true, forward);
                }
            }

            float staminaDrain = 0f;

            // Koşma Stamina Tüketimi (Sadece belli bir seviyenin üstündeyse tekrar koşmaya izin ver)
            if (shiftPressed && !IsExhausted.Value && CurrentStamina.Value > MaxStamina * 0.1f)
            {
                IsSprinting.Value = true;
                staminaDrain += _sprintDrainRate;
            }
            else
            {
                IsSprinting.Value = false;
            }

            // Stamina dolunca Ragdoll'dan çık
            if (IsExhausted.Value && CurrentStamina.Value > MaxStamina * 0.3f)
            {
                IsExhausted.Value = false;
                SetRagdollState(false);
                Debug.Log("[Gameplay] Stamina biraz doldu, karakter ayağa kalktı.");
            }

            // Şarj (Basılı Tutma) Stamina Tüketimi
            if (_isLocalCharging && !IsExhausted.Value)
            {
                staminaDrain += 40f; // Saniyede 40 birim! Şarjı fullemek 1.5 saniyede 60 stamina yer.
            }

            if (staminaDrain > 0f)
            {
                // Tüketim var
                float newVal = CurrentStamina.Value - (staminaDrain * Time.deltaTime);
                CurrentStamina.Value = Mathf.Clamp(newVal, 0f, MaxStamina);
            }
            else
            {
                // Hiçbir şey yapmıyorsa Yenilenme
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
            if (!IsOwner) return;

            Vector3 velocity = Vector3.zero;

            if (IsExhausted.Value || IsKnockedDown.Value)
            {
                // Yığılmış veya düşmüş durumda
                _currentMoveVelocity.x = Mathf.Lerp(_currentMoveVelocity.x, 0f, Time.deltaTime * 2f);
                _currentMoveVelocity.z = Mathf.Lerp(_currentMoveVelocity.z, 0f, Time.deltaTime * 2f);
                
                if (!_characterController.isGrounded)
                {
                    _currentMoveVelocity.y -= 20f * Time.deltaTime;
                }
                
                _characterController.Move(_currentMoveVelocity * Time.deltaTime);
                return;
            }

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
            Vector3 desiredVelocity = Vector3.zero;
            float targetTilt = 0f;

            if (inputDirection.magnitude >= 0.1f)
            {
                // W/A/S/D tuşlarını karakterin mevcut yönüne (kameranın yönüne) göre uygula
                Vector3 moveDirection = (transform.right * inputDirection.x + transform.forward * inputDirection.z).normalized;
                
                float targetSpeed = (IsSprinting.Value && _isGrounded) ? _sprintSpeed : _normalSpeed;
                desiredVelocity = moveDirection * targetSpeed;

                // Sağa/Sola giderken Tilt hesapla (inputDirection.x)
                targetTilt = -inputDirection.x * _tiltAmount;

                float targetAnimSpeed = (IsSprinting.Value && _isGrounded) ? 1f : 0.5f;
                UpdateAnimator(targetAnimSpeed);
            }
            else
            {
                UpdateAnimator(0f);
            }

            // İvmeli Hareket (Momentum) - Anında durmaz/hızlanmaz
            float accelRate = (inputDirection.magnitude > 0) ? _acceleration : _deceleration;
            _currentMoveVelocity = Vector3.Lerp(_currentMoveVelocity, desiredVelocity, accelRate * Time.deltaTime);

            if (_characterController.enabled)
            {
                _characterController.Move(_currentMoveVelocity * Time.deltaTime);
            }

            // Görseli yana yatır (Tilt)
            if (_visualsHolder != null)
            {
                Quaternion currentTilt = _visualsHolder.localRotation;
                Quaternion desiredTilt = Quaternion.Euler(0, 0, targetTilt);
                _visualsHolder.localRotation = Quaternion.Slerp(currentTilt, desiredTilt, _tiltSpeed * Time.deltaTime);
            }

            // Ayak Tozu (Dust) Efekti
            if (_dustParticles != null)
            {
                var emission = _dustParticles.emission;
                if (_isGrounded && inputDirection.magnitude > 0.1f)
                {
                    emission.rateOverTime = IsSprinting.Value ? 20f : 10f;
                }
                else
                {
                    emission.rateOverTime = 0f;
                }
            }
        }

        private void HandleGravityAndJump()
        {
            if (!_characterController.enabled) return;

            bool wasGrounded = _isGrounded;
            _isGrounded = _characterController.isGrounded;

            if (!_isGrounded)
            {
                // Zıplama yükselişini sayma, SADECE serbest düşüşte geçen süreyi hesapla!
                if (_velocity.y < 0) 
                {
                    _fallTimer += Time.deltaTime;
                }
            }
            else
            {
                // Eğer SADECE aşağı doğru 0.8 saniyeden fazla düştüyse (yüksekten düştüyse) ragdoll çalıştır
                if (!wasGrounded && _fallTimer > 0.8f)
                {
                    StartCoroutine(HardLandingRoutine());
                }
                _fallTimer = 0f;
            }

            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }

            // Zıplama tuşuna basıldığını hafızaya al (0.2 saniye boyunca hatırla)
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && !IsExhausted.Value && !IsKnockedDown.Value)
            {
                _jumpBufferTimer = 0.2f;
            }
            else if (_jumpBufferTimer > 0)
            {
                _jumpBufferTimer -= Time.deltaTime;
            }

            // Hafızada zıplama komutu varsa ve yere değiyorsa anında zıpla (Sadece yerde ve ayaktaysa)
            if (_jumpBufferTimer > 0 && _isGrounded && !IsExhausted.Value && !IsKnockedDown.Value)
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                _jumpBufferTimer = 0f; 
            }

            // Havada süzülme (Jetpack/Boost Jump) mekaniği (Sadece ayaktaysa)
            if (!_isGrounded && IsSprinting.Value && !IsExhausted.Value && !IsKnockedDown.Value)
            {
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

                    // Vuruş yönü: Karakterden topa doğru yatay
                    Vector3 forceDirection = hit.gameObject.transform.position - transform.position;
                    forceDirection.y = 0.1f; // Dribbling hep yerden gider
                    forceDirection.Normalize();

                    // Eskisi gibi sert vurmak yerine, sadece önünde sürüklüyor (Dribbling)
                    // Gerçek vuruşlar artık Mouse Sol ve Sağ tık ile yapılıyor!
                    float finalForce = IsSprinting.Value ? _dribbleForce * 1.5f : _dribbleForce;

                    // Sunucuya topa vurmasını söyle
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
        private System.Collections.IEnumerator HardLandingRoutine()
        {
            // Spawn koruması aktifse yüksekten düşme çalışmasın!
            if (_spawnGraceTimer > 0f)
            {
                Debug.Log("[Gameplay] [PlayerController.HardLandingRoutine] -> Spawn koruması aktif, Hard Landing iptal edildi.");
                yield break;
            }

            IsKnockedDown.Value = true;
            SetRagdollState(true);
            
            if (Camera.main != null)
            {
                var camFollow = Camera.main.GetComponent<PlayerCameraFollow>();
                if (camFollow != null) camFollow.TriggerShake(0.3f, 0.2f); // Daha hafif shake
            }
            
            Debug.Log("[Gameplay] Yüksekten düşüldü, karakter yere çakıldı!");
            
            yield return new WaitForSeconds(1.5f); // 1.5 saniye yerde kal
            
            if (!IsExhausted.Value) 
            {
                SetRagdollState(false);
            }
            
            IsKnockedDown.Value = false;
        }

        [ClientRpc]
        public void TargetTeleportClientRpc(Vector3 position, Quaternion rotation, ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner) return;
            
            Debug.Log($"[Gameplay] [PlayerController.TargetTeleportClientRpc] -> Işınlanma tetiklendi. (Hedef: {position})");

            // Işınlanırken stamina ve exhaust durumu sıfırlanmalıdır
            IsExhausted.Value = false;
            SetRagdollState(false);

            // Işınlanma sonrası spawn korumasını yeniden başlat
            _spawnGraceTimer = 2f;
            _fallTimer = 0f;

            // CharacterController, transform.position atamalarını engeller, bu yüzden kapat-ata-aç yapmalıyız
            if (_characterController != null)
            {
                _characterController.enabled = false;
                transform.position = position;
                transform.rotation = rotation;
                _characterController.enabled = true;
                Debug.Log("[Gameplay] [PlayerController.TargetTeleportClientRpc] -> Işınlanma tamamlandı. (Başarı: True)");
            }
            else
            {
                Debug.LogWarning("[Gameplay] [PlayerController.TargetTeleportClientRpc] -> Başarısız: CharacterController bulunamadı!");
            }

            Debug.Log($"[Gameplay] [PlayerController] -> Sunucu komutuyla Spawn noktasına ışınlanıldı: {position}");
        }
    }
}
