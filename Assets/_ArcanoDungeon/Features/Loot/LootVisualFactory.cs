using ArcanoDungeon.Infrastructure.Assets;
using UnityEngine;

namespace ArcanoDungeon.Features.Loot
{
    /// <summary>
    /// Crea el visual de un loot item (prefab del ItemData, gems BTM o primitivo).
    /// Escala pequeña + collider físico para no atravesar el suelo.
    /// </summary>
    public static class LootVisualFactory
    {
        const string GemsRoot = "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/";
        const float DefaultScale = 0.38f;

        public static GameObject Create(ItemData item, Vector3 position)
        {
            if (item == null)
                return CreateFallbackOrb(LootItemKind.Heal, position);

            GameObject go = null;

            if (item.worldPrefab != null)
            {
                go = Object.Instantiate(item.worldPrefab, position, Quaternion.identity);
            }
            else
            {
                string path = ResolveFallbackPath(item);
                var prefab = LoadPrefab(path);
                if (prefab != null)
                    go = Object.Instantiate(prefab, position, Quaternion.identity);
            }

            if (go == null)
                go = CreateFallbackOrbFromItem(item, position);

            go.name = $"Loot_{item.id}";
            go.transform.localScale = Vector3.one * ResolveScale(item);
            EnsurePickupPhysics(go);

            var pickup = go.GetComponent<ItemPickup>() ?? go.AddComponent<ItemPickup>();
            pickup.Configure(item);
            return go;
        }

        public static GameObject Create(LootItemKind kind, Vector3 position)
        {
            var go = CreateFallbackOrb(kind, position);
            EnsurePickupPhysics(go);
            return go;
        }

        public static void EnsurePickupPhysics(GameObject go)
        {
            if (go == null)
                return;

            // Quitar colliders de malla enormes / triggers del prefab BTM.
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
                Object.Destroy(col);

            var sphere = go.AddComponent<SphereCollider>();
            sphere.radius = 0.28f;
            sphere.center = new Vector3(0f, 0.28f, 0f);
            sphere.isTrigger = false;

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null)
                rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.35f;
            rb.linearDamping = 2.2f;
            rb.angularDamping = 4f;
            rb.useGravity = true;
            rb.isKinematic = true; // ItemPickup lo libera al aterrizar / al lanzar.
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        static string ResolveFallbackPath(ItemData item)
        {
            string id = (item.id ?? item.name ?? "").ToLowerInvariant();
            if (item.hasOrbital || id.Contains("orbital") || id.Contains("orbe"))
                return GemsRoot + "SphereGem.prefab";
            if (item.hasChainLightning || id.Contains("rayo") || id.Contains("chain") || id.Contains("electric"))
                return GemsRoot + "Thunder.prefab";
            if (item.hasHolyAura || id.Contains("aura") || id.Contains("fuego"))
                return GemsRoot + "Ruby.prefab";
            if (item.hasVampiric || id.Contains("vampir") || id.Contains("colmillo"))
                return GemsRoot + "Ruby2.prefab";
            if (item.hasMultishot || id.Contains("lente") || id.Contains("multishot") || id.Contains("dispersion"))
                return GemsRoot + "Diamondo.prefab";
            if (item.hasCrystalHeart || id.Contains("cristal") || id.Contains("crystal_heart"))
                return GemsRoot + "HeartGem.prefab";
            if (id.Contains("hermes") || id.Contains("botas") || Mathf.Abs(item.moveSpeedMult - 1.2f) < 0.01f)
                return GemsRoot + "SpeedChevYellow.prefab";
            if (item.bombCharges > 0 || id.Contains("bomb") || id.Contains("polvora"))
                return GemsRoot + "Bomb.prefab";
            if (item.type == ItemType.Weapon || id.Contains("sword") || id.Contains("staff") || id.Contains("crossbow")
                || id.Contains("gaunt") || id.Contains("grimorio") || id.Contains("mandoble") || id.Contains("ballesta"))
                return GemsRoot + "StarGem3.prefab";
            if (item.refillShield || id.Contains("escudo") || id.Contains("shield"))
                return GemsRoot + "BluePotion.prefab";
            if (item.scoreAmount > 0 || id.Contains("coin") || id.Contains("moneda") || id.Contains("gema"))
                return GemsRoot + "Coin.prefab";
            if (item.healAmount > 0 || id.Contains("pocion") || id.Contains("heal"))
                return GemsRoot + "RedPotion.prefab";
            return GemsRoot + "StarGem1.prefab";
        }

        static float ResolveScale(ItemData item)
        {
            if (item.scoreAmount > 0)
                return 0.32f;
            if (item.hasOrbital || item.hasHolyAura)
                return 0.36f;
            if (item.hasChainLightning)
                return 0.34f;
            return DefaultScale;
        }

        static GameObject LoadPrefab(string assetPath)
        {
            return DungeonRuntimeAssets.Load<GameObject>(assetPath);
        }

        static GameObject CreateFallbackOrbFromItem(ItemData item, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"LootFallback_{item.id}";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.22f;
            Object.Destroy(go.GetComponent<Collider>());
            Tint(go, KindColorFromItem(item));
            return go;
        }

        static GameObject CreateFallbackOrb(LootItemKind kind, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"LootFallback_{kind}";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.22f;
            Object.Destroy(go.GetComponent<Collider>());
            Tint(go, KindColor(kind));
            return go;
        }

        static void Tint(GameObject go, Color c)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null)
                return;
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            if (shader == null)
                return;
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", c);
            rend.sharedMaterial = mat;
        }

        static Color KindColorFromItem(ItemData item)
        {
            if (item.hasOrbital) return new Color(0.55f, 0.85f, 1f);
            if (item.hasChainLightning) return new Color(0.5f, 0.9f, 1f);
            if (item.hasHolyAura) return new Color(1f, 0.45f, 0.15f);
            if (item.refillShield) return new Color(0.35f, 0.7f, 1f);
            if (item.scoreAmount > 0) return new Color(1f, 0.85f, 0.2f);
            if (item.healAmount > 0) return new Color(1f, 0.25f, 0.3f);
            return Color.white;
        }

        public static Color KindColor(LootItemKind kind)
        {
            return kind switch
            {
                LootItemKind.Heal => new Color(1f, 0.25f, 0.3f),
                LootItemKind.MaxHealth => new Color(1f, 0.45f, 0.55f),
                LootItemKind.Speed => new Color(1f, 0.9f, 0.25f),
                LootItemKind.Strength => new Color(1f, 0.55f, 0.15f),
                LootItemKind.Reach => new Color(0.35f, 0.7f, 1f),
                LootItemKind.Stamina => new Color(0.35f, 0.95f, 0.55f),
                _ => Color.white,
            };
        }
    }
}
