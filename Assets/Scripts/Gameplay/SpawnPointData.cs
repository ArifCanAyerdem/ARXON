using UnityEngine;

namespace Arixon.Gameplay
{
    /// <summary>
    /// Bu script, arenadaki her bir doğma noktasını (Spawn Point) temsil eder.
    /// Takım ID'si üzerinden NetworkManager/MatchManager tarafından okunarak
    /// oyuncular maç başladığında doğru yerlere yerleştirilir.
    /// </summary>
    public class SpawnPointData : MonoBehaviour
    {
        [Tooltip("1 = Mavi Takım, 2 = Kırmızı Takım")]
        public int TeamID = 1;

        // Editörde noktaların rahat görünmesi için Gizmo çizelim
        private void OnDrawGizmos()
        {
            Gizmos.color = (TeamID == 1) ? Color.blue : Color.red;
            
            // Yerde bir tekerlek şeklinde doğma alanı
            Gizmos.DrawWireSphere(transform.position, 1f);
            
            // Oyuncunun baktığı yönü gösteren bir ok
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        }
    }
}
