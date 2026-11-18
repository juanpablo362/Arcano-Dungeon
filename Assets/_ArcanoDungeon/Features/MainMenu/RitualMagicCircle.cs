using UnityEngine;

public class RitualMagicCircle : MonoBehaviour
{
    public float ringSize = 3.2f;
    public float coreSize = 1.35f;
    public float heightOffset = 0.08f;
    public float ringSpinSpeed = 18f;
    public float coreSpinSpeed = -10f;
    public float lightMin = 1.2f;
    public float lightMax = 3.4f;
    public float breathPeriod = 3.2f;

    Transform ring;
    Transform core;
    Light centerLight;

    public static RitualMagicCircle CreateAt(Vector3 worldPosition)
    {
        var root = new GameObject("RitualMagicCircle");
        root.transform.position = worldPosition;
        return root.AddComponent<RitualMagicCircle>();
    }

    void Start()
    {
        var ringMat = Resources.Load<Material>("RitualCircleRing");
        var coreMat = Resources.Load<Material>("RitualCircleCore");

        ring = CreateFxQuad("RitualRing", ringMat, ringSize, 0.02f);
        core = CreateFxQuad("RitualCore", coreMat, coreSize, 0.04f);

        var lightObject = new GameObject("RitualCenterLight");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        centerLight = lightObject.AddComponent<Light>();
        centerLight.type = LightType.Point;
        centerLight.color = new Color(0.7f, 0.3f, 1f);
        centerLight.intensity = lightMax;
        centerLight.range = 4.5f;
        var additional = lightObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
        additional.usePipelineSettings = true;
    }

    Transform CreateFxQuad(string name, Material material, float size, float y)
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        Object.Destroy(quad.GetComponent<Collider>());

        var t = quad.transform;
        t.SetParent(transform, false);
        t.localPosition = new Vector3(0f, heightOffset + y, 0f);
        t.localRotation = Quaternion.Euler(90f, 0f, 0f);
        t.localScale = Vector3.one * size;

        var renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return t;
    }

    void Update()
    {
        if (ring != null)
            ring.Rotate(Vector3.forward, ringSpinSpeed * Time.deltaTime, Space.Self);
        if (core != null)
            core.Rotate(Vector3.forward, coreSpinSpeed * Time.deltaTime, Space.Self);

        if (centerLight != null)
        {
            float breath = (Mathf.Sin(Time.time * (Mathf.PI * 2f / breathPeriod)) + 1f) * 0.5f;
            float flicker = Mathf.PerlinNoise(12.7f, Time.time * 0.9f);
            centerLight.intensity = Mathf.Lerp(lightMin, lightMax, Mathf.Lerp(breath, flicker, 0.3f));
        }
    }
}
