using UnityEngine;

namespace Tycoon3D.Interaction
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float reach = 3f;

        public string CurrentPrompt { get; private set; }

        private void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            CurrentPrompt = string.Empty;
            if (viewCamera == null || Cursor.lockState != CursorLockMode.Locked) return;
            if (!Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward,
                    out RaycastHit hit, reach, ~0, QueryTriggerInteraction.Ignore)) return;

            IInteractable target = null;
            foreach (MonoBehaviour component in hit.collider.GetComponentsInParent<MonoBehaviour>())
            {
                if (component is IInteractable interactable) { target = interactable; break; }
            }
            if (target == null) return;
            CurrentPrompt = target.Prompt;
            if (Input.GetKeyDown(InputBindings.Interact)) target.Interact();
            if (Input.GetKeyDown(InputBindings.Secondary) && target is ISecondaryInteractable secondary) secondary.InteractSecondary();
        }
    }
}
