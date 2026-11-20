using UnityEngine;

public class PortalVisual : MonoBehaviour
{
    public Transform runeRing;
    public Light glow;
    public float runeSpeed = 24f;

    float baseIntensity = 4f;

    void Awake()
    {
        if (glow != null)
            baseIntensity = glow.intensity;
    }

    void Update()
    {
        if (runeRing != null)
            runeRing.Rotate(0f, 0f, runeSpeed * Time.deltaTime, Space.Self);

        if (glow != null)
            glow.intensity = baseIntensity * (0.82f + Mathf.Sin(Time.time * 3.2f) * 0.18f);
    }
}
