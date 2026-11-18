using UnityEngine;

public class MenuAmbientLighting : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    public float breathPeriod = 4f;
    public float flickerSpeed = 0.7f;
    public float lightMin = 10f;
    public float lightMax = 14f;
    public float lightRange = 6.8f;
    public Color backdropTint = new Color(0.55f, 0.55f, 0.7f, 1f);
    public float backdropMin = 0.55f;
    public float backdropMax = 1.15f;

    Light crystalLight;
    Transform crystal;
    Renderer backdropRenderer;
    MaterialPropertyBlock block;
    float seed;

    void Start()
    {
        var crystalObject = GameObject.Find("Crystal");
        if (crystalObject != null)
            crystal = crystalObject.transform;

        var lightObject = GameObject.Find("CrystalLight");
        if (lightObject != null)
        {
            crystalLight = lightObject.GetComponent<Light>();
            crystalLight.range = lightRange;
            crystalLight.shadows = LightShadows.None;
        }

        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light != crystalLight && light.type == LightType.Directional)
                light.intensity = 0f;
        }

        var backdrop = GameObject.Find("MenuBackdrop");
        if (backdrop != null)
            backdropRenderer = backdrop.GetComponent<Renderer>();

        block = new MaterialPropertyBlock();
        seed = Random.Range(0f, 50f);
    }

    void Update()
    {
        float breath = (Mathf.Sin(Time.time * (Mathf.PI * 2f / breathPeriod)) + 1f) * 0.5f;
        float flicker = Mathf.PerlinNoise(seed, Time.time * flickerSpeed);
        float pulse = Mathf.Lerp(breath, flicker, 0.35f);

        if (crystalLight != null)
        {
            if (crystal != null)
                crystalLight.transform.position = crystal.position;
            crystalLight.range = lightRange;
            crystalLight.intensity = Mathf.Lerp(lightMin, lightMax, pulse);
        }

        if (backdropRenderer != null)
        {
            float tintStrength = Mathf.Lerp(backdropMin, backdropMax, pulse);
            Color color = backdropTint * tintStrength;
            color.a = 1f;
            backdropRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            backdropRenderer.SetPropertyBlock(block);
        }
    }
}
