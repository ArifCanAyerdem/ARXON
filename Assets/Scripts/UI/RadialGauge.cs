using UnityEngine;
using UnityEngine.UIElements;

namespace Arixon.UI
{
    public class RadialGauge : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<RadialGauge, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlFloatAttributeDescription m_Progress = new UxmlFloatAttributeDescription { name = "progress", defaultValue = 100f };
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((RadialGauge)ve).Progress = m_Progress.GetValueFromBag(bag, cc);
            }
        }

        private float _progress = 100f;
        public float Progress
        {
            get => _progress;
            set
            {
                if (Mathf.Abs(_progress - value) > 0.1f)
                {
                    _progress = Mathf.Clamp(value, 0, 100);
                    MarkDirtyRepaint();
                }
            }
        }

        public RadialGauge()
        {
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            float width = layout.width;
            float height = layout.height;
            if (width < 0.1f || height < 0.1f) return;

            float radius = Mathf.Min(width, height) / 2f;
            float thickness = radius * 0.18f; // Rocket League tarzı biraz daha zarif
            Vector2 center = new Vector2(width / 2f, height / 2f);

            // Renkleri Dinamik Ayarla
            Color gaugeColor = new Color(0f, 1f, 1f, 1f); // Neon Cyan
            if (_progress <= 25f) gaugeColor = new Color(1f, 0.1f, 0.2f, 1f); // Red
            else if (_progress <= 50f) gaugeColor = new Color(1f, 0.6f, 0f, 1f); // Orange

            // Arka plan (boş) gauge çizimi
            DrawArc(mgc, center, radius, thickness, 360f, new Color(0.1f, 0.1f, 0.15f, 0.8f));

            // Dolu gauge çizimi
            float angle = (_progress / 100f) * 360f;
            if (angle > 0)
            {
                DrawArc(mgc, center, radius, thickness, angle, gaugeColor);
            }
        }

        private void DrawArc(MeshGenerationContext mgc, Vector2 center, float radius, float thickness, float angleDegrees, Color color)
        {
            int segments = Mathf.CeilToInt(angleDegrees / 2f); // Her 2 derecede 1 segment
            if (segments < 1) return;

            float innerRadius = radius - thickness;
            var mesh = mgc.Allocate(segments * 4, segments * 6);

            // Roket League gibi alttan değil, sağdan veya üstten başlatabiliriz
            // Biz üstten başlatalım (saat yönünde)
            float startAngle = -90f; 
            float angleStep = angleDegrees / segments;

            for (int i = 0; i < segments; i++)
            {
                float currentAngle = (startAngle + (i * angleStep)) * Mathf.Deg2Rad;
                float nextAngle = (startAngle + ((i + 1) * angleStep)) * Mathf.Deg2Rad;

                Vector2 p0 = center + new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle)) * innerRadius;
                Vector2 p1 = center + new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle)) * radius;
                Vector2 p2 = center + new Vector2(Mathf.Cos(nextAngle), Mathf.Sin(nextAngle)) * radius;
                Vector2 p3 = center + new Vector2(Mathf.Cos(nextAngle), Mathf.Sin(nextAngle)) * innerRadius;

                int startVert = i * 4;
                mesh.SetNextVertex(new Vertex { position = new Vector3(p0.x, p0.y, Vertex.nearZ), tint = color });
                mesh.SetNextVertex(new Vertex { position = new Vector3(p1.x, p1.y, Vertex.nearZ), tint = color });
                mesh.SetNextVertex(new Vertex { position = new Vector3(p2.x, p2.y, Vertex.nearZ), tint = color });
                mesh.SetNextVertex(new Vertex { position = new Vector3(p3.x, p3.y, Vertex.nearZ), tint = color });

                mesh.SetNextIndex((ushort)(startVert));
                mesh.SetNextIndex((ushort)(startVert + 1));
                mesh.SetNextIndex((ushort)(startVert + 2));
                mesh.SetNextIndex((ushort)(startVert));
                mesh.SetNextIndex((ushort)(startVert + 2));
                mesh.SetNextIndex((ushort)(startVert + 3));
            }
        }
    }
}
