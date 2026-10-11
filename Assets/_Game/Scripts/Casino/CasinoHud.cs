using Tycoon3D.Economy;
using UnityEngine;

namespace Tycoon3D.Casino
{
    /// <summary>
    /// HUD del Casino FOX (IMGUI, como el HUD de prototipo). Una etiqueta siempre visible indica que el casino está abierto y
    /// a qué distancia queda; cerca de una máquina, o justo tras girar, se abre el panel con los rodillos, las fichas de
    /// apuesta (1-4), el resultado y la tabla de premios. Accesibilidad: texto grande escalado a la pantalla, contraste alto,
    /// símbolos por forma además de color, y el resultado también se anuncia con sonido.
    /// </summary>
    public sealed class CasinoHud : MonoBehaviour
    {
        [SerializeField] private CasinoService service;
        [SerializeField] private Wallet wallet;
        [SerializeField] private Transform entrance;

        private float entranceDistance;
        private float entranceSide;
        private int lastScaleKey = -1;
        private GUIStyle title, body, big, small, chip, banner;
        private int walletBefore;
        private float walletChangeAt = -100f;
        private int walletDelta;
        private int lastWallet = -1;

        private static readonly Color Plum = new Color32(31, 25, 39, 178);
        private static readonly Color PlumLight = new Color32(69, 58, 79, 188);
        private static readonly Color Mango = new Color32(244, 201, 93, 255);
        private static readonly Color Cream = new Color32(247, 238, 221, 255);
        private static readonly Color Chile = new Color32(228, 87, 46, 255);
        private static readonly Color Teal = new Color32(43, 166, 160, 255);

        public void Configure(CasinoService casino, Wallet money, Transform door)
        {
            service = casino;
            wallet = money;
            entrance = door;
        }

        /// <summary>Diagnóstico: cuántas veces se dibujó el HUD (Repaint) y si el último fotograma pintó el panel.</summary>
        public static int RepaintCount { get; private set; }
        public static bool LastPanelDrawn { get; private set; }

        public bool PanelVisible => service != null && (service.NearestDistance < 5f || Time.time - service.LastActivityTime < 5f);

        private float Scale => Mathf.Clamp(Screen.height / 1080f, .64f, 1.25f);

        private void Update()
        {
            Camera cam = Camera.main;
            if (cam != null && entrance != null)
            {
                Vector3 d = entrance.position - cam.transform.position;
                d.y = 0;
                entranceDistance = d.magnitude;
                entranceSide = d.x;
            }
            if (wallet != null)
            {
                if (lastWallet >= 0 && wallet.Pesos != lastWallet) { walletDelta = wallet.Pesos - lastWallet; walletChangeAt = Time.time; }
                lastWallet = wallet.Pesos;
            }
        }

        private void Styles()
        {
            int key = Mathf.RoundToInt(Scale * 100);
            if (key == lastScaleKey && title != null) return;
            lastScaleKey = key;
            float s = Scale;
            title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(26 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = Mango;
            body = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(22 * s), alignment = TextAnchor.MiddleCenter, wordWrap = true };
            body.normal.textColor = Cream;
            big = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(40 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = Mango;
            small = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(18 * s), alignment = TextAnchor.MiddleCenter, wordWrap = true };
            small.normal.textColor = Cream;
            chip = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(22 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            banner = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(46 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }

        private static void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static void Frame(Rect r, Color fill, Color border, float w)
        {
            Fill(new Rect(r.x - w, r.y - w, r.width + 2 * w, r.height + 2 * w), border);
            Fill(r, fill);
        }

        private void OnGUI()
        {
            if (service == null) return;
            Styles();
            DrawTag();
            bool panel = PanelVisible;
            if (panel) DrawPanel();
            DrawToast();
            if (Event.current.type == EventType.Repaint) { RepaintCount++; LastPanelDrawn = panel; }
        }

        private void DrawTag()
        {
            float s = Scale, w = Mathf.Min(370f * s, Screen.width - 32f * s), h = 58f * s;
            var r = new Rect(Screen.width - w - 16f * s, 16f * s, w, h);
            Fill(r, Plum);
            Fill(new Rect(r.x, r.y, 8f * s, r.height), Mango);
            string where = entranceDistance < 3f ? "Estás en la puerta"
                : Mathf.RoundToInt(entranceDistance) + " m al " + (entranceSide >= 0 ? "este >>" : "<< oeste");
            title.fontSize = Mathf.RoundToInt(19 * s);
            GUI.Label(new Rect(r.x + 14f * s, r.y + 3f * s, w - 18f * s, 27f * s), "CASINO FOX · abierto siempre", title);
            title.fontSize = Mathf.RoundToInt(26 * s);
            GUI.Label(new Rect(r.x + 14f * s, r.y + 29f * s, w - 18f * s, 24f * s), where + " · pesos virtuales", small);
        }

        // aviso de la cajera o de una ronda liquidada al volver: arriba al centro, unos segundos
        private void DrawToast()
        {
            if (string.IsNullOrEmpty(service.ToastText) || Time.time > service.ToastUntil) return;
            float s = Scale, w = Mathf.Min(760f * s, Screen.width - 32f * s), h = 52f * s;
            var r = new Rect(Screen.width * .5f - w * .5f, 20f * s, w, h);
            Frame(r, Plum, Teal, 3f * s);
            body.fontSize = Mathf.RoundToInt(21 * s);
            GUI.Label(r, service.ToastText, body);
        }

        private void DrawSymbol(Rect r, SlotSymbol symbol)
        {
            Frame(r, Cream, Plum, 4f * Scale);
            var atlas = service.SymbolAtlas;
            if (atlas != null)
            {
                int cell = (int)symbol;
                GUI.DrawTextureWithTexCoords(r, atlas, new Rect(cell / 5f, .25f, 1f / 5f, .5f));
            }
        }

        private void DrawPanel()
        {
            float s = Scale;
            var session = service.Session;
            var machine = service.Spinning || Time.time - service.LastActivityTime < 5f
                ? (service.LastMachine != null ? service.LastMachine : service.NearestMachine)
                : service.NearestMachine;
            // panel compacto anclado abajo: no tapa la máquina que se está jugando
            float margin = 60f * s;
            float w = Mathf.Min(780f * s, Screen.width - 2f * margin);
            float h = Mathf.Min(350f * s, Screen.height - 2f * margin);
            var r = new Rect(Screen.width * .5f - w * .5f, Screen.height - h - margin, w, h);
            Frame(r, Plum, Mango, 4f * s);
            Fill(new Rect(r.x, r.yMax - 6f * s, w, 6f * s), Chile);

            title.fontSize = Mathf.RoundToInt(24 * s);
            string name = machine != null ? machine.DisplayName : "Tragamonedas FOX";
            GUI.Label(new Rect(r.x, r.y + 4f * s, w, 32f * s), name.ToUpperInvariant(), title);
            title.fontSize = Mathf.RoundToInt(26 * s);
            small.fontSize = Mathf.RoundToInt(17 * s);
            if (machine != null)
                GUI.Label(new Rect(r.x, r.y + 34f * s, w, 24f * s),
                    "Premio máximo x" + machine.Paytable.TopMultiplier + " · gana " + machine.Paytable.HitPercent + " de cada 100 giros", small);

            // rodillos (izquierda)
            float iw = 84f * s, gap = 12f * s, x0 = r.x + 28f * s, y0 = r.y + 66f * s;
            for (int i = 0; i < 3; i++)
                DrawSymbol(new Rect(x0 + i * (iw + gap), y0, iw, iw), service.DisplayReels[i]);

            // fichas de apuesta 2x2 (derecha)
            float cw = 136f * s, ch = 38f * s, cg = 10f * s, cx0 = r.xMax - 2 * cw - cg - 28f * s;
            chip.fontSize = Mathf.RoundToInt(20 * s);
            for (int i = 0; i < CasinoSession.BetTiers.Length; i++)
            {
                var rect = new Rect(cx0 + (i % 2) * (cw + cg), y0 + (i / 2) * (ch + cg + 2f * s), cw, ch);
                bool on = i == session.BetIndex && !session.NextSpinIsFree;
                Frame(rect, on ? Mango : PlumLight, on ? Cream : Mango, on ? 4f * s : 2f * s);
                chip.normal.textColor = on ? Plum : Cream;
                GUI.Label(rect, "[" + (i + 1) + "]  $" + CasinoSession.BetTiers[i], chip);
            }

            // saldo, entre los rodillos y las fichas
            int saldo = wallet != null ? wallet.Pesos : 0;
            string delta = Time.time - walletChangeAt < 3f && walletDelta != 0 ? (walletDelta > 0 ? "  +$" + walletDelta : "  -$" + (-walletDelta)) : "";
            float sx = x0 + 3 * iw + 2 * gap + 14f * s, sw = cx0 - sx - 10f * s;
            Frame(new Rect(sx, y0, sw, iw), PlumLight, Mango, 2f * s);
            small.fontSize = Mathf.RoundToInt(15 * s);
            GUI.Label(new Rect(sx, y0 + 4f * s, sw, 22f * s), "SALDO" + delta, small);
            title.fontSize = Mathf.RoundToInt(28 * s);
            GUI.Label(new Rect(sx, y0 + 22f * s, sw, 36f * s), "$" + saldo, title);
            small.fontSize = Mathf.RoundToInt(15 * s);
            small.normal.textColor = session.Chips > 0 ? Mango : Cream;
            GUI.Label(new Rect(sx, y0 + 58f * s, sw, 22f * s), session.Chips + (session.Chips == 1 ? " ficha" : " fichas"), small);
            small.normal.textColor = Cream;
            title.fontSize = Mathf.RoundToInt(26 * s);

            // resultado
            float ry = y0 + iw + 10f * s;
            var res = new Rect(r.x + 20f * s, ry, w - 40f * s, 48f * s);
            banner.fontSize = Mathf.RoundToInt(36 * s);
            if (service.Spinning)
            {
                banner.normal.textColor = Cream;
                GUI.Label(res, "Girando… apuesta $" + service.PendingStake, banner);
            }
            else if (service.LastRound.HasValue && Time.time - service.LastRevealTime < 6f)
            {
                var round = service.LastRound.Value;
                float t = Mathf.Clamp01((Time.time - service.LastRevealTime) / .7f);
                int shown = Mathf.RoundToInt(round.Payout * t);
                bool win = round.Payout > 0;
                banner.normal.textColor = win ? Mango : Cream;
                GUI.Label(res, win ? "¡GANASTE +$" + shown + "!" : "Sin premio", banner);
                body.fontSize = Mathf.RoundToInt(19 * s);
                GUI.Label(new Rect(res.x, res.yMax - 2f * s, res.width, 26f * s),
                    CasinoService.Describe(round).Replace(" +$" + round.Payout, "") + (round.FreeSpin ? "  (giro de cortesía)" : ""), body);
            }
            else
            {
                GUI.Label(res, session.NextSpinIsFree ? "Giro de cortesía: la casa invita $" + CasinoSession.MinBet : "Apuesta $" + session.CurrentBet, big_());
            }

            // tabla de premios compacta: tres iguales y el par
            if (machine != null)
            {
                var t = machine.Paytable;
                var filas = new[]
                {
                    (SlotSymbol.Fox, SlotTier.JackpotFox), (SlotSymbol.Hamburguesa, SlotTier.TrioHamburguesa),
                    (SlotSymbol.Mango, SlotTier.TrioMango), (SlotSymbol.Chile, SlotTier.TrioChile)
                };
                float ty = r.yMax - 104f * s, tw = (w - 40f * s) / 5f, ts = 30f * s;
                small.fontSize = Mathf.RoundToInt(15 * s);
                GUI.Label(new Rect(r.x, ty - 19f * s, w, 20f * s), "Tres iguales pagan:", small);
                chip.fontSize = Mathf.RoundToInt(19 * s);
                chip.normal.textColor = Cream;
                for (int i = 0; i < filas.Length; i++)
                {
                    int idx = System.Array.IndexOf(SlotMachineLogic.Tiers, filas[i].Item2);
                    int m = t.Weights[idx] == 0 ? 0 : t.Multipliers[idx];
                    var icon = new Rect(r.x + 24f * s + i * tw, ty, ts, ts);
                    DrawSymbol(icon, filas[i].Item1);
                    GUI.Label(new Rect(icon.xMax + 4f * s, ty, tw - ts - 8f * s, ts), m > 0 ? "x" + m : "—", chip);
                }
                GUI.Label(new Rect(r.x + 20f * s + 4 * tw, ty, tw, ts), "Par x" + t.MultiplierOf(SlotTier.Par), chip);
            }

            small.fontSize = Mathf.RoundToInt(16 * s);
            GUI.Label(new Rect(r.x + 10f * s, r.yMax - 52f * s, w - 20f * s, 24f * s),
                "E girar · 1-4 o rueda: apuesta · Q siguiente · F luces " + (service.ReduceFlashing ? "fijas" : "animadas"), small);
            small.fontSize = Mathf.RoundToInt(14 * s);
            GUI.Label(new Rect(r.x + 10f * s, r.yMax - 28f * s, w - 20f * s, 22f * s),
                "Libro del casino: " + session.RoundsStarted + " rondas · apostado $" + session.TotalWagered + " · premios $" + session.TotalWon
                + (session.FreeSpins > 0 ? " · cortesías " + session.FreeSpins : "") + " · 1 ficha = $" + CasinoSession.ChipValue, small);
        }

        private GUIStyle big_() { big.fontSize = Mathf.RoundToInt(34 * Scale); return big; }
    }
}
