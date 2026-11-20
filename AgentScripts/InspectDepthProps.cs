using System.Text;
using UnityEngine;

public static class InspectDepthProps
{
    public static string Main()
    {
        var sb = new StringBuilder();
        var depth = GameObject.Find("/MenuDepth");
        if (depth == null)
            return "no MenuDepth";

        var renderers = depth.GetComponentsInChildren<Renderer>(true);
        sb.AppendLine("renderers=" + renderers.Length);
        foreach (var r in renderers)
        {
            sb.AppendLine(r.name + " enabled=" + r.enabled +
                          " bounds=" + r.bounds +
                          " layer=" + r.gameObject.layer +
                          " mat=" + (r.sharedMaterial != null ? r.sharedMaterial.shader.name : "null"));
        }

        for (int i = 0; i < depth.transform.childCount; i++)
        {
            var t = depth.transform.GetChild(i);
            var rends = t.GetComponentsInChildren<Renderer>(true);
            Bounds? b = null;
            foreach (var r in rends)
            {
                if (b == null) b = r.bounds;
                else
                {
                    var bb = b.Value;
                    bb.Encapsulate(r.bounds);
                    b = bb;
                }
            }
            sb.AppendLine(t.name + " pos=" + t.position + " scale=" + t.localScale +
                          " childRenderers=" + rends.Length +
                          (b.HasValue ? " worldBounds=" + b.Value : " noBounds"));
        }

        return sb.ToString();
    }
}
