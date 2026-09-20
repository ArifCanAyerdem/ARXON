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
        private TrailRenderer _trailRenderer;
        
        // Gol olduğunda doğru kişiye yazılabilmesi için (Own Goal mantığı)
        public ulong LastTouchedPlayerId { get; private set; } = 999;
        public ulong LastTouchedBluePlayerId { get; private set; } = 999;
        public ulong LastTouchedRedPlayerId { get; private set; } = 999;
        public ulong LastPasserBlueId { get; private set; } = 999;
        public ulong LastPasserRedId { get; private set; } = 999;

        private LineRenderer _dropIndicator;

        private void Awake()
        {
            // Ölçeklendirme (Daha görünür büyük top)
            transform.localScale = Vector3.one * 1.5f; // %50 büyüt

            _rb = GetComponent<Rigidbody>();
            
            // Rocket League tarzı top fizikleri:
            _rb.mass = 0.5f; // Daha hafif
            _rb.linearDamping = 0.5f; // Havada süzülme sürtünmesi (eski adıyla drag)
            _rb.angularDamping = 0.5f; // Dönme sürtünmesi
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Hızlı vuruşlarda içinden geçmemesi için

            // Zıplama için PhysicMaterial
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                PhysicsMaterial bouncyMat = new PhysicsMaterial("BallBouncyMat");
                bouncyMat.bounciness = 0.8f; // Çok zıplayan bir top
                bouncyMat.bounceCombine = PhysicsMaterialCombine.Maximum; // En yüksek sekme değerini al
                bouncyMat.dynamicFriction = 0.2f; // Kaygan
                bouncyMat.staticFriction = 0.2f;
                bouncyMat.frictionCombine = PhysicsMaterialCombine.Minimum;
                col.material = bouncyMat;
            }

            // Hız izi (Trail) kurulumu
            _trailRenderer = GetComponent<TrailRenderer>();
            if (_trailRenderer == null)
            {
                _trailRenderer = gameObject.AddComponent<TrailRenderer>();
            }
            _trailRenderer.time = 0.4f;
            _trailRenderer.startWidth = 0.6f;
            _trailRenderer.endWidth = 0f;
            Shader trailShader = Shader.Find("Universal Render Pipeline/Lit");
            if (trailShader == null) trailShader = Shader.Find("Standard");
            if (trailShader == null) trailShader = Shader.Find("Hidden/InternalErrorShader");
            
            _trailRenderer.material = new Material(trailShader);
            _trailRenderer.material.color = Color.white;
            _trailRenderer.emitting = false;

            CreateDropIndicator();
        }

        private void CreateDropIndicator()
        {
            GameObject indicatorObj = new GameObject("DropIndicator");
            // Top döndükçe LineRenderer yamulmasın diye topun çocuk objesi yapmıyoruz.
            
            _dropIndicator = indicatorObj.AddComponent<LineRenderer>();
            int segments = 36;
            _dropIndicator.positionCount = segments + 1;
            _dropIndicator.useWorldSpace = true;
            _dropIndicator.startWidth = 0.15f;
            _dropIndicator.endWidth = 0.15f;
            _dropIndicator.loop = true;
            _dropIndicator.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
            _dropIndicator.material = new Material(Shader.Find("Sprites/Default"));
            _dropIndicator.startColor = new Color(0, 0, 0, 0.5f);
            _dropIndicator.endColor = new Color(0, 0, 0, 0.5f);
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

        private void Update()
        {
            if (_trailRenderer != null && _rb != null)
            {
                // Hız 15'ten büyükse iz bırak
                _trailRenderer.emitting = _rb.linearVelocity.magnitude > 15f;
            }

            UpdateDropIndicator();
        }

        private void UpdateDropIndicator()
        {
            if (_dropIndicator == null) return;

            // Topun tam altındaki zemini bul (Topun kendi colliderını yok sayması için biraz alttan başlatıyoruz)
            Vector3 rayStart = transform.position + Vector3.down * 0.5f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                float height = transform.position.y - hit.point.y;
                
                // Eğer top çok yüksekteyse göstergeyi belirginleştir, yerdeyse kapat
                if (height > 1f)
                {
                    _dropIndicator.enabled = true;
                    
                    float radius = 1.5f; // Topun büyüklüğüne yakın bir gölge
                    float angle = 0f;
                    
                    for (int i = 0; i < _dropIndicator.positionCount; i++)
                    {
                        float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
                        float z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
                        
                        // Halkanın zeminin azıcık üstünde durması (Z-Fighting önlemek için)
                        _dropIndicator.SetPosition(i, hit.point + new Vector3(x, 0.05f, z)); 
                        angle += (360f / (_dropIndicator.positionCount - 1));
                    }

                    // Yükseğe çıktıkça hafifçe saydamlaşsın
                    float alpha = Mathf.Clamp01(1f - (height / 30f)) * 0.7f;
                    _dropIndicator.startColor = new Color(0, 0, 0, alpha);
                    _dropIndicator.endColor = new Color(0, 0, 0, alpha);
                }
                else
                {
                    _dropIndicator.enabled = false;
                }
            }
            else
            {
                _dropIndicator.enabled = false;
            }
        }

        private void ProcessTouch(ulong hitterId)
        {
            if (!IsServer || MatchManager.Instance == null || MatchManager.Instance.CurrentState.Value != MatchState.Playing) return;

            PlayerController pc = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(hitterId)?.GetComponent<PlayerController>();
            if (pc == null) return;

            ulong previousHitter = LastTouchedPlayerId;
            int teamId = pc.TeamColorID.Value;

            if (previousHitter != 999 && previousHitter != hitterId)
            {
                PlayerController prevPc = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(previousHitter)?.GetComponent<PlayerController>();
                if (prevPc != null)
                {
                    if (prevPc.TeamColorID.Value == teamId)
                    {
                        // PAS: Aynı takımdan başka birine değdi
                        MatchManager.Instance.RegisterPass(previousHitter);
                        Debug.Log($"[Gameplay] [GameBall.ProcessTouch] -> PAS gerçekleşti! (Tutan: {hitterId}, PasVeren: {previousHitter}, Takım: {teamId})");
                        if (teamId == 1) LastPasserBlueId = previousHitter;
                        else if (teamId == 2) LastPasserRedId = previousHitter;
                    }
                    else
                    {
                        // ARAYA GİRME (INTERCEPTION): Rakip takım araya girdi, pas zinciri kırıldı!
                        MatchManager.Instance.RegisterInterception(hitterId);
                        Debug.Log($"[Gameplay] [GameBall.ProcessTouch] -> ARAYA GİRME (INTERCEPT)! (ArayaGiren: {hitterId}, PasıKesilen: {previousHitter}, YeniTakım: {teamId})");
                        if (teamId == 1) LastPasserBlueId = 999; 
                        else if (teamId == 2) LastPasserRedId = 999;
                    }
                }
            }

            LastTouchedPlayerId = hitterId;
            if (teamId == 1) LastTouchedBluePlayerId = hitterId;
            else if (teamId == 2) LastTouchedRedPlayerId = hitterId;

            CheckForSave(pc);
        }

        private void CheckForSave(PlayerController pc)
        {
            // KURTARIŞ (SAVE): Top kendi kalene çok yakınsa ve vurduysan
            GoalTrigger[] goals = FindObjectsByType<GoalTrigger>(FindObjectsSortMode.None);
            foreach (var goal in goals)
            {
                if (goal.TeamId == pc.TeamColorID.Value)
                {
                    float dist = Vector3.Distance(transform.position, goal.transform.position);
                    if (dist < 15f) // Tehlike bölgesi (15 metre)
                    {
                        MatchManager.Instance.RegisterSave(pc.OwnerClientId);
                        Debug.Log($"[Gameplay] [GameBall.CheckForSave] -> KURTARIŞ (SAVE)! (Oyuncu: {pc.OwnerClientId}, KaleUzaklığı: {dist:F1}m)");
                    }
                }
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
                    ProcessTouch(pc.OwnerClientId);
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
                ProcessTouch(hitterId);
                _rb.AddForce(force, ForceMode.Impulse);
                Debug.Log($"[Gameplay] [GameBall.HitBall] -> Topa vuruldu! (Vuran: {hitterId}, UygulananGüç: {force}, Başarı: True)");
            }
            else
            {
                Debug.LogWarning($"[Gameplay] [GameBall.HitBall] -> Başarısız: Sadece sunucu topa güç uygulayabilir!");
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
                LastTouchedBluePlayerId = 999;
                LastTouchedRedPlayerId = 999;
                LastPasserBlueId = 999;
                LastPasserRedId = 999;
                Debug.Log("[Gameplay] [GameBall.ResetBall] -> Top merkeze sıfırlandı ve ilk vuruş için kilitlendi.");
            }
        }

        private void OnDestroy()
        {
            if (_dropIndicator != null)
            {
                Destroy(_dropIndicator.gameObject);
            }
        }
    }
}
