using System.Text;
using UnityEngine;
using UnityEngine.UI;

public static class VerifyMainMenuPolish
{
    public static string Main()
    {
        var sb = new StringBuilder();
        var quit = GameObject.Find("/Canvas/MenuPanel/Quit");
        sb.AppendLine(quit != null ? "Quit ok" : "Quit missing");
        if (quit != null)
        {
            var label = quit.transform.Find("Label")?.GetComponent<Text>();
            sb.AppendLine("label=" + (label != null ? label.text : "null"));
            var btn = quit.GetComponent<Button>();
            sb.AppendLine("onClickCount=" + (btn != null ? btn.onClick.GetPersistentEventCount().ToString() : "null"));
            if (btn != null && btn.onClick.GetPersistentEventCount() > 0)
            {
                sb.AppendLine("target=" + btn.onClick.GetPersistentTarget(0));
                sb.AppendLine("method=" + btn.onClick.GetPersistentMethodName(0));
            }
        }

        var depth = GameObject.Find("/MenuDepth");
        sb.AppendLine(depth != null
            ? "MenuDepth children=" + depth.transform.childCount
            : "MenuDepth missing");
        if (depth != null)
        {
            for (int i = 0; i < depth.transform.childCount; i++)
                sb.AppendLine("  " + depth.transform.GetChild(i).name);
        }

        var row = GameObject.Find("/Canvas/MenuPanel/New Game")?.GetComponent<MenuButtonRow>();
        sb.AppendLine(row != null
            ? "NewGame fill=" + (row.fill != null) + " frame=" + (row.frame != null) + " glow=" + (row.glow != null)
            : "MenuButtonRow missing");

        return sb.ToString();
    }
}
