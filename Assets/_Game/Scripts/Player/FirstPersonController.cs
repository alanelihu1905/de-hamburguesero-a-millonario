using UnityEngine;

namespace Tycoon3D.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float gravity = -20f;

        private CharacterController controller;
        private float verticalSpeed;
        private float pitch;
        private Vector3 spawnPosition;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            spawnPosition = transform.position;
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>Orienta la vista (giro en grados, sin inclinación). Lo usan las pruebas en vivo para no depender del ratón.</summary>
        public void FaceDirection(float yawDegrees)
        {
            transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            pitch = 0f;
            if (viewCamera != null) viewCamera.transform.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            if (transform.position.y < -5f)
            {
                controller.enabled = false;
                transform.position = spawnPosition;
                controller.enabled = true;
                verticalSpeed = 0f;
            }
            if (Input.GetKeyDown(KeyCode.Escape) && !Tycoon3D.UI.SettingsMenu.Listening)
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }
            else if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0) && !Tycoon3D.UI.SettingsMenu.PointerInside(Input.mousePosition))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Cursor.lockState != CursorLockMode.Locked || viewCamera == null) return;

            transform.Rotate(Vector3.up, Input.GetAxisRaw("Mouse X") * mouseSensitivity);
            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * mouseSensitivity, -85f, 85f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            Vector3 direction = (transform.right * Input.GetAxisRaw("Horizontal") +
                                 transform.forward * Input.GetAxisRaw("Vertical")).normalized;
            if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            verticalSpeed += gravity * Time.deltaTime;
            controller.Move((direction * walkSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);
        }
    }
}
