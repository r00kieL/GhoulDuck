using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using SodaCraft.Localizations;
using UnityEngine;

namespace GhoulDuck
{
    [Serializable]
    public class EnemyTypeSettings
    {
        [InspectorName("拾荒者")]
        [Tooltip("包括普通、暴走和雪地拾荒者。")]
        public bool scavenger = true;
        [InspectorName("雇佣兵")]
        public bool mercenary = false;
        [InspectorName("游荡者")]
        public bool raider = false;
        [InspectorName("Boss")]
        [Tooltip("优先识别游戏中标记为 Boss 的敌人。")]
        public bool boss = false;
        [InspectorName("机械蜘蛛")]
        public bool spider = false;
        [InspectorName("狼、狗")]
        public bool wolfOrDog = false;
        [InspectorName("幽灵")]
        public bool ghost = false;
        [InspectorName("风暴生物")]
        public bool stormCreature = false;
        [InspectorName("行走菇")]
        public bool mushroom = false;

        internal bool IsEnabled(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Scavenger: return scavenger;
                case EnemyType.Mercenary: return mercenary;
                case EnemyType.Raider: return raider;
                case EnemyType.Boss: return boss;
                case EnemyType.Spider: return spider;
                case EnemyType.WolfOrDog: return wolfOrDog;
                case EnemyType.Ghost: return ghost;
                case EnemyType.StormCreature: return stormCreature;
                case EnemyType.Mushroom: return mushroom;
                default: return false;
            }
        }
    }

    [Serializable]
    public class Settings
    {
        [InspectorName("可食用的敌人类型")]
        [Tooltip("勾选后，该类型死亡盒子的交互列表才会出现“吃”。")]
        public EnemyTypeSettings enemyTypes = new EnemyTypeSettings();

        [InspectorName("吃掉后清除剩余战利品")]
        [Tooltip("开启后，食用会清除盒内剩余战利品；关闭时剩余战利品会散落在原地。")]
        public bool clearRemainingLoot = false;

        [InspectorName("恢复生命值")]
        [Tooltip("每次食用恢复的生命值；0 表示不恢复。")]
        public float health = 0f;

        [InspectorName("恢复能量")]
        [Tooltip("每次食用恢复的能量（黄色闪电图标），默认 30 点；不会超过能量上限。")]
        public float stamina = 30f;

        [InspectorName("恢复水分")]
        [Tooltip("每次食用恢复的水分；0 表示不恢复。")]
        public float water = 0f;
    }

    // Group members are selected through the loot box's existing collider.
    // They must not create an extra collider or a second world marker.
    public sealed class EatInteraction : InteractableBase
    {
        internal CorpseMarker Owner;
        protected override void Awake() { }
        protected override void Start() { }
        protected override bool IsInteractable()
        {
            return Owner && !Owner.Consumed && Owner.Box;
        }
        protected override void OnInteractFinished()
        {
            if (ModBehaviour.Instance && Owner && interactCharacter)
                ModBehaviour.Instance.Eat(Owner, interactCharacter);
        }
    }

    public sealed class CorpseMarker : MonoBehaviour
    {
        private static readonly FieldInfo GroupMembers = AccessTools.Field(typeof(InteractableBase), "otherInterablesInGroup");
        private static readonly FieldInfo InteractionTime = AccessTools.Field(typeof(InteractableBase), "interactTime");
        private static readonly FieldInfo HudTargetCache = AccessTools.Field(typeof(InteractHUD), "interactableMasterTemp");
        public InteractableLootbox Box;
        private Transform originalHideIfEmpty;
        private EatInteraction eatInteraction;
        private bool originalGroupEnabled;
        private EnemyType enemyType;
        private bool typeEnabled;
        public bool Consumed;

        internal void Initialize(InteractableLootbox box, EnemyType type, bool enabled)
        {
            if (GroupMembers == null) throw new MissingFieldException("InteractableBase.otherInterablesInGroup");
            if (InteractionTime == null) throw new MissingFieldException("InteractableBase.interactTime");
            Box = box;
            enemyType = type;
            originalHideIfEmpty = box.hideIfEmpty;
            originalGroupEnabled = box.interactableGroup;
            var members = GroupMembers.GetValue(box) as List<InteractableBase>;
            if (members == null)
            {
                members = new List<InteractableBase>();
                GroupMembers.SetValue(box, members);
            }

            var actionObject = new GameObject("GhoulDuck_EatInteraction");
            actionObject.transform.SetParent(box.transform, false);
            eatInteraction = actionObject.AddComponent<EatInteraction>();
            eatInteraction.Owner = this;
            InteractionTime.SetValue(eatInteraction, 1.5f);
            eatInteraction.InteractName = "GhoulDuck_Interact_Eat";
            eatInteraction.zoomIn = false;
            members.Insert(0, eatInteraction);
            box.interactableGroup = true;
            typeEnabled = enabled;
            if (enabled)
                box.hideIfEmpty = null;
            else
            {
                eatInteraction.gameObject.SetActive(false);
                if (!originalGroupEnabled) box.interactableGroup = false;
            }
        }
        internal EnemyType Type { get { return enemyType; } }

        internal void SetTypeEnabled(bool enabled)
        {
            if (!Box || !eatInteraction || typeEnabled == enabled) return;
            typeEnabled = enabled;
            if (!enabled && eatInteraction.Interacting) eatInteraction.StopInteract();
            eatInteraction.gameObject.SetActive(enabled);

            if (enabled)
            {
                Box.interactableGroup = true;
                // Restore an empty box hidden by the vanilla loot interaction, if applicable.
                if (originalHideIfEmpty && !originalHideIfEmpty.gameObject.activeSelf &&
                    Box.Inventory && Box.Inventory.IsEmpty())
                    originalHideIfEmpty.gameObject.SetActive(true);
                Box.hideIfEmpty = null;
            }
            else
            {
                if (!originalGroupEnabled) Box.interactableGroup = false;
                Box.hideIfEmpty = originalHideIfEmpty;
                if (originalHideIfEmpty && Box.Inventory) Box.CheckHideIfEmpty();
            }
            RefreshNearbyHud();
        }

        private void RefreshNearbyHud()
        {
            var player = CharacterMainControl.Main;
            if (!player || !player.interactAction || player.interactAction.MasterInteractableAround != Box) return;
            player.interactAction.SwitchInteractable(0);
            if (HudTargetCache == null) return;
            foreach (var hud in Resources.FindObjectsOfTypeAll<InteractHUD>())
                if (hud && hud.gameObject.scene.IsValid()) HudTargetCache.SetValue(hud, null);
        }
        private void OnDestroy()
        {
            if (!Box) return;
            var members = GroupMembers == null ? null : GroupMembers.GetValue(Box) as List<InteractableBase>;
            if (members != null && eatInteraction) members.Remove(eatInteraction);
            if (!originalGroupEnabled && (members == null || members.Count == 0)) Box.interactableGroup = false;
            if (eatInteraction) Destroy(eatInteraction.gameObject);
            if (!Consumed && originalHideIfEmpty)
            {
                Box.hideIfEmpty = originalHideIfEmpty;
                if (Box.Inventory) Box.CheckHideIfEmpty();
            }
        }
    }

    public sealed class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private Harmony harmony;
        private bool settingsGridPatched;
        private float nextSettingsGridCheck;
        internal static int DeathDepth;
        internal static EnemyType CurrentEnemyType;
        private static readonly FieldInfo CharacterIconType = AccessTools.Field(typeof(CharacterRandomPreset), "characterIconType");
        private readonly List<CorpseMarker> corpses = new List<CorpseMarker>();
        internal static ModBehaviour Instance;
        [SerializeField, InspectorName("食尸鬼鸭")]
        private Settings settings = new Settings();
        private string ConfigPath { get { return Path.Combine(Application.persistentDataPath, "GhoulDuck.json"); } }

        private void Awake()
        {
            Instance = this;
            LoadSettings();
            SetInteractionText();
            LocalizationManager.OnSetLanguage += OnLanguageChanged;
            try
            {
                harmony = new Harmony("com.ghoulduck.eatroastduck");
                harmony.PatchAll(typeof(ModBehaviour).Assembly);
                Debug.Log("[GhoulDuck] 已加载，吃烤鸭已加入交互菜单。");
            }
            catch (Exception e) { if (harmony != null) harmony.UnpatchAll("com.ghoulduck.eatroastduck"); Debug.LogError("[GhoulDuck] 初始化失败：" + e); }
        }
        internal static EnemyType Classify(CharacterMainControl character)
        {
            if (!character || !character.characterPreset) return EnemyType.Unknown;
            var preset = character.characterPreset;
            bool boss = false;
            try
            {
                boss = CharacterIconType != null &&
                    (CharacterIconTypes)CharacterIconType.GetValue(preset) == CharacterIconTypes.boss;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GhoulDuck] 无法读取 Boss 标记，将按敌人名称识别：" + e.Message);
            }
            return EnemyClassification.Classify(preset.nameKey, boss);
        }
        internal void Register(InteractableLootbox box, EnemyType type)
        {
            if (!box || type == EnemyType.Unknown || box.GetComponent<CorpseMarker>()) return;
            var marker = box.gameObject.AddComponent<CorpseMarker>();
            try
            {
                marker.Initialize(box, type, settings.enemyTypes.IsEnabled(type));
                corpses.Add(marker);
            }
            catch (Exception e)
            {
                Destroy(marker);
                Debug.LogError("[GhoulDuck] 无法加入烤鸭交互菜单：" + e);
            }
        }
        private static void SetInteractionText()
        {
            LocalizationManager.SetOverrideText("GhoulDuck_Interact_Eat", "吃");
        }
        private void OnLanguageChanged(SystemLanguage language) { SetInteractionText(); }
        private void Update()
        {
            if (settingsGridPatched || harmony == null || Time.unscaledTime < nextSettingsGridCheck) return;
            nextSettingsGridCheck = Time.unscaledTime + 2f;
            var pageType = AccessTools.TypeByName("SlimeNull.DuckovModSettings.UI.SettingsPage");
            if (pageType == null) return;
            var renderNode = AccessTools.Method(pageType, "RenderNode");
            var postfix = AccessTools.Method(typeof(SettingsGridPatch), "RenderNodePostfix");
            if (renderNode == null || postfix == null)
            {
                settingsGridPatched = true;
                Debug.LogWarning("[GhoulDuck] 设置模组界面不兼容网格布局，保留原有列表。");
                return;
            }
            try
            {
                harmony.Patch(renderNode, postfix: new HarmonyMethod(postfix));
                settingsGridPatched = true;
            }
            catch (Exception e)
            {
                settingsGridPatched = true;
                Debug.LogWarning("[GhoulDuck] 网格布局初始化失败，保留原有列表：" + e.Message);
            }
        }
        private void OnDestroy()
        {
            LocalizationManager.OnSetLanguage -= OnLanguageChanged;
            if (harmony != null) harmony.UnpatchAll("com.ghoulduck.eatroastduck");
            foreach (var corpse in corpses) if (corpse) Destroy(corpse);
            corpses.Clear();
            Instance = null;
            DeathDepth = 0;
            CurrentEnemyType = EnemyType.Unknown;
        }
        internal bool CanEat(CorpseMarker marker, CharacterMainControl player)
        {
            if (!marker || marker.Consumed || !settings.enemyTypes.IsEnabled(marker.Type) ||
                !marker.Box || !marker.Box.Inventory || marker.Box.Inventory.Loading)
            {
                if (player) player.PopText("烤鸭尚未准备好");
                return false;
            }
            return true;
        }
        internal void Eat(CorpseMarker marker, CharacterMainControl player)
        {
            if (!CanEat(marker, player)) return;
            var box = marker.Box;
            var inventory = box.Inventory;
            try
            {
                var items = new List<Item>();
                foreach (var item in inventory) if (item) items.Add(item);
                foreach (var item in items)
                {
                    if (settings.clearRemainingLoot)
                    {
                        item.Detach();
                        item.DestroyTree();
                    }
                    else
                    {
                        DropLoot(item, inventory, box.transform.position);
                    }
                }
                if (!inventory.IsEmpty()) { player.PopText("物品处理失败，未吃掉烤鸭"); return; }
                marker.Consumed = true;
                player.CurrentEnergy = Mathf.Clamp(player.CurrentEnergy + settings.stamina, 0f, player.MaxEnergy);
                if (settings.health > 0f) player.Health.AddHealth(settings.health);
                if (settings.water > 0f) player.CurrentWater = Mathf.Min(player.MaxWater, player.CurrentWater + settings.water);
                Destroy(box.gameObject);
                player.PopText("好吃！");
            }
            catch (Exception e) { Debug.LogError("[GhoulDuck] 吃烤鸭失败：" + e); player.PopText("吃烤鸭失败，请查看日志"); }
        }
        private static void DropLoot(Item item, Inventory inventory, Vector3 position)
        {
            try
            {
                if (!item.Drop(position + Vector3.up * 0.3f, true, Vector3.forward, 360f))
                    throw new InvalidOperationException("掉落物品失败");
            }
            catch
            {
                // Drop detaches the item before creating its pickup. If creation fails
                // before a pickup exists, return it to the original box when possible.
                if (item && !item.IsBeingDestroyed && !item.ActiveAgent && item.ParentObject == null)
                {
                    try
                    {
                        if (!inventory.AddItem(item))
                            Debug.LogError("[GhoulDuck] 物品掉落失败且无法放回盒子：" + item.name);
                    }
                    catch (Exception restoreError)
                    {
                        Debug.LogError("[GhoulDuck] 物品掉落失败，放回盒子时发生错误：" + restoreError);
                    }
                }
                throw;
            }
        }
        // The old file is read only as an initial fallback. Duckov Mod Settings
        // restores its own saved values afterwards and owns future writes.
        private void LoadSettings()
        {
            settings = new Settings();
            try
            {
                if (File.Exists(ConfigPath))
                    settings = JsonUtility.FromJson<Settings>(File.ReadAllText(ConfigPath)) ?? new Settings();
                OnValidate();
            }
            catch (Exception e)
            {
                settings = new Settings();
                Debug.LogWarning("[GhoulDuck] 旧设置读取失败，使用默认值：" + e.Message);
            }
        }
        private void OnValidate()
        {
            if (settings == null) settings = new Settings();
            if (settings.enemyTypes == null) settings.enemyTypes = new EnemyTypeSettings();
            settings.health = ClampAmount(settings.health);
            settings.stamina = ClampAmount(settings.stamina);
            settings.water = ClampAmount(settings.water);
            for (int i = corpses.Count - 1; i >= 0; --i)
            {
                var marker = corpses[i];
                if (!marker) { corpses.RemoveAt(i); continue; }
                marker.SetTypeEnabled(settings.enemyTypes.IsEnabled(marker.Type));
            }
        }
        private static float ClampAmount(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return 0f;
            return Mathf.Clamp(value, 0f, 10000f);
        }

    }

    // Validate before starting the action, while keeping the menu option visible.
    [HarmonyPatch(typeof(InteractableBase), "StartInteract")]
    internal static class EatStartPatch
    {
        private static bool Prefix(InteractableBase __instance, CharacterMainControl _interactCharacter, ref bool __result)
        {
            var eat = __instance as EatInteraction;
            if (!eat) return true;
            if (ModBehaviour.Instance && ModBehaviour.Instance.CanEat(eat.Owner, _interactCharacter)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(CharacterMainControl), "OnDead")]
    internal static class DeathPatch
    {
        private struct DeathScope { public bool active; public EnemyType previous; }
        private static void Prefix(CharacterMainControl __instance, out DeathScope __state)
        {
            __state = new DeathScope { active = __instance && !__instance.IsMainCharacter, previous = ModBehaviour.CurrentEnemyType };
            if (!__state.active) return;
            try { ModBehaviour.CurrentEnemyType = ModBehaviour.Classify(__instance); }
            catch (Exception e)
            {
                ModBehaviour.CurrentEnemyType = EnemyType.Unknown;
                Debug.LogWarning("[GhoulDuck] 无法识别敌人类型：" + e.Message);
            }
            ModBehaviour.DeathDepth++;
        }
        private static Exception Finalizer(Exception __exception, DeathScope __state)
        {
            if (__state.active)
            {
                ModBehaviour.DeathDepth--;
                ModBehaviour.CurrentEnemyType = __state.previous;
            }
            return __exception;
        }
    }
    [HarmonyPatch(typeof(InteractableLootbox), "CreateFromItem")]
    internal static class LootPatch
    {
        private static void Postfix(InteractableLootbox __result)
        { if (ModBehaviour.DeathDepth > 0 && ModBehaviour.Instance && __result) ModBehaviour.Instance.Register(__result, ModBehaviour.CurrentEnemyType); }
    }
}
