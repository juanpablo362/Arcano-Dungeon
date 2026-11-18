using UnityEngine;

public class SkullEyeGlow : MonoBehaviour
{
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    public Color baseEmission = new Color(1.8f, 0.05f, 0.02f);
    public float minIntensity = 0.55f;
    public float maxIntensity = 1.85f;
    public float speed = 1.4f;

    Renderer[] eyes;
    MaterialPropertyBlock block;
    float[] phases;

    void Start()
    {
        var found = GetComponentsInChildren<Renderer>(true);
        var list = new System.Collections.Generic.List<Renderer>();
        foreach (var renderer in found)
        {
            if (renderer.gameObject.name.Contains("Ojos"))
                list.Add(renderer);
        }

        eyes = list.ToArray();
        phases = new float[eyes.Length];
        block = new MaterialPropertyBlock();

        for (int i = 0; i < eyes.Length; i++)
            phases[i] = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (eyes == null || eyes.Length == 0)
            return;

        for (int i = 0; i < eyes.Length; i++)
        {
            float n = Mathf.PerlinNoise(phases[i], Time.time * speed);
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, n);
            eyes[i].GetPropertyBlock(block);
            block.SetColor(EmissionColorId, baseEmission * intensity);
            eyes[i].SetPropertyBlock(block);
        }
    }
}
