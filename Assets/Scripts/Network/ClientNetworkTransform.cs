using Unity.Netcode.Components;
using UnityEngine;

namespace Arixon.Network
{
    /// <summary>
    /// Ağ üzerindeki objelerin (Özellikle Oyuncu Karakterlerinin) pozisyonunu 
    /// sunucudan (Server) değil, objenin sahibinden (Client Owner) alarak senkronize etmesini sağlar.
    /// Bu sayede istemcideki (Client) hareket anında (gecikmesiz) gerçekleşir.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        // NGO 1.x ve 2.x sürümlerinde Client-Authoritative hareket için temel kural:
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
