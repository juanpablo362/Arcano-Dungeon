using UnityEngine;

namespace ArcanoDungeon.Features.Combat
{
    /// <summary>
    /// Estela / semiluna de ataque. Activo solo entre OnAttackHitFrame y OnAttackEndFrame.
    /// Knight: TrailRenderer + slash mesh opcional.
    /// Mage: onda/crescent que viaja hasta el rango del bastón.
    /// Step 3: destello puntual en tip (sin arco ancho).
    /// </summary>
    public class AttackArcVFX : MonoBehaviour
    {
        [Header("Knight")]
        [SerializeField] TrailRenderer weaponTrail;
        [SerializeField] GameObject slashArcPrefab;
        [SerializeField] Transform slashSpawnPoint;

        [Header("Mage")]
        [SerializeField] GameObject crescentWavePrefab;
        [SerializeField] Transform crescentSpawnPoint;
        [SerializeField] float crescentTravelSeconds = 0.28f;

        [Header("Finisher puntual")]
        [SerializeField] GameObject punctualFlashPrefab;

        [Header("Refs")]
        [SerializeField] Transform weaponTip;
        [SerializeField] Transform owner;

        GameObject activeSlash;
        GameObject activeCrescent;
        GameObject activeFlash;
        Coroutine crescentRoutine;
        bool trailSoftened;

        // Estela suave tipo glow (rosa translúcido → transparente).
        static readonly Color TrailHead = new Color(1f, 0.62f, 0.88f, 0.28f);
        static readonly Color TrailTail = new Color(1f, 0.75f, 0.95f, 0f);

        void Awake()
        {
            if (owner == null)
                owner = transform.root != null ? transform.root : transform;
            EnsureTrail();
            SoftenTrailLook();
            SetTrailEmitting(false);
        }

        Transform Owner => owner != null ? owner : (transform.root != null ? transform.root : transform);

        public void Configure(
            Transform tip,
            TrailRenderer trail,
            GameObject slashPrefab,
            GameObject crescentPrefab,
            GameObject punctualPrefab,
            Transform slashPoint,
            Transform crescentPoint)
        {
            weaponTip = tip;
            weaponTrail = trail;
            slashArcPrefab = slashPrefab;
            crescentWavePrefab = crescentPrefab;
            punctualFlashPrefab = punctualPrefab;
            slashSpawnPoint = slashPoint;
            crescentSpawnPoint = crescentPoint;
            if (owner == null)
                owner = transform.root != null ? transform.root : transform;
            EnsureTrail();
            SoftenTrailLook();
            SetTrailEmitting(false);
        }

        /// <summary>Estela del swing: solo TrailRenderer en la punta (sin bolita/crescent).</summary>
        public void PlaySwingTrail(int comboStep, float weaponLength, MeleeWeaponController.AttackStyle style)
        {
            StopActiveMeshes();
            SetTrailEmitting(true);
        }

        /// <summary>
        /// Pop de slash/finisher: SOLO al completar el combo (paso 3) y cuando el golpe conectó.
        /// </summary>
        public void PlayFinisherPop(Vector3 hitPoint, float weaponLength)
        {
            var own = Owner;
            Quaternion facing = Quaternion.LookRotation(Flatten(own.forward), Vector3.up);
            Vector3 tipPos = weaponTip != null ? weaponTip.position : hitPoint;

            // Preferir pop pequeño de impacto; si hay slash prefab, usarlo muy reducido.
            if (punctualFlashPrefab != null)
            {
                activeFlash = Instantiate(punctualFlashPrefab, hitPoint != Vector3.zero ? hitPoint : tipPos, facing);
                activeFlash.transform.localScale = Vector3.one * 0.28f;
                SafeDestroy(activeFlash, 0.65f);
            }
            else if (slashArcPrefab != null)
            {
                Transform parent = weaponTip != null ? weaponTip : own;
                activeSlash = Instantiate(slashArcPrefab, tipPos, facing, parent);
                activeSlash.transform.localScale = Vector3.one * 0.28f;
                SafeDestroy(activeSlash, 0.65f);
            }
        }

        /// <summary>Compat: trail de swing; el pop de slash ya no se spawnea aquí.</summary>
        public void Play(
            int comboStep,
            float weaponLength,
            float arcAngle,
            MeleeWeaponController.AttackStyle style)
        {
            PlaySwingTrail(comboStep, weaponLength, style);
        }

        public void Stop()
        {
            SetTrailEmitting(false);
            StopActiveMeshes();
        }

        void StopActiveMeshes()
        {
            if (crescentRoutine != null)
            {
                StopCoroutine(crescentRoutine);
                crescentRoutine = null;
            }

            if (activeSlash != null)
            {
                SafeDestroy(activeSlash, 0f);
                activeSlash = null;
            }

            if (activeCrescent != null)
            {
                SafeDestroy(activeCrescent, 0f);
                activeCrescent = null;
            }
        }

        static void SafeDestroy(Object obj, float delay)
        {
            if (obj == null)
                return;
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(obj);
                return;
            }
            if (delay > 0f)
                Object.Destroy(obj, delay);
            else
                Object.Destroy(obj);
        }

        System.Collections.IEnumerator TravelCrescent(GameObject wave, Vector3 forward, float maxDistance)
        {
            if (wave == null)
                yield break;

            Vector3 start = wave.transform.position;
            Vector3 end = start + forward.normalized * maxDistance;
            float t = 0f;
            var renderers = wave.GetComponentsInChildren<Renderer>();

            while (t < 1f && wave != null)
            {
                t += Time.deltaTime / Mathf.Max(0.05f, crescentTravelSeconds);
                float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                wave.transform.position = Vector3.Lerp(start, end, eased);

                float alpha = 1f - eased;
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null)
                        continue;
                    // Instancia de runtime: materials es aceptable en play; evitar en edit.
                    var mats = Application.isPlaying ? renderers[i].materials : renderers[i].sharedMaterials;
                    for (int m = 0; m < mats.Length; m++)
                    {
                        var mat = mats[m];
                        if (mat == null)
                            continue;
                        if (mat.HasProperty("_BaseColor"))
                        {
                            Color c = mat.GetColor("_BaseColor");
                            c.a = alpha;
                            mat.SetColor("_BaseColor", c);
                        }
                        else if (mat.HasProperty("_Color"))
                        {
                            Color c = mat.color;
                            c.a = alpha;
                            mat.color = c;
                        }
                    }
                }

                yield return null;
            }

            if (wave != null)
                SafeDestroy(wave, 0f);
            if (activeCrescent == wave)
                activeCrescent = null;
            crescentRoutine = null;
        }

        void EnsureTrail()
        {
            if (weaponTrail != null)
                return;

            Transform tip = weaponTip != null ? weaponTip : transform;
            weaponTrail = tip.GetComponent<TrailRenderer>();
            if (weaponTrail != null)
                return;

            weaponTrail = tip.gameObject.AddComponent<TrailRenderer>();
            weaponTrail.time = 0.14f;
            weaponTrail.minVertexDistance = 0.02f;
            weaponTrail.widthMultiplier = 0.08f;
            weaponTrail.emitting = false;
            weaponTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void SoftenTrailLook()
        {
            if (weaponTrail == null || trailSoftened)
                return;

            weaponTrail.time = Mathf.Min(weaponTrail.time, 0.16f);
            if (weaponTrail.widthMultiplier > 0.1f)
                weaponTrail.widthMultiplier = 0.08f;
            weaponTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(TrailHead, 0f),
                    new GradientColorKey(TrailTail, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.28f, 0f),
                    new GradientAlphaKey(0.1f, 0.4f),
                    new GradientAlphaKey(0f, 1f)
                });
            weaponTrail.colorGradient = gradient;

            weaponTrail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0.05f));

            // Rosa suave casi transparente (no sólido).
            var soft = new Color(1f, 0.62f, 0.88f, 0.18f);
            var existing = weaponTrail.sharedMaterial;
            Material mat;
            if (existing != null && existing.name.Contains("WeaponTrailSoftPink"))
            {
                mat = existing;
            }
            else if (existing == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Sprites/Default")
                             ?? Shader.Find("Unlit/Color");
                if (shader == null)
                    return;
                mat = new Material(shader) { name = "WeaponTrailSoftPink" };
                weaponTrail.sharedMaterial = mat;
            }
            else
            {
                mat = new Material(existing) { name = "WeaponTrailSoftPink" };
                weaponTrail.sharedMaterial = mat;
            }

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", soft);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", soft);
            if (mat.HasProperty("_TintColor"))
                mat.SetColor("_TintColor", soft);
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_SrcBlend"))
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend"))
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite"))
                mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            trailSoftened = true;
        }

        void SetTrailEmitting(bool emitting)
        {
            EnsureTrail();
            if (weaponTrail == null)
                return;

            // Asegurar que el trail vive en la punta del arma.
            if (weaponTip != null && weaponTrail.transform != weaponTip)
            {
                weaponTrail.transform.SetParent(weaponTip, false);
                weaponTrail.transform.localPosition = Vector3.zero;
                weaponTrail.transform.localRotation = Quaternion.identity;
            }

            if (emitting)
            {
                weaponTrail.Clear();
                weaponTrail.emitting = true;
            }
            else
            {
                weaponTrail.emitting = false;
            }
        }

        static Vector3 Flatten(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.001f ? v.normalized : Vector3.forward;
        }
    }
}
