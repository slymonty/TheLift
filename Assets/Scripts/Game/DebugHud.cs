using UnityEngine;

namespace TheLift.Game
{
    public class DebugHud : MonoBehaviour
    {
        [SerializeField] private FighterController fighterA;
        [SerializeField] private FighterController fighterB;

        private Texture2D _barTexture;

        private void Awake()
        {
            _barTexture = new Texture2D(1, 1);
            _barTexture.SetPixel(0, 0, Color.white);
            _barTexture.Apply();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 680, 320));
            GUILayout.BeginHorizontal();

            DrawColumn(fighterA);
            GUILayout.Space(20);
            DrawColumn(fighterB);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawColumn(FighterController controller)
        {
            GUILayout.BeginVertical(GUILayout.Width(320));

            if (controller == null || controller.Fighter == null)
            {
                GUILayout.Label("(unassigned)");
                GUILayout.EndVertical();
                return;
            }

            var fighter = controller.Fighter;

            var headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 };
            GUILayout.Label($"{controller.name} — {controller.Archetype}", headerStyle);

            DrawBar("Stamina", fighter.Stamina, fighter.Body.MaxStamina);
            DrawBar("Composure", fighter.Composure, fighter.Body.MaxComposure);
            DrawBar("Balance", fighter.Balance, 100f);
            DrawBar("Rattled", fighter.Rattled, 100f);
            DrawBar("Adrenaline", fighter.Adrenaline, 100f);

            GUILayout.Space(8);
            GUILayout.Label($"IsExhausted: {fighter.IsExhausted}");
            GUILayout.Label($"AdrenalineState: {fighter.AdrenalineState}");
            GUILayout.Label($"RattledState: {fighter.RattledState}");

            GUILayout.Space(8);
            GUILayout.Label($"ActionPhase: {fighter.ActionPhase}");
            GUILayout.Label($"CurrentActionType: {fighter.CurrentActionType}");
            GUILayout.Label($"LightChainCount: {fighter.LightChainCount}");

            GUILayout.EndVertical();
        }

        private void DrawBar(string label, float value, float max)
        {
            float fraction = max > 0f ? Mathf.Clamp01(value / max) : 0f;

            GUILayout.Label($"{label}: {value:F1} / {max:F1}");

            Rect rect = GUILayoutUtility.GetRect(280, 16);
            GUI.color = Color.gray;
            GUI.DrawTexture(rect, _barTexture);
            GUI.color = Color.green;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * fraction, rect.height), _barTexture);
            GUI.color = Color.white;
        }
    }
}
