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
        [SerializeField] private float _positionSmoothTime = 0.05f; // Çok daha pürüzsüz pozisyon takibi
        [SerializeField] private float _rotationSmoothTime = 0.05f; // Çok daha pürüzsüz kamera dönüşü
        
        private float _currentX = 0f;
        private float _currentY = 15f; 
        
        private float _smoothX = 0f;
        private float _smoothY = 15f;
        private float _velX;
        private float _velY;
        private Vector3 _posVelocity;
        
        [Header("Shake Settings")]
        private float _shakeIntensity = 0f;
        private float _shakeDuration = 0f;
        private float _shakeTimer = 0f;
        
        [Header("Mouse Settings")]
        [SerializeField] private float _mouseSensitivity = 1.5f; // Hassasiyeti biraz kıstık ki soft olsun
        [SerializeField] private float _minY = -20f;
        [SerializeField] private float _maxY = 60f;
        
        [Header("Dynamic FOV")]
        [SerializeField] private float _normalFOV = 60f;
        [SerializeField] private float _sprintFOV = 75f;
        [SerializeField] private float _fovTransitionSpeed = 5f;

        private Camera _cam;
        private PlayerController _playerController;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            bool isGameScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu";

            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                bool shouldRotateCamera = isGameScene && _target != null;

                if (shouldRotateCamera)
                {
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
                    if (Cursor.lockState != CursorLockMode.None)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                }
            }

            // Pürüzsüz Rotasyon (SmoothDampAngle titremeleri tamamen yok eder)
            _smoothX = Mathf.SmoothDampAngle(_smoothX, _currentX, ref _velX, _rotationSmoothTime);
            _smoothY = Mathf.SmoothDampAngle(_smoothY, _currentY, ref _velY, _rotationSmoothTime);

            Quaternion rotation = Quaternion.Euler(_smoothY, _smoothX, 0);
            
            Vector3 lookAtPoint = _target.position + (Vector3.up * _height);
            Vector3 desiredPosition = lookAtPoint - (rotation * Vector3.forward * _distance);
            
            // Pozisyonu yumuşak takip et (Lerp yerine SmoothDamp çok daha pürüzsüzdür ve donma hissini keser)
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _posVelocity, _positionSmoothTime);
            transform.LookAt(lookAtPoint);

            // Dinamik FOV (Hız Hissiyatı)
            if (_cam != null && _playerController != null)
            {
                float targetFOV = _playerController.IsSprinting.Value ? _sprintFOV : _normalFOV;
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, Time.deltaTime * _fovTransitionSpeed);
            }

            // Sarsıntı (Shake) Efekti Uygula
            if (_shakeTimer > 0)
            {
                if (PlayerPrefs.GetInt("CameraShake", 1) == 1)
                {
                    transform.position += Random.insideUnitSphere * _shakeIntensity;
                }
                _shakeTimer -= Time.deltaTime;
                if (_shakeTimer <= 0)
                {
                    _shakeIntensity = 0f;
                }
            }
        }

        public void TriggerShake(float intensity, float duration)
        {
            _shakeIntensity = intensity;
            _shakeDuration = duration;
            _shakeTimer = duration;
        }

        public void SetTarget(Transform newTarget)
        {
            _target = newTarget;
            
            if (_target != null)
            {
                _playerController = _target.GetComponent<PlayerController>();
                Debug.Log($"[Gameplay] [PlayerCameraFollow.SetTarget] -> Kamera hedefi ayarlandı. (Hedef: {_target.name})");
                
                _currentX = _target.eulerAngles.y;
                _smoothX = _currentX;
                _currentY = 15f;
                _smoothY = _currentY;
                
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
