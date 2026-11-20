using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SetupEnemyLichPolyart
{
    const string CtrlPath = "Assets/_ArcanoDungeon/Animators/LichGameplay.controller";
    const string PrefabPath = "Assets/_ArcanoDungeon/Prefabs/EnemyLichPolyart.prefab";
    const string SourcePath = "Assets/Mini Legion Lich PBR HP Polyart/Prefabs/FreeLichPolyart.prefab";
    const string AnimRoot = "Assets/Mini Legion Lich PBR HP Polyart/Animations/Lich/";
    const string LevelPath = "Assets/_ArcanoDungeon/Scenes/Level1.unity";

    [MenuItem("Tools/Arcano/Setup Enemy Lich Polyart", false, 120)]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            Debug.LogWarning("[SetupEnemyLich] Sal de Play Mode y vuelve a ejecutar.");
            return;
        }

        ConvertLichMaterialsToUrp();
        var ctrl = BuildController();
        var prefab = BuildPrefab(ctrl);
        WireFinalRoom(prefab);
        AssetDatabase.SaveAssets();
        Debug.Log("[SetupEnemyLich] Listo: " + PrefabPath);
    }

    /// <summary>
    /// El pack del Lich viene con shaders Built-in (Standard) → rosa en URP.
    /// </summary>
    static void ConvertLichMaterialsToUrp()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[SetupEnemyLich] No se encontró URP Lit.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[]
        {
            "Assets/Mini Legion Lich PBR HP Polyart"
        });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null)
                continue;
            if (mat.shader.name.Contains("Universal Render Pipeline"))
                continue;

            Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            Texture bump = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
            Texture metallic = mat.HasProperty("_MetallicGlossMap") ? mat.GetTexture("_MetallicGlossMap") : null;
            Texture emission = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
            Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            Color emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
            float metallicVal = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
            float gloss = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.2f;

            mat.shader = urpLit;
            if (mainTex != null && mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", mainTex);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (bump != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", bump);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (metallic != null && mat.HasProperty("_MetallicGlossMap"))
            {
                mat.SetTexture("_MetallicGlossMap", metallic);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallicVal);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", gloss);
            if (emission != null || emissionColor.maxColorComponent > 0.01f)
            {
                if (emission != null && mat.HasProperty("_EmissionMap"))
                    mat.SetTexture("_EmissionMap", emission);
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", emissionColor);
                mat.EnableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(mat);
            Debug.Log("[SetupEnemyLich] Material URP: " + path);
        }
    }

    static AnimationClip FindClip(string fbxPath)
    {
        AnimationClip best = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            var clip = a as AnimationClip;
            if (clip == null || clip.name.StartsWith("__preview"))
                continue;
            if (best == null || clip.name.IndexOf("Take", System.StringComparison.OrdinalIgnoreCase) < 0)
                best = clip;
        }

        return best;
    }

    static AnimatorController BuildController()
    {
        var idle = FindClip(AnimRoot + "Idle.fbx");
        var walk = FindClip(AnimRoot + "Walk.fbx");
        var attack = FindClip(AnimRoot + "Attack01.fbx");
        var hit = FindClip(AnimRoot + "GetHit.fbx");
        var die = FindClip(AnimRoot + "Die.fbx");
        if (idle == null || walk == null || attack == null || hit == null || die == null)
            throw new System.Exception("Faltan clips de animación del Lich.");

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CtrlPath) != null)
            AssetDatabase.DeleteAsset(CtrlPath);

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CtrlPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;
        foreach (var s in sm.states)
            sm.RemoveState(s.state);

        var stIdle = sm.AddState("Idle", new Vector3(200f, 0f, 0f));
        stIdle.motion = idle;
        sm.defaultState = stIdle;

        var stWalk = sm.AddState("Walk", new Vector3(200f, 80f, 0f));
        stWalk.motion = walk;

        var stAttack = sm.AddState("Attack", new Vector3(450f, 40f, 0f));
        stAttack.motion = attack;

        var stHit = sm.AddState("Hit", new Vector3(450f, 140f, 0f));
        stHit.motion = hit;

        var stDeath = sm.AddState("Death", new Vector3(450f, 240f, 0f));
        stDeath.motion = die;

        var idleToWalk = stIdle.AddTransition(stWalk);
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.1f;
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var walkToIdle = stWalk.AddTransition(stIdle);
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.1f;
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        var anyAtk = sm.AddAnyStateTransition(stAttack);
        anyAtk.hasExitTime = false;
        anyAtk.duration = 0.05f;
        anyAtk.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

        var atkToIdle = stAttack.AddTransition(stIdle);
        atkToIdle.hasExitTime = true;
        atkToIdle.exitTime = 0.9f;
        atkToIdle.duration = 0.1f;

        var anyHit = sm.AddAnyStateTransition(stHit);
        anyHit.hasExitTime = false;
        anyHit.duration = 0.05f;
        anyHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");

        var hitToIdle = stHit.AddTransition(stIdle);
        hitToIdle.hasExitTime = true;
        hitToIdle.exitTime = 0.85f;
        hitToIdle.duration = 0.08f;

        var anyDie = sm.AddAnyStateTransition(stDeath);
        anyDie.hasExitTime = false;
        anyDie.duration = 0.05f;
        anyDie.AddCondition(AnimatorConditionMode.If, 0f, "Die");

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    static GameObject BuildPrefab(AnimatorController ctrl)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        if (source == null)
            throw new System.Exception("No se encontró " + SourcePath);

        var temp = (GameObject)PrefabUtility.InstantiatePrefab(source);
        temp.name = "EnemyLichPolyart";
        temp.transform.localScale = Vector3.one * 0.48f;
        try { temp.tag = "Enemy"; } catch { /* tag opcional */ }

        var anim = temp.GetComponent<Animator>();
        if (anim == null)
            anim = temp.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        var cc = temp.GetComponent<CharacterController>();
        if (cc == null)
            cc = temp.AddComponent<CharacterController>();
        cc.height = 3.5f;
        cc.radius = 0.7f;
        cc.center = new Vector3(0f, 1.75f, 0f);
        cc.skinWidth = 0.08f;
        cc.stepOffset = 0.3f;

        var box = temp.GetComponent<BoxCollider>();
        if (box == null)
            box = temp.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.4f, 3.5f, 1.2f);
        box.center = new Vector3(0f, 1.75f, 0f);

        var eh = temp.GetComponent<EnemyHealth>();
        if (eh == null)
            eh = temp.AddComponent<EnemyHealth>();
        eh.maxHealth = 110;
        eh.currentHealth = 110;
        eh.deathTrigger = "Die";
        eh.hitTrigger = "Hit";
        eh.deathStateName = "Death";
        eh.deathDestroyDelay = 2.4f;

        var ai = temp.GetComponent<EnemyAI>();
        if (ai == null)
            ai = temp.AddComponent<EnemyAI>();
        ai.attackRange = 1.6f;
        ai.attackDamage = 16;
        ai.attackCooldown = 1.6f;
        ai.moveSpeed = 2.6f;
        ai.aggroRange = 40f;

        var skSrc = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Skeleton.prefab");
        try
        {
            var skCanvas = skSrc.transform.Find("HealthBarCanvas");
            if (skCanvas != null)
            {
                var existingCanvas = temp.transform.Find("HealthBarCanvas");
                if (existingCanvas != null)
                    Object.DestroyImmediate(existingCanvas.gameObject);

                var canvasGo = Object.Instantiate(skCanvas.gameObject, temp.transform);
                canvasGo.name = "HealthBarCanvas";
                var rt = canvasGo.GetComponent<RectTransform>();
                rt.localPosition = new Vector3(0f, 3.9f, 0f);
                rt.localScale = Vector3.one * 0.02f;

                eh.healthCanvasTransform = canvasGo.transform;
                eh.healthSlider = canvasGo.GetComponentInChildren<Slider>(true);
                if (eh.healthSlider != null && eh.healthSlider.fillRect != null)
                    eh.healthFillImage = eh.healthSlider.fillRect.GetComponent<Image>();
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(skSrc);
        }

        var saved = PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);
        Object.DestroyImmediate(temp);
        return saved;
    }

    static void WireFinalRoom(GameObject lichPrefab)
    {
        var scene = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
        var rooms = Object.FindObjectsByType<RoomController>(FindObjectsInactive.Include);
        foreach (var room in rooms)
        {
            if (room == null)
                continue;

            var isFinal = room.spawnMiniBoss ||
                          (!string.IsNullOrEmpty(room.roomName) && room.roomName.Contains("Minijefe"));
            if (!isFinal)
                continue;

            room.skeletonPrefab = lichPrefab;
            room.additionalEnemyPrefabs = System.Array.Empty<GameObject>();
            room.enemyScale = 2.1f;
            room.miniBossScale = 2.6f;
            EditorUtility.SetDirty(room);
            Debug.Log("[SetupEnemyLich] Room cableada: " + room.roomName);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
