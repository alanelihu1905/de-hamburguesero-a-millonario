using Tycoon3D.Economy;
using Tycoon3D.Interaction;
using Tycoon3D.Player;
using Tycoon3D.Restaurant;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tycoon3D.UI
{
    /// <summary>HUD diegético ligero: vitales, dinero, interacción contextual y teléfono con carga actual.</summary>
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private Wallet wallet;
        [SerializeField] private BurgerLoop burgerLoop;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private PlayerStatus playerStatus;

        private bool phoneOpen;
        private int styleScale = -1;
        private GUIStyle micro, label, value, promptStyle, keyStyle, phoneTitle;

        private static readonly Color Panel = new Color32(17, 22, 27, 126);
        private static readonly Color PanelDeep = new Color32(13, 17, 21, 184);
        private static readonly Color White = new Color32(246, 243, 234, 255);
        private static readonly Color Secondary = new Color32(194, 198, 197, 255);
        private static readonly Color Accent = new Color32(229, 117, 69, 255);
        private static readonly Color Green = new Color32(123, 197, 146, 255);
        private static readonly Color Red = new Color32(231, 91, 76, 255);

        public void Configure(Wallet money, BurgerLoop loop, PlayerInteractor player)
        {
            wallet = money;
            burgerLoop = loop;
            interactor = player;
            playerStatus = player != null ? player.GetComponent<PlayerStatus>() : null;
        }

        private void Awake()
        {
            if (wallet == null) wallet = FindFirstObjectByType<Wallet>();
            if (burgerLoop == null) burgerLoop = FindFirstObjectByType<BurgerLoop>();
            if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
            if (playerStatus == null && interactor != null) playerStatus = interactor.GetComponent<PlayerStatus>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            if (Input.GetKeyDown(KeyCode.M)) phoneOpen = !phoneOpen;
        }

        private float S => Mathf.Clamp(Screen.height / 900f, .78f, 1.25f);

        private void Styles()
        {
            int key = Mathf.RoundToInt(S * 100);
            if (key == styleScale && label != null) return;
            styleScale = key;
            micro = MakeStyle(11, FontStyle.Bold, TextAnchor.MiddleLeft, Secondary);
            label = MakeStyle(13, FontStyle.Normal, TextAnchor.MiddleLeft, Secondary);
            value = MakeStyle(19, FontStyle.Bold, TextAnchor.MiddleLeft, White);
            promptStyle = MakeStyle(16, FontStyle.Bold, TextAnchor.MiddleLeft, White);
            keyStyle = MakeStyle(15, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            phoneTitle = MakeStyle(19, FontStyle.Bold, TextAnchor.MiddleLeft, White);
            micro.fontSize = Mathf.RoundToInt(micro.fontSize * S);
            label.fontSize = Mathf.RoundToInt(label.fontSize * S);
            value.fontSize = Mathf.RoundToInt(value.fontSize * S);
            promptStyle.fontSize = Mathf.RoundToInt(promptStyle.fontSize * S);
            keyStyle.fontSize = Mathf.RoundToInt(keyStyle.fontSize * S);
            phoneTitle.fontSize = Mathf.RoundToInt(phoneTitle.fontSize * S);
        }

        private static GUIStyle MakeStyle(int size, FontStyle weight, TextAnchor anchor, Color color)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = weight,
                alignment = anchor,
                clipping = TextClipping.Clip,
                normal = { textColor = color }
            };
            return style;
        }

        private static void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static void Line(Rect r, Color c, float width)
        {
            Fill(new Rect(r.x, r.y, r.width, width), c);
            Fill(new Rect(r.x, r.yMax - width, r.width, width), c);
            Fill(new Rect(r.x, r.y, width, r.height), c);
            Fill(new Rect(r.xMax - width, r.y, width, r.height), c);
        }

        private void OnGUI()
        {
            Styles();
            DrawVitals();
            DrawWallet();
            DrawPhoneButton();
            DrawInteraction();
            if (phoneOpen) DrawPhone();
        }

        private void DrawVitals()
        {
            float s = S, margin = 24f * s, w = 236f * s, h = 54f * s;
            Rect r = new Rect(margin, Screen.height - margin - h, w, h);
            Fill(r, Panel);
            Line(r, new Color(1f, 1f, 1f, .12f), 1f * s);
            int hp = playerStatus != null ? playerStatus.Health : 100;
            int maxHp = playerStatus != null ? playerStatus.MaximumHealth : 100;
            float ratio = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
            Fill(new Rect(r.x + 12f * s, r.y + 11f * s, 4f * s, 31f * s), ratio < .3f ? Red : Accent);
            GUI.Label(new Rect(r.x + 24f * s, r.y + 8f * s, 95f * s, 18f * s), "SALUD", micro);
            value.fontSize = Mathf.RoundToInt(19 * s);
            GUI.Label(new Rect(r.x + 24f * s, r.y + 24f * s, 70f * s, 24f * s), hp.ToString(), value);
            GUI.Label(new Rect(r.x + 64f * s, r.y + 27f * s, 54f * s, 18f * s), "/ " + maxHp, label);
            Fill(new Rect(r.x + 125f * s, r.y + 25f * s, 96f * s, 5f * s), new Color(1f, 1f, 1f, .16f));
            Fill(new Rect(r.x + 125f * s, r.y + 25f * s, 96f * s * ratio, 5f * s), ratio < .3f ? Red : Green);
        }

        private void DrawWallet()
        {
            float s = S, margin = 24f * s, w = 154f * s, h = 54f * s;
            Rect r = new Rect(Screen.width - margin - w, Screen.height - margin - h, w, h);
            Fill(r, Panel);
            Line(r, new Color(1f, 1f, 1f, .12f), 1f * s);
            GUI.Label(new Rect(r.x + 14f * s, r.y + 8f * s, w - 24f * s, 17f * s), "EFECTIVO", micro);
            value.fontSize = Mathf.RoundToInt(20 * s);
            GUI.Label(new Rect(r.x + 14f * s, r.y + 25f * s, w - 24f * s, 23f * s), "$" + (wallet != null ? wallet.Pesos : 0).ToString("N0"), value);
        }

        private void DrawPhoneButton()
        {
            float s = S, size = 40f * s, x = 24f * s, y = 22f * s;
            Rect r = new Rect(x, y, size, size);
            Fill(r, phoneOpen ? Accent : PanelDeep);
            Line(r, new Color(1f, 1f, 1f, .22f), 1f * s);
            Rect phone = new Rect(r.x + 13f * s, r.y + 7f * s, 14f * s, 25f * s);
            Line(phone, White, 1.5f * s);
            Fill(new Rect(phone.x + 4f * s, phone.y + 3f * s, 6f * s, 1f * s), White);
            Fill(new Rect(phone.x + 5f * s, phone.yMax - 4f * s, 4f * s, 1f * s), White);
            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) phoneOpen = !phoneOpen;
        }

        private void DrawInteraction()
        {
            if (interactor == null || string.IsNullOrWhiteSpace(interactor.CurrentPrompt)) return;
            float s = S, w = Mathf.Min(460f * s, Screen.width - 40f * s), h = 46f * s;
            Rect r = new Rect((Screen.width - w) * .5f, Screen.height * .71f, w, h);
            Fill(r, PanelDeep);
            Fill(new Rect(r.x, r.y, 3f * s, h), Accent);
            string text = interactor.CurrentPrompt.Trim();
            string key = "E";
            int split = text.IndexOf("E - ");
            if (split < 0) { split = text.IndexOf("Q - "); key = "Q"; }
            if (split >= 0) text = text.Substring(split + 4).Trim();
            Rect keyRect = new Rect(r.x + 12f * s, r.y + 8f * s, 34f * s, 30f * s);
            Fill(keyRect, Accent);
            GUI.Label(keyRect, key, keyStyle);
            GUI.Label(new Rect(keyRect.xMax + 10f * s, r.y + 4f * s, w - 66f * s, h - 8f * s), text, promptStyle);
        }

        private void DrawPhone()
        {
            float s = S, w = Mathf.Min(340f * s, Screen.width - 36f * s), h = Mathf.Min(430f * s, Screen.height - 42f * s);
            Rect r = new Rect(Screen.width - w - 24f * s, 72f * s, w, h);
            Fill(r, PanelDeep);
            Line(r, new Color(1f, 1f, 1f, .24f), 1f * s);
            Fill(new Rect(r.x, r.y, 3f * s, r.height), Accent);
            GUI.Label(new Rect(r.x + 18f * s, r.y + 14f * s, w - 55f * s, 28f * s), "TELÉFONO", phoneTitle);
            GUI.Label(new Rect(r.xMax - 38f * s, r.y + 13f * s, 28f * s, 28f * s), "×", phoneTitle);
            GUI.Label(new Rect(r.x + 18f * s, r.y + 52f * s, w - 32f * s, 20f * s), "BILLETERA", micro);
            GUI.Label(new Rect(r.x + 18f * s, r.y + 73f * s, w - 32f * s, 34f * s), "$" + (wallet != null ? wallet.Pesos : 0).ToString("N0"), value);
            Fill(new Rect(r.x + 16f * s, r.y + 117f * s, w - 32f * s, 1f * s), new Color(1f, 1f, 1f, .14f));

            GUI.Label(new Rect(r.x + 18f * s, r.y + 130f * s, w - 32f * s, 21f * s), "ESTADO", micro);
            int hp = playerStatus != null ? playerStatus.Health : 100;
            int maxHp = playerStatus != null ? playerStatus.MaximumHealth : 100;
            int heat = playerStatus != null ? playerStatus.Heat : 0;
            int cargo = playerStatus != null ? playerStatus.ContrabandPackages : 0;
            StatRow(r, 158f * s, "Salud", hp + " / " + maxHp, new Color32(123, 197, 146, 255));
            StatRow(r, 192f * s, "Atención", heat + " %", heat > 60 ? Red : Secondary);
            StatRow(r, 226f * s, "Paquetes", cargo.ToString(), cargo > 0 ? Accent : Secondary);

            Fill(new Rect(r.x + 16f * s, r.y + 264f * s, w - 32f * s, 1f * s), new Color(1f, 1f, 1f, .14f));
            GUI.Label(new Rect(r.x + 18f * s, r.y + 277f * s, w - 32f * s, 22f * s), "BOLSA", micro);
            string item = "Vacía";
            Color itemColor = Secondary;
            if (burgerLoop != null && burgerLoop.Step == BurgerStep.NeedCooking) { item = "Ingredientes   ×1"; itemColor = White; }
            else if (burgerLoop != null && burgerLoop.Step == BurgerStep.ReadyToSell) { item = "Hamburguesa   ×1"; itemColor = White; }
            if (cargo > 0) item += "\nContrabando   ×" + cargo;
            label.normal.textColor = itemColor;
            GUI.Label(new Rect(r.x + 18f * s, r.y + 307f * s, w - 36f * s, Mathf.Max(40f * s, h - 320f * s)), item, label);
            GUI.Label(new Rect(r.xMax - 78f * s, r.y + 14f * s, 62f * s, 24f * s), "CERRAR", micro);
            if (GUI.Button(new Rect(r.xMax - 82f * s, r.y + 10f * s, 72f * s, 32f * s), GUIContent.none, GUIStyle.none)) phoneOpen = false;
        }

        private void StatRow(Rect panel, float y, string name, string amount, Color color)
        {
            float s = S, w = panel.width;
            label.normal.textColor = Secondary;
            GUI.Label(new Rect(panel.x + 18f * s, panel.y + y, w * .58f, 24f * s), name, label);
            label.normal.textColor = color;
            label.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(panel.x + w * .58f, panel.y + y, w * .36f, 24f * s), amount, label);
            label.alignment = TextAnchor.MiddleLeft;
        }
    }
}
