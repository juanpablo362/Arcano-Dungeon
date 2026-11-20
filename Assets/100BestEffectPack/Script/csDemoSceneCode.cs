using UnityEngine;

/// <summary>
/// Demo del pack 100 Best Effects. Actualizado: GUIText ya no existe en Unity 6.
/// </summary>
public class csDemoSceneCode : MonoBehaviour
{
    public string[] EffectNames;
    public string[] Effect2Names;
    public Transform[] Effect;
    [SerializeField] UnityEngine.UI.Text Text1;
    int i;
    int a;

    void Start()
    {
        if (Effect != null && Effect.Length > 0 && Effect[i] != null)
            Instantiate(Effect[i], new Vector3(0, 5, 0), Quaternion.identity);
        RefreshLabel();
    }

    void Update()
    {
        RefreshLabel();

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (i <= 0)
                i = 99;
            else
                i--;

            SpawnCurrent();
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            if (i < 99)
                i++;
            else
                i = 0;

            SpawnCurrent();
        }

        if (Input.GetKeyDown(KeyCode.C))
            SpawnCurrent();
    }

    void RefreshLabel()
    {
        if (Text1 == null || EffectNames == null || EffectNames.Length == 0)
            return;

        int idx = Mathf.Clamp(i, 0, EffectNames.Length - 1);
        Text1.text = (idx + 1) + ":" + EffectNames[idx];
    }

    void SpawnCurrent()
    {
        if (Effect == null || Effect.Length == 0 || i < 0 || i >= Effect.Length || Effect[i] == null)
            return;

        if (EffectNames != null && Effect2Names != null)
        {
            for (a = 0; a < Effect2Names.Length; a++)
            {
                if (EffectNames[i] == Effect2Names[a])
                {
                    Instantiate(Effect[i], new Vector3(0, 0.2f, 0), Quaternion.identity);
                    return;
                }
            }
        }

        Instantiate(Effect[i], new Vector3(0, 5, 0), Quaternion.identity);
    }
}
