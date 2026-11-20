using System.Text;
using UnityEngine;

public static class InspectMainMenu
{
    public static string Main()
    {
        var sb = new StringBuilder();
        void Dump(Transform t, int d)
        {
            sb.AppendLine(new string(' ', d * 2) + t.name);
            for (int i = 0; i < t.childCount; i++)
                Dump(t.GetChild(i), d + 1);
        }

        var panel = GameObject.Find("/Canvas/MenuPanel");
        if (panel != null)
            Dump(panel.transform, 0);
        else
            sb.AppendLine("MenuPanel missing");

        var ruins = GameObject.Find("/CastleRuins");
        if (ruins != null)
        {
            sb.AppendLine("--- CastleRuins children: " + ruins.transform.childCount);
            int n = Mathf.Min(ruins.transform.childCount, 40);
            for (int i = 0; i < n; i++)
            {
                var c = ruins.transform.GetChild(i);
                sb.AppendLine("  " + c.name + " pos=" + c.position);
            }
        }

        sb.AppendLine(
            "fog=" + RenderSettings.fog +
            " dens=" + RenderSettings.fogDensity +
            " color=" + RenderSettings.fogColor);

        var cam = Camera.main;
        sb.AppendLine(cam != null
            ? "cam pos=" + cam.transform.position + " rot=" + cam.transform.eulerAngles
            : "no cam");

        foreach (var name in new[] { "Altar", "Altar_Menu", "Crystal", "MenuBackdrop" })
        {
            var go = GameObject.Find(name);
            sb.AppendLine(go != null
                ? name + " pos=" + go.transform.position + " scale=" + go.transform.localScale
                : name + " missing");
        }

        return sb.ToString();
    }
}
