using System.Collections.Generic;
using UnityEngine;

public class CrystalFloat : MonoBehaviour
{
    public float amplitude = 0.16f;
    public float period = 4f;
    public float spinDegreesPerSecond = 4f;
    public float ringSpeedA = 28f;
    public float ringSpeedB = -18f;

    Vector3 startPosition;
    readonly List<Transform> rings = new();

    void Start()
    {
        startPosition = transform.position;
        CrystalMaterials.Apply(gameObject);

        rings.Clear();
        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            if (child == transform)
                continue;
            if (child.name.Contains("Orbit") || child.name.Contains("Gold"))
                rings.Add(child);
        }
    }

    void Update()
    {
        float bob = Mathf.Sin(Time.time * (Mathf.PI * 2f / period)) * amplitude;
        transform.position = startPosition + Vector3.up * bob;
        transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);

        for (int i = 0; i < rings.Count; i++)
        {
            float speed = i % 2 == 0 ? ringSpeedA : ringSpeedB;
            rings[i].Rotate(Vector3.up, speed * Time.deltaTime, Space.Self);
        }
    }
}
