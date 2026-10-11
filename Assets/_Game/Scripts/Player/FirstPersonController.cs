using UnityEngine;

namespace Tycoon3D.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 6.8f;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundAcceleration = 18f;
        [SerializeField] private float airAcceleration = 5f;
        [SerializeField] private float jumpHeight = 1.05f;
        [SerializeField] private float headBobFrequency = 9f;
        [SerializeField] private float headBobAmplitude = .035f;
        [SerializeField] private float sprintFieldOfView = 76f;
        [SerializeField] private float fieldOfViewSharpness = 7f;

        private CharacterController controller;
        private float verticalSpeed;
        private float pitch;
        private float bobPhase;
        private float planarX, planarZ;
        private float baseFieldOfView = 60f;
        private Vector3 cameraRestPosition;
        private Vector3 spawnPosition;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (GetComponent<PlayerStatus>() == null) gameObject.AddComponent<PlayerStatus>();
            spawnPosition = transform.position;
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            if (viewCamera != null)
            {
                cameraRestPosition = viewCamera.transform.localPosition;
                baseFieldOfView = viewCamera.fieldOfView;
            }
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
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }
            else if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            bool inputActive = Cursor.lockState == CursorLockMode.Locked && viewCamera != null;
            if (inputActive)
            {
                transform.Rotate(Vector3.up, Input.GetAxisRaw("Mouse X") * mouseSensitivity);
                pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * mouseSensitivity, -85f, 85f);
                viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            float inputX = 0f, inputZ = 0f;
            if (inputActive)
                PlayerMotionMath.NormalizeInput(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), out inputX, out inputZ);
            Vector3 localDirection = new Vector3(inputX, 0f, inputZ);
            Vector3 targetVelocity = transform.TransformDirection(localDirection) *
                                     (Input.GetKey(KeyCode.LeftShift) && inputActive ? sprintSpeed : walkSpeed);

            bool grounded = controller.isGrounded;
            float acceleration = grounded ? groundAcceleration : airAcceleration;
            PlayerMotionMath.MovePlanar(planarX, planarZ, targetVelocity.x, targetVelocity.z,
                acceleration * Time.deltaTime, out planarX, out planarZ);

            if (grounded && verticalSpeed < 0f) verticalSpeed = -2f;
            if (grounded && inputActive && Input.GetKeyDown(KeyCode.Space))
                verticalSpeed = PlayerMotionMath.JumpSpeed(jumpHeight, -gravity);
            verticalSpeed += gravity * Time.deltaTime;
            controller.Move(new Vector3(planarX, verticalSpeed, planarZ) * Time.deltaTime);

            if (viewCamera != null)
            {
                float planarSpeed = Mathf.Sqrt(planarX * planarX + planarZ * planarZ);
                bool sprinting = inputActive && Input.GetKey(KeyCode.LeftShift) && planarSpeed > .15f;
                if (grounded && planarSpeed > .15f)
                    bobPhase += Time.deltaTime * headBobFrequency * (sprinting ? 1.28f : Mathf.Clamp(planarSpeed / walkSpeed, .65f, 1.1f));
                else bobPhase = 0f;
                float bob = grounded && planarSpeed > .15f ? PlayerMotionMath.BobOffset(bobPhase, headBobAmplitude * (sprinting ? 1.35f : 1f)) : 0f;
                Vector3 cameraTarget = cameraRestPosition + Vector3.up * bob + Vector3.right * (bob * .22f);
                float blend = 1f - Mathf.Exp(-12f * Time.deltaTime);
                viewCamera.transform.localPosition = Vector3.Lerp(viewCamera.transform.localPosition, cameraTarget, blend);
                float fovTarget = sprinting ? sprintFieldOfView : baseFieldOfView;
                viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, fovTarget, 1f - Mathf.Exp(-fieldOfViewSharpness * Time.deltaTime));
            }
        }
    }
}
