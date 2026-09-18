using UnityEngine;
using UnityEngine.UIElements;

namespace Arixon.UI
{
    public class GhostEnergyBar : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<GhostEnergyBar, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlFloatAttributeDescription m_Progress = new UxmlFloatAttributeDescription { name = "progress", defaultValue = 100f };
            
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var bar = ve as GhostEnergyBar;
                bar.Progress = m_Progress.GetValueFromBag(bag, cc);
            }
        }

        private float _progress = 100f;
        private float _ghostProgress = 100f;
        private float _skewAngle = 20f; // Görseldeki gibi yatık (Paralelkenar) açısı
        
        // Görselinize tam uygun renkler:
        private Color _bgColor = new Color(0.35f, 0.1f, 0.15f, 1f); // Koyu kırmızı arka plan
        private Color _ghostColor = new Color(1f, 0.8f, 0.8f, 1f);  // Uçuk pembe/beyaz geriden gelen gölge
        private Color _mainColor = new Color(0.2f, 0.9f, 0.6f, 1f); // Neon Su Yeşili/Cyan ön bar

        public float Progress
        {
            get => _progress;
            set
            {
                _progress = Mathf.Clamp(value, 0f, 100f);
                MarkDirtyRepaint();
            }
        }

        public GhostEnergyBar()
        {
            generateVisualContent += OnGenerateVisualContent;
            
            // Gölgenin (Ghost) ana barı arkadan yavaşça takip etmesi için animasyon zamanlayıcısı
            schedule.Execute(UpdateGhostAnim).Every(16); // ~60 FPS
        }

        private void UpdateGhostAnim()
        {
            if (_ghostProgress > _progress)
            {
                _ghostProgress -= 0.6f; // Gölgenin erime hızı
                if (_ghostProgress < _progress) _ghostProgress = _progress;
                MarkDirtyRepaint();
            }
            else if (_ghostProgress < _progress)
            {
                _ghostProgress = _progress; // Stamina dolarken gölge beklemeye gerek yok, anında dolsun
                MarkDirtyRepaint();
            }
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            Rect r = contentRect;
            if (r.width < 1 || r.height < 1) return;

            // Eğiklik payı hesaplaması
            float skewOffset = Mathf.Tan(_skewAngle * Mathf.Deg2Rad) * r.height;
            float maxWidth = r.width - skewOffset;

            // 1. Katman: En alttaki Koyu Kırmızı Arka Plan
            DrawParallelogram(mgc, r, maxWidth, skewOffset, _bgColor);

            // 2. Katman: Geriden gelen Pembe/Beyaz Gölge
            float ghostWidth = maxWidth * (_ghostProgress / 100f);
            DrawParallelogram(mgc, r, ghostWidth, skewOffset, _ghostColor);

            // 3. Katman: En üstteki Neon Yeşil Ana Enerji Barı
            float mainWidth = maxWidth * (_progress / 100f);
            DrawParallelogram(mgc, r, mainWidth, skewOffset, _mainColor);
        }

        private void DrawParallelogram(MeshGenerationContext mgc, Rect rect, float width, float skewOffset, Color color)
        {
            if (width <= 0) return;

            // UI Toolkit Backface Culling (Görünmezlik) hatasını önlemek için 
            // üçgenler SAAT YÖNÜNDE (Clockwise) çizilmelidir.
            // P0: Sol Üst, P1: Sağ Üst, P2: Sağ Alt, P3: Sol Alt
            Vector2 p0 = new Vector2(rect.x + skewOffset, rect.yMin); 
            Vector2 p1 = new Vector2(rect.x + width + skewOffset, rect.yMin); 
            Vector2 p2 = new Vector2(rect.x + width, rect.yMax); 
            Vector2 p3 = new Vector2(rect.x, rect.yMax); 

            var meshWriteData = mgc.Allocate(4, 6);

            meshWriteData.SetNextVertex(new Vertex { position = new Vector3(p0.x, p0.y, Vertex.nearZ), tint = color });
            meshWriteData.SetNextVertex(new Vertex { position = new Vector3(p1.x, p1.y, Vertex.nearZ), tint = color });
            meshWriteData.SetNextVertex(new Vertex { position = new Vector3(p2.x, p2.y, Vertex.nearZ), tint = color });
            meshWriteData.SetNextVertex(new Vertex { position = new Vector3(p3.x, p3.y, Vertex.nearZ), tint = color });

            // Saat yönünde iki üçgen: (0, 1, 2) ve (2, 3, 0)
            meshWriteData.SetNextIndex(0);
            meshWriteData.SetNextIndex(1);
            meshWriteData.SetNextIndex(2);
            meshWriteData.SetNextIndex(2);
            meshWriteData.SetNextIndex(3);
            meshWriteData.SetNextIndex(0);
        }
    }
}
