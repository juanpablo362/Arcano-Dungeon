using System;
using System.Collections;
using System.Collections.Generic;
using ArcanoDungeon.Infrastructure.Audio;
using UnityEngine;

namespace ArcanoDungeon.Features.Combat
{
    /// <summary>
    /// Combate melee con combo 3 hits, Animation Events, rango por tip de arma e hitstop local.
    /// Steps 1-2 = arco frontal; step 3 = golpe puntual en tip/cápsula.
    /// </summary>
    public class MeleeWeaponController : MonoBehaviour
    {
        public enum AttackStyle
        {
            SwordArc,
            StaffCrescent
        }

        [Header("Referencias")]
        [SerializeField] Animator animator;
        [SerializeField] Transform weaponBaseTransform;
        [SerializeField] Transform weaponTipTransform;
        [SerializeField] AttackArcVFX attackArcVfx;
        [SerializeField] GameObject impactVfxPrefab;
        [SerializeField] CharacterController characterController;

        [Header("Estilo")]
        [SerializeField] AttackStyle attackStyle = AttackStyle.SwordArc;
        [SerializeField] float fallbackWeaponLength = 1.4f;
        [Tooltip("Multiplicador de alcance efectivo desde el cuerpo (1 = largo del arma).")]
        [SerializeField] float reachMultiplier = 1.9f;
        [SerializeField] float arcAngle = 110f;
        [SerializeField] float crossFadeDuration = 0.08f;

        [Header("Daño por step (1, 2, 3)")]
        [SerializeField] int[] damagePerStep = { 28, 32, 48 };
        [SerializeField] float punctualRadius = 0.55f;

        [Header("Combo / Buffer")]
        [SerializeField] float inputBufferWindow = 0.35f;
        [SerializeField] float comboResetTime = 0.7f;
        [SerializeField] int maxComboSteps = 3;
        [Tooltip("Si está activo, solo encadena si el golpe anterior conectó. Desactivado = combo por timing/clics.")]
        [SerializeField] bool requireHitToContinueCombo = false;

        [Header("Movimiento al atacar")]
        [Tooltip("Espada/dagas: multiplicador WASD durante el swing (0 = parado).")]
        [SerializeField] float meleeAttackMoveMultiplier = 0f;
        [Tooltip("Maga/bastón: caminata lenta de combate mientras canaliza.")]
        [SerializeField] float rangedAttackMoveMultiplier = 0.3f;
        [SerializeField] float meleeLungeDistance = 1.5f;
        [SerializeField] float meleeLungeDuration = 0.3f;
        [Tooltip("Tope duro si el Animation Event de fin no llega.")]
        [SerializeField] float attackFallbackSeconds = 1.35f;

        [Header("Hitstop")]
        [SerializeField] float hitstopDuration = 0.04f;
        [SerializeField] float hitstopSpeed = 0.05f;

        [Header("Capa / Tags")]
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] string enemyTag = "Enemy";

        static readonly int ComboIndexHash = Animator.StringToHash("ComboIndex");
        static readonly string[] AttackStateNames = { "Attack1", "Attack2", "Attack3" };

        readonly HashSet<EnemyHealth> hitThisSwing = new HashSet<EnemyHealth>();
        readonly Collider[] overlapBuffer = new Collider[32];

        int comboIndex;
        bool isAttacking;
        bool hitWindowOpen;
        bool hitResolvedThisSwing;
        bool bufferedInput;
        bool swungConnected;
        bool lastSwingConnected;
        float bufferExpireTime;
        float comboExpireTime;
        float attackStartedAt;
        float defaultAnimatorSpeed = 1f;
        Coroutine hitstopRoutine;
        Coroutine safetyRoutine;
        Coroutine lungeRoutine;
        Coroutine hardFallbackRoutine;

        public bool IsAttacking => isAttacking;
        public int CurrentComboStep => comboIndex;
        public AttackStyle Style => attackStyle;

        /// <summary>0 en melee (bloquea WASD); 0.3 en canalización a distancia.</summary>
        public float AttackMoveSpeedMultiplier
        {
            get
            {
                if (!isAttacking)
                    return 1f;
                return attackStyle == AttackStyle.StaffCrescent
                    ? rangedAttackMoveMultiplier
                    : meleeAttackMoveMultiplier;
            }
        }

        /// <summary>Melee: bloquea WASD durante el swing. Maga: permite caminata lenta.</summary>
        public bool LocksMoveInput => isAttacking && attackStyle != AttackStyle.StaffCrescent;

        /// <summary>Dash/sprint deshabilitados mientras ataca o canaliza.</summary>
        public bool BlocksDashAndSprint => isAttacking;

        public Transform WeaponBase => weaponBaseTransform;
        public Transform WeaponTip => weaponTipTransform;

        public event Action AttackStarted;
        public event Action AttackEnded;
        public event Action<EnemyHealth, Vector3> EnemyHit;

        float arcAngleMultiplier = 1f;

        public void SetArcAngleMultiplier(float mult)
        {
            arcAngleMultiplier = Mathf.Max(0.25f, mult);
        }

        public float GetArcAngle() => arcAngle * arcAngleMultiplier;

        void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            if (attackArcVfx == null)
                attackArcVfx = GetComponent<AttackArcVFX>() ?? GetComponentInChildren<AttackArcVFX>();
            if (characterController == null)
                characterController = GetComponent<CharacterController>()
                                     ?? GetComponentInParent<CharacterController>();
            if (animator != null)
            {
                if (animator.speed < 0.1f)
                    animator.speed = 1f;
                defaultAnimatorSpeed = 1f;
            }
        }

        void OnDisable()
        {
            if (hitstopRoutine != null)
            {
                StopCoroutine(hitstopRoutine);
                hitstopRoutine = null;
            }
            StopLunge();
            StopHardFallback();
            if (safetyRoutine != null)
            {
                StopCoroutine(safetyRoutine);
                safetyRoutine = null;
            }
            RestorePlayerAnimatorSpeed();
            if (isAttacking)
                ForceEndAttack(notifyEnded: true);
        }

        void Update()
        {
            // Watchdog: si el hitstop terminó y la velocidad quedó baja, recuperar.
            if (hitstopRoutine == null && animator != null && animator.speed > 0f && animator.speed < 0.2f)
                RestorePlayerAnimatorSpeed();

            // Anti-bloqueo extra: si el swing se alarga más allá del fallback, soltar.
            if (isAttacking && Time.time - attackStartedAt > attackFallbackSeconds)
                ForceEndAttack(notifyEnded: true);

            if (isAttacking)
                return;

            if (comboIndex > 0 && Time.time > comboExpireTime)
                comboIndex = 0;
        }

        /// <summary>Llamado desde PlayerController al LMB. No aplica daño.</summary>
        public bool TryAttack(Vector3 aimDirection)
        {
            if (aimDirection.sqrMagnitude < 0.001f)
                aimDirection = transform.forward;

            FaceAim(aimDirection);

            // Durante el swing: buffer 0.2s para encadenar al terminar (solo si conecta).
            if (isAttacking)
            {
                bufferedInput = true;
                bufferExpireTime = Time.time + inputBufferWindow;
                return false;
            }

            // Ventana post-golpe: solo encadena si el golpe anterior conectó.
            bool canChain = Time.time <= comboExpireTime
                            && comboIndex < maxComboSteps - 1
                            && (!requireHitToContinueCombo || lastSwingConnected);

            if (!canChain)
                comboIndex = 0;
            else
                comboIndex++;

            StartSwing(aimDirection);
            return true;
        }

        Vector3 lastHitPoint;

        /// <summary>Animation Event: frame de impacto. Una sola resolución de daño por swing.</summary>
        public void OnAttackHitFrame()
        {
            if (!isAttacking)
                return;

            // Evita multi-hit si el evento + safety (o eventos duplicados) disparan más de una vez.
            if (hitResolvedThisSwing)
                return;

            hitResolvedThisSwing = true;
            hitWindowOpen = true;
            lastHitPoint = Vector3.zero;

            int step = Mathf.Clamp(comboIndex, 0, maxComboSteps - 1);
            float reach = GetAttackReach();

            // Estela siempre; el pop de slash SOLO si es finisher y conecta.
            attackArcVfx?.PlaySwingTrail(step, reach, attackStyle);
            ResolveHits(step);

            bool isFinisher = step >= maxComboSteps - 1;
            if (isFinisher && swungConnected)
                attackArcVfx?.PlayFinisherPop(lastHitPoint, reach);
        }

        /// <summary>Animation Event: fin de ventana / swing.</summary>
        public void OnAttackEndFrame()
        {
            if (!isAttacking)
                return;

            hitWindowOpen = false;
            attackArcVfx?.Stop();
            StopSafety();
            StopHardFallback();
            StopLunge();

            // Asegurar salida de cámara lenta al terminar el swing.
            if (hitstopRoutine != null)
            {
                StopCoroutine(hitstopRoutine);
                hitstopRoutine = null;
            }
            RestorePlayerAnimatorSpeed();

            lastSwingConnected = swungConnected;
            bool wantNext = bufferedInput
                            && Time.time <= bufferExpireTime
                            && (!requireHitToContinueCombo || swungConnected);
            bufferedInput = false;

            isAttacking = false;
            AttackEnded?.Invoke();
            comboExpireTime = Time.time + comboResetTime;

            if (!lastSwingConnected)
            {
                comboIndex = 0;
                return;
            }

            if (wantNext && comboIndex < maxComboSteps - 1)
            {
                comboIndex++;
                StartSwing(transform.forward);
                return;
            }

            // Finisher completado (o sin buffer): resetear para nuevo combo.
            if (comboIndex >= maxComboSteps - 1)
                comboIndex = 0;
        }

        /// <summary>Fuerza salida de ataque (fallback / OnDisable). Evita soft-lock de movimiento.</summary>
        public void ForceEndAttack(bool notifyEnded = true)
        {
            if (!isAttacking)
            {
                StopLunge();
                StopSafety();
                StopHardFallback();
                return;
            }

            hitWindowOpen = false;
            bufferedInput = false;
            attackArcVfx?.Stop();
            StopLunge();
            StopSafety();
            StopHardFallback();

            if (hitstopRoutine != null)
            {
                StopCoroutine(hitstopRoutine);
                hitstopRoutine = null;
            }
            RestorePlayerAnimatorSpeed();

            isAttacking = false;
            lastSwingConnected = swungConnected;
            comboExpireTime = Time.time + comboResetTime;
            if (!lastSwingConnected)
                comboIndex = 0;

            if (notifyEnded)
                AttackEnded?.Invoke();
        }

        void StartSwing(Vector3 aimDirection)
        {
            FaceAim(aimDirection);

            // Por si un hitstop anterior dejó la cámara lenta.
            RestorePlayerAnimatorSpeed();

            isAttacking = true;
            attackStartedAt = Time.time;
            hitWindowOpen = false;
            hitResolvedThisSwing = false;
            swungConnected = false;
            lastHitPoint = Vector3.zero;
            hitThisSwing.Clear();
            bufferedInput = false;

            int step = Mathf.Clamp(comboIndex, 0, maxComboSteps - 1);
            string stateName = AttackStateNames[step];

            if (animator != null)
            {
                if (HasIntParam(ComboIndexHash))
                    animator.SetInteger(ComboIndexHash, step);

                // Play a t=0 obligatorio: el mismo clip en Attack1/2/3 no reinicia con CrossFade.
                int stateHash = Animator.StringToHash(stateName);
                if (animator.HasState(0, stateHash))
                {
                    animator.Play(stateHash, 0, 0f);
                    animator.Update(0f);
                }
                else if (animator.HasState(0, Animator.StringToHash("Attack")))
                {
                    animator.Play(Animator.StringToHash("Attack"), 0, 0f);
                    animator.Update(0f);
                }
                else
                    animator.SetTrigger("Attack");
            }

            // Ventana de combo viva durante el swing + margen post-golpe.
            comboExpireTime = Time.time + comboResetTime + 2.0f;
            StopSafety();
            safetyRoutine = StartCoroutine(SwingSafetyTimeout(step));
            StopHardFallback();
            hardFallbackRoutine = StartCoroutine(HardAttackFallback());

            AttackStarted?.Invoke();

            var catalog = DungeonSfxCatalog.Instance;
            if (catalog != null && catalog.swordSwing != null)
                GameSfx.PlayCombat(catalog.swordSwing);

            // Espadachina / asesina: step-in corto; WASD queda bloqueado vía LocksMoveInput.
            if (attackStyle == AttackStyle.SwordArc)
                BeginMeleeLunge(aimDirection);
        }

        IEnumerator SwingSafetyTimeout(int step)
        {
            // Esperar un frame a que el Animator entre en el estado.
            yield return null;

            float clipLen = 0.6f;
            if (animator != null)
            {
                var info = animator.GetCurrentAnimatorStateInfo(0);
                if (info.length > 0.05f)
                    clipLen = info.length / Mathf.Max(0.01f, info.speedMultiplier);
            }

            float hitWait = clipLen * 0.40f;
            float endWait = clipLen * 0.90f;

            yield return new WaitForSeconds(hitWait);
            if (!isAttacking)
                yield break;
            if (!hitResolvedThisSwing)
                OnAttackHitFrame();

            float remaining = Mathf.Max(0.05f, endWait - hitWait);
            yield return new WaitForSeconds(remaining);
            if (isAttacking)
                OnAttackEndFrame();
        }

        IEnumerator HardAttackFallback()
        {
            yield return new WaitForSeconds(Mathf.Max(0.4f, attackFallbackSeconds));
            if (isAttacking)
                ForceEndAttack(notifyEnded: true);
            hardFallbackRoutine = null;
        }

        void BeginMeleeLunge(Vector3 aimDirection)
        {
            StopLunge();
            if (meleeLungeDistance <= 0.01f || meleeLungeDuration <= 0.01f)
                return;

            Vector3 dir = aimDirection;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f)
                dir = GetFlatForward();
            else
                dir.Normalize();

            lungeRoutine = StartCoroutine(MeleeLungeRoutine(dir));
        }

        IEnumerator MeleeLungeRoutine(Vector3 direction)
        {
            float duration = Mathf.Clamp(meleeLungeDuration, 0.25f, 0.4f);
            float distance = Mathf.Max(0f, meleeLungeDistance);
            float elapsed = 0f;
            float covered = 0f;

            while (elapsed < duration && isAttacking)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease-out: más empuje al inicio, frena al final; total ≈ meleeLungeDistance.
                float targetCovered = distance * (1f - (1f - t) * (1f - t));
                float step = targetCovered - covered;
                covered = targetCovered;
                if (characterController != null && characterController.enabled)
                    characterController.Move(direction * step);
                else
                    transform.position += direction * step;
                yield return null;
            }

            lungeRoutine = null;
        }

        void StopLunge()
        {
            if (lungeRoutine == null)
                return;
            StopCoroutine(lungeRoutine);
            lungeRoutine = null;
        }

        void StopSafety()
        {
            if (safetyRoutine == null)
                return;
            StopCoroutine(safetyRoutine);
            safetyRoutine = null;
        }

        void StopHardFallback()
        {
            if (hardFallbackRoutine == null)
                return;
            StopCoroutine(hardFallbackRoutine);
            hardFallbackRoutine = null;
        }

        void ResolveHits(int step)
        {
            Vector3 forward = GetFlatForward();
            float reach = GetAttackReach();
            // Punto de impacto preferido: delante del personaje a distancia de arma
            // (el tip del mesh puede quedar detrás según el socket del bastón).
            Vector3 strikePoint = transform.position + Vector3.up * 1.0f + forward * reach;
            bool punctual = step >= maxComboSteps - 1;
            int damage = GetDamage(step);

            if (punctual)
                ResolvePunctualHits(forward, reach, strikePoint, damage);
            else
                ResolveArcHits(forward, reach, strikePoint, damage);
        }

        void ResolveArcHits(Vector3 forward, float reach, Vector3 strikePoint, int damage)
        {
            // Esfera centrada a media distancia hacia delante: cubre desde el pecho hasta el tip.
            Vector3 origin = transform.position + Vector3.up * 1.0f + forward * (reach * 0.5f);
            float radius = Mathf.Max(0.55f, reach * 0.55f);
            float halfAngle = GetArcAngle() * 0.5f;
            float maxDist = reach + 0.35f;

            // Ignore triggers: el minion tiene BoxCollider trigger + CharacterController;
            // ambos resolvían al mismo EnemyHealth, pero evita ruido/doble overlap.
            int count = Physics.OverlapSphereNonAlloc(origin, radius, overlapBuffer, hitMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider col = overlapBuffer[i];
                if (col == null || col.isTrigger || !IsValidEnemy(col))
                    continue;

                Vector3 to = col.bounds.center - transform.position;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist > maxDist)
                    continue;

                if (dist > 0.05f)
                {
                    float angle = Vector3.Angle(forward, to / dist);
                    if (angle > halfAngle)
                        continue;
                }

                ApplyHit(col, damage, strikePoint);
            }
        }

        void ResolvePunctualHits(Vector3 forward, float reach, Vector3 strikePoint, int damage)
        {
            Vector3 chest = transform.position + Vector3.up * 1.0f;
            Vector3 tip = chest + forward * reach;
            float radius = Mathf.Clamp(punctualRadius, 0.25f, 0.55f);

            int count = Physics.OverlapCapsuleNonAlloc(
                chest, tip, radius, overlapBuffer, hitMask, QueryTriggerInteraction.Ignore);
            if (count == 0)
                count = Physics.OverlapSphereNonAlloc(tip, radius, overlapBuffer, hitMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider col = overlapBuffer[i];
                if (col == null || col.isTrigger || !IsValidEnemy(col))
                    continue;

                Vector3 to = col.bounds.center - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.001f && Vector3.Angle(forward, to.normalized) > 40f)
                    continue;

                ApplyHit(col, damage, tip);
            }
        }

        void ApplyHit(Collider col, int damage, Vector3 fallbackPoint)
        {
            EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy == null)
                return;

            if (!hitThisSwing.Add(enemy))
                return;

            swungConnected = true;
            enemy.TakeDamage(damage);

            var catalog = DungeonSfxCatalog.Instance;
            if (catalog != null && catalog.swordHit != null)
                GameSfx.PlayCombat(catalog.swordHit);

            Vector3 hitPoint = col.ClosestPoint(fallbackPoint);
            lastHitPoint = hitPoint;
            EnemyHit?.Invoke(enemy, hitPoint);

            // Impacto pequeño solo en hit real; el pop de slash grande va en el finisher.
            if (impactVfxPrefab != null)
            {
                var fx = Instantiate(impactVfxPrefab, hitPoint, Quaternion.identity);
                fx.transform.localScale = Vector3.one * 0.45f;
                Destroy(fx, 1.2f);
            }

            // Hitstop SOLO en el atacante. Congelar el Animator del esqueleto provoca
            // T-pose / animaciones atascadas al mezclar Hit + Attack + muerte.
            BeginHitstop();
        }

        void BeginHitstop()
        {
            if (hitstopRoutine != null)
            {
                StopCoroutine(hitstopRoutine);
                hitstopRoutine = null;
                RestorePlayerAnimatorSpeed();
            }

            hitstopRoutine = StartCoroutine(HitstopRoutine());
        }

        IEnumerator HitstopRoutine()
        {
            float playerRestore = defaultAnimatorSpeed > 0.1f ? defaultAnimatorSpeed : 1f;

            if (animator != null)
                animator.speed = hitstopSpeed;

            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, hitstopDuration));

            RestorePlayerAnimatorSpeed(playerRestore);
            hitstopRoutine = null;
        }

        void RestorePlayerAnimatorSpeed(float speed = -1f)
        {
            if (animator == null)
                return;
            float restore = speed > 0.1f ? speed : (defaultAnimatorSpeed > 0.1f ? defaultAnimatorSpeed : 1f);
            animator.speed = restore;
            defaultAnimatorSpeed = restore;
        }

        bool IsValidEnemy(Collider col)
        {
            if (!string.IsNullOrEmpty(enemyTag) && col.CompareTag(enemyTag))
                return true;
            return col.GetComponentInParent<EnemyHealth>() != null;
        }

        int damageBonus;
        float reachBonus;

        public void AddDamageBonus(int amount)
        {
            if (amount == 0)
                return;
            damageBonus += amount;
        }

        public void AddReachBonus(float amount)
        {
            if (Mathf.Abs(amount) < 0.001f)
                return;
            reachBonus += amount;
        }

        int GetDamage(int step)
        {
            if (damagePerStep == null || damagePerStep.Length == 0)
                return 30 + damageBonus;
            return damagePerStep[Mathf.Clamp(step, 0, damagePerStep.Length - 1)] + damageBonus;
        }

        public float GetWeaponLength()
        {
            if (weaponBaseTransform != null && weaponTipTransform != null)
            {
                float d = Vector3.Distance(weaponBaseTransform.position, weaponTipTransform.position);
                if (d > 0.05f)
                    return d;
            }

            return Mathf.Max(0.5f, fallbackWeaponLength);
        }

        public float GetAttackReach()
        {
            return Mathf.Max(0.8f, GetWeaponLength() * Mathf.Max(1f, reachMultiplier) + Mathf.Max(0f, reachBonus));
        }

        Vector3 GetFlatForward()
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = transform.forward;
            return forward.normalized;
        }

        void FaceAim(Vector3 aimDirection)
        {
            Vector3 flat = aimDirection;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.001f)
                return;
            transform.rotation = Quaternion.LookRotation(flat.normalized);
        }

        bool HasIntParam(int hash)
        {
            if (animator == null)
                return false;
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == hash && p.type == AnimatorControllerParameterType.Int)
                    return true;
            }
            return false;
        }

        void OnDrawGizmosSelected()
        {
            float length = Application.isPlaying ? GetAttackReach() : EstimateEditorLength() * Mathf.Max(1f, reachMultiplier);
            Vector3 chest = transform.position + Vector3.up * 1.0f;
            Vector3 forward = GetFlatForward();

            Color fill = new Color(1f, 0.15f, 0.1f, 0.22f);
            Color wire = new Color(1f, 0.2f, 0.15f, 0.85f);
            Gizmos.color = wire;

            Vector3 tipPos = chest + forward * length;
            Gizmos.DrawLine(chest, tipPos);
            Gizmos.DrawWireSphere(tipPos, 0.06f);

            DrawArcGizmo(chest, forward, length, arcAngle, fill, wire);

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.7f);
            Gizmos.DrawWireSphere(tipPos, punctualRadius);
            Gizmos.DrawWireSphere(chest + forward * (length * 0.5f), Mathf.Max(0.55f, length * 0.55f));
        }

        float EstimateEditorLength()
        {
            if (weaponBaseTransform != null && weaponTipTransform != null)
            {
                float d = Vector3.Distance(weaponBaseTransform.position, weaponTipTransform.position);
                if (d > 0.05f)
                    return d;
            }
            return fallbackWeaponLength;
        }

        static void DrawArcGizmo(Vector3 origin, Vector3 forward, float radius, float angle, Color fill, Color wire)
        {
            int segments = 24;
            float half = angle * 0.5f;
            Vector3 prev = origin + Quaternion.AngleAxis(-half, Vector3.up) * forward * radius;

            Gizmos.color = wire;
            Gizmos.DrawLine(origin, prev);
            for (int i = 1; i <= segments; i++)
            {
                float a = Mathf.Lerp(-half, half, i / (float)segments);
                Vector3 next = origin + Quaternion.AngleAxis(a, Vector3.up) * forward * radius;
                Gizmos.color = fill;
                Gizmos.DrawLine(origin, next);
                Gizmos.color = wire;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
            Gizmos.DrawLine(origin, prev);
        }

#if UNITY_EDITOR
        public void EditorAssignRefs(
            Animator anim,
            Transform weaponBase,
            Transform weaponTip,
            AttackArcVFX vfx,
            AttackStyle style,
            float fallbackLength,
            GameObject impactPrefab,
            float reachMult = 1.9f)
        {
            animator = anim;
            weaponBaseTransform = weaponBase;
            weaponTipTransform = weaponTip;
            attackArcVfx = vfx;
            attackStyle = style;
            fallbackWeaponLength = fallbackLength;
            impactVfxPrefab = impactPrefab;
            reachMultiplier = Mathf.Max(1f, reachMult);
            if (characterController == null)
                characterController = GetComponent<CharacterController>()
                                     ?? GetComponentInParent<CharacterController>();
        }
#endif
    }
}
