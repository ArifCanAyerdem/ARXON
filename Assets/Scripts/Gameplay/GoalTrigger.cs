using UnityEngine;
using Unity.Netcode;

namespace Arixon.Gameplay
{
    public class GoalTrigger : MonoBehaviour
    {
        [Tooltip("Bu kalenin kime ait olduğu. 1: Mavi, 2: Kırmızı")]
        [SerializeField] private int _teamId;
        
        private void Start()
        {
            // TeamId Kontrolü
            if (_teamId != 1 && _teamId != 2)
            {
                string objName = gameObject.name.ToLower();
                if (objName.Contains("blue") || objName.Contains("mavi") || objName.Contains("1")) _teamId = 1;
                else if (objName.Contains("red") || objName.Contains("kirmizi") || objName.Contains("kırmızı") || objName.Contains("2")) _teamId = 2;
                else _teamId = 1; 
            }

            // Kalenin sınırlarını bul (mesh üzerinden)
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            }
            else
            {
                bounds.size = new Vector3(10, 5, 5);
            }

            // --- 1. GOL ÇİZGİSİ (TRIGGER) OLUŞTURMA ---
            // Trigger mutlaka GoalTrigger scriptinin olduğu objede (PARENT) olmalıdır! Alt objede olursa OnTriggerEnter çalışmaz.
            Collider[] colliders = GetComponents<Collider>();
            bool hasTrigger = false;
            foreach (var c in colliders)
            {
                if (c.isTrigger) 
                {
                    hasTrigger = true; 
                    break;
                }
            }

            if (!hasTrigger)
            {
                BoxCollider triggerCol = gameObject.AddComponent<BoxCollider>();
                triggerCol.isTrigger = true;
                
                Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
                Vector3 localSize = transform.InverseTransformVector(bounds.size);
                
                triggerCol.center = localCenter;
                // Z eksenini (derinliği) yarıya düşürdüm ki kale direğine çarpar çarpmaz gol saymasın, içeri girsin.
                triggerCol.size = new Vector3(Mathf.Abs(localSize.x) * 0.85f, Mathf.Abs(localSize.y) * 0.85f, Mathf.Abs(localSize.z) * 0.5f);
            }

            // --- 2. GÖRSEL DÜZENLEME (ÇİRKİN KÜP YERİNE İSKELETİ BOYAMA) ---
            // Kalenin içini kapatan devasa küpü sildik. Onun yerine orijinal kalenin iskeletini havalı bir metale çevirip parlatıyoruz.
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            Material mat = urpLit != null ? new Material(urpLit) : new Material(Shader.Find("Standard"));

            // Mavi ve Kırmızı takımlar için koyu gövde ve parlayan neon kenarlar
            Color baseColor = _teamId == 1 ? new Color(0f, 0.2f, 0.4f) : new Color(0.4f, 0.1f, 0.1f);
            Color emissionColor = _teamId == 1 ? new Color(0f, 0.6f, 1f) * 1.5f : new Color(1f, 0.2f, 0.2f) * 1.5f;

            if (urpLit != null) mat.SetColor("_BaseColor", baseColor);
            else mat.SetColor("_Color", baseColor);

            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor);

            // Kalenin tüm orijinal iskeletine uygula
            foreach (var r in renderers)
            {
                // Eğer iskeletin render'ında çirkin bir child küp kaldıysa sil
                if (r.gameObject.name == "Neon_Goal_Field") 
                {
                    Destroy(r.gameObject);
                    continue;
                }
                r.material = mat;
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Golleri sadece sunucu kontrol eder
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return; 

            // Topun Collider'ı bir alt objede (örneğin mesh içinde) olabilir, bu yüzden Parent'ta GameBall arıyoruz.
            GameBall ball = other.GetComponentInParent<GameBall>();
            if (ball != null)
            {
                ulong scorerId = ball.LastTouchedPlayerId;
                Debug.Log($"[Gameplay] [GoalTrigger] -> GOL! Takım {_teamId} kalesine gol atıldı! Son dokunan: {scorerId}");
                
                // Skoru yaz
                if (MatchManager.Instance != null)
                {
                    MatchManager.Instance.RegisterGoal(_teamId, scorerId);
                }
            }
        }
    }
}
