using Tycoon3D.Economy;
using Tycoon3D.Interaction;
using Tycoon3D.Restaurant;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tycoon3D.UI
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private Wallet wallet;
        [SerializeField] private BurgerLoop burgerLoop;
        [SerializeField] private PlayerInteractor interactor;

        private GUIStyle label;

        public void Configure(Wallet money, BurgerLoop loop, PlayerInteractor player)
        {
            wallet = money;
            burgerLoop = loop;
            interactor = player;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnGUI()
        {
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
                label.normal.textColor = Color.white;
            }

            GUI.Box(new Rect(16, 16, 530, 132), "PROTOTIPO - Barrio residencial");
            GUI.Label(new Rect(30, 43, 500, 25), "Dinero: $" + (wallet != null ? wallet.Pesos : 0) +
                      "    Ventas: " + (burgerLoop != null ? burgerLoop.Sales : 0), label);
            if (burgerLoop != null) GUI.Label(new Rect(30, 72, 500, 52), burgerLoop.Message, label);
            GUI.Label(new Rect(20, Screen.height - 40, 650, 32),
                "WASD mover · Ratón mirar · E interactuar · R reiniciar · Esc cursor", label);
            if (interactor != null && !string.IsNullOrEmpty(interactor.CurrentPrompt))
                GUI.Label(new Rect(Screen.width * .5f - 180, Screen.height * .65f, 400, 32),
                    interactor.CurrentPrompt, label);
            GUI.Label(new Rect(Screen.width * .5f - 5, Screen.height * .5f - 16, 20, 30), "+", label);
        }
    }
}
