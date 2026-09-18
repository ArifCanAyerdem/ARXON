using UnityEngine;
using Unity.Netcode;

namespace Arixon.Gameplay
{
    public class GoalTrigger : MonoBehaviour
    {
        [Tooltip("Bu kalenin kime ait olduğu. 1: Mavi, 2: Kırmızı")]
        [SerializeField] private int _teamId;
        public int TeamId => _teamId;
        
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

            Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = transform.InverseTransformVector(bounds.size);

            // --- 1. GOL ÇİZGİSİ (TRIGGER) OLUŞTURMA ---
            // Trigger mutlaka GoalTrigger scriptinin olduğu objede (PARENT) olmalıdır!
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
                triggerCol.center = localCenter;
                // Z eksenini (derinliği) yarıya düşürdüm ki kale direğine çarpar çarpmaz gol saymasın, içeri girsin.
                triggerCol.size = new Vector3(Mathf.Abs(localSize.x) * 0.85f, Mathf.Abs(localSize.y) * 0.85f, Mathf.Abs(localSize.z) * 0.5f);
            }

            // --- 2. GÖRSEL DÜZENLEME (ROCKET LEAGUE STİLİ NEON KALKAN) ---
            Color teamColor = _teamId == 1 ? new Color(0f, 0.8f, 1f, 0.35f) : new Color(1f, 0.1f, 0.3f, 0.35f);

            // Kalenin içine görsel olarak "İnce bir enerji duvarı" (ForceField) yerleştiriyoruz.
            // Çirkin durmaması için derinliği (Z) çok ince olacak. Sadece çizgiyi belirtecek.
            GameObject forceField = GameObject.CreatePrimitive(PrimitiveType.Cube);
            forceField.name = "Neon_ForceField";
            forceField.transform.SetParent(transform);
            forceField.transform.localPosition = localCenter;
            forceField.transform.localRotation = Quaternion.identity;
            forceField.transform.localScale = new Vector3(Mathf.Abs(localSize.x) * 0.95f, Mathf.Abs(localSize.y) * 0.95f, 0.2f); // Z ekseni sadece 0.2 kalınlıkta!

            // Fiziki olarak topu engellememesi için kalkanın collider'ını siliyoruz (Zaten trigger'ımız var)
            Destroy(forceField.GetComponent<Collider>());

            // Kusursuz çalışan Sprites/Default şeffaf materyali
            Material forceMat = new Material(Shader.Find("Sprites/Default"));
            forceMat.SetColor("_Color", teamColor);
            forceField.GetComponent<Renderer>().material = forceMat;

            // Rengin etrafa yayılması için ışık (Duvardaki mavilik/kırmızılık)
            Light goalLight = GetComponent<Light>();
            if (goalLight == null) goalLight = gameObject.AddComponent<Light>();
            
            goalLight.type = LightType.Point;
            goalLight.color = _teamId == 1 ? Color.cyan : Color.red;
            goalLight.range = 15f; 
            goalLight.intensity = 15f; 
            goalLight.transform.localPosition = localCenter; 
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Golleri sadece sunucu kontrol eder
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return; 

            // Optimizasyon: GetComponentInParent çok maliyetlidir. attachedRigidbody ile anında GameBall'ı buluruz.
            if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent(out GameBall ball))
            {
                ulong scorerId = ball.LastTouchedPlayerId;
                Debug.Log($"[Gameplay] [GoalTrigger] -> GOL! Takım {_teamId} kalesine gol atıldı! Son dokunan: {scorerId}");

                if (MatchManager.Instance != null)
                {
                    MatchManager.Instance.RegisterGoal(_teamId, ball);
                }
            }
        }
    }
}
