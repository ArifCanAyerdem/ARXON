using UnityEngine;
using Unity.Netcode;

namespace Arixon.Gameplay
{
    public class PlayerCameraFollow : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform _target;
        
        [Header("Camera Settings")]
        [SerializeField] private float _distance = 7f;
        [SerializeField] private float _height = 4f;
        [SerializeField] private float _smoothSpeed = 10f;
        
        private float _currentX = 0f;
        private float _currentY = 15f; // Başlangıç açısı
        
        [Header("Mouse Settings")]
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _minY = -20f;
        [SerializeField] private float _maxY = 60f;

        private void LateUpdate()
        {
            if (_target == null) return;

            // Scene'e göre kontrol tipini belirle
            bool isGameScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameScene";

            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                // Eğer oyundaysak (GameScene) kamerayı hep farenin yönüne çevir.
                bool shouldRotateCamera = isGameScene && _target != null;

                if (shouldRotateCamera)
                {
                    // Kamerayı kontrol ederken farenin ekrandan çıkmaması için kilitle
                    if (Cursor.lockState != CursorLockMode.Locked)
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                    
                    Vector2 mouseDelta = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();
                    _currentX += mouseDelta.x * _mouseSensitivity * 0.1f;
                    _currentY -= mouseDelta.y * _mouseSensitivity * 0.1f;
                    _currentY = Mathf.Clamp(_currentY, _minY, _maxY);
                }
                else
                {
                    // Kamerayı kontrol etmiyorsak (Örn: Lobide sağ tık bırakıldıysa) imleci göster
                    if (Cursor.lockState != CursorLockMode.None)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                }
            }

            // Rotasyonu hesapla
            Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0);
            
            // Kamera pozisyonunu hedefin arkasına yerleştir
            Vector3 lookAtPoint = _target.position + (Vector3.up * _height);
            Vector3 desiredPosition = lookAtPoint - (rotation * Vector3.forward * _distance);
            
            // Kamerayı uygula
            transform.position = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.deltaTime);
            transform.LookAt(lookAtPoint);
        }

        public void SetTarget(Transform newTarget)
        {
            _target = newTarget;
            
            if (_target != null)
            {
                Debug.Log($"[Gameplay] [PlayerCameraFollow.SetTarget] -> Kamera hedefi ayarlandı. (Hedef: {_target.name})");
                
                // (Kursor kilitleme işlemi LateUpdate içinde yapılıyor)
                
                // Başlangıç rotasyonunu hedefin arkasına göre ayarla
                _currentX = _target.eulerAngles.y;
                
                Vector3 lookAtPoint = _target.position + (Vector3.up * _height);
                Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0);
                transform.position = lookAtPoint - (rotation * Vector3.forward * _distance);
                transform.LookAt(lookAtPoint);
            }
            else
            {
                Debug.LogWarning("[Gameplay] [PlayerCameraFollow.SetTarget] -> Hedef NULL olarak ayarlandı! Kamera takibi duracak.");
            }
        }
    }
}
