using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace GhoulDuck
{
    // Retains the settings mod's existing rows and controls; only their layout changes.
    internal static class SettingsGridPatch
    {
        private const string GroupKey = "GhoulDuck.ModBehaviour.settings.enemyTypes";
        private static PropertyInfo storeKeyProperty;

        internal static void RenderNodePostfix(RectTransform parent, object node)
        {
            try
            {
                if (!parent || node == null) return;
                if (storeKeyProperty == null || storeKeyProperty.DeclaringType != node.GetType())
                    storeKeyProperty = AccessTools.Property(node.GetType(), "StoreKey");
                if (storeKeyProperty == null || !string.Equals(storeKeyProperty.GetValue(node, null) as string, GroupKey, StringComparison.Ordinal)) return;

                var group = parent.Find("Group 可食用的敌人类型");
                var body = group ? group.Find("Indented Content/Children") as RectTransform : null;
                if (!body || body.Find("GhoulDuck_EnemyGrid")) return;
                var oldLayout = body.GetComponent<VerticalLayoutGroup>();
                if (!oldLayout) return;

                var rows = new List<Transform>();
                foreach (Transform child in body)
                {
                    if (!child.name.StartsWith("Setting settings.enemyTypes.", StringComparison.Ordinal)) return;
                    rows.Add(child);
                }
                if (rows.Count == 0) return;

                // Unity does not allow two LayoutGroups on the same object. Keep the
                // existing vertical group and place the grid on a new child instead.
                var gridObject = new GameObject("GhoulDuck_EnemyGrid", typeof(RectTransform));
                gridObject.transform.SetParent(body, false);
                var grid = gridObject.AddComponent<EnemyTypeGrid>();
                if (!grid)
                {
                    UnityEngine.Object.Destroy(gridObject);
                    return;
                }
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
                grid.spacing = new Vector2(6f, 5f);
                grid.cellSize = new Vector2(100f, 48f);

                foreach (Transform child in rows)
                {
                    child.SetParent(gridObject.transform, false);
                    var row = child.GetComponent<HorizontalLayoutGroup>();
                    if (row) { row.padding = new RectOffset(8, 8, 5, 5); row.spacing = 6f; }
                    var label = child.Find("Name");
                    var labelLayout = label ? label.GetComponent<LayoutElement>() : null;
                    if (labelLayout) labelLayout.minWidth = 0f;
                    var control = child.Find("Control");
                    var controlLayout = control ? control.GetComponent<LayoutElement>() : null;
                    if (controlLayout) { controlLayout.minWidth = 30f; controlLayout.preferredWidth = 30f; }
                }

                LayoutRebuilder.MarkLayoutForRebuild(body);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GhoulDuck] 无法调整敌人类型网格：" + e.Message);
            }
        }
    }

    public sealed class EnemyTypeGrid : GridLayoutGroup
    {
        public override void SetLayoutHorizontal()
        {
            float width = Mathf.Max(0f, rectTransform.rect.width - padding.horizontal - spacing.x * 2f);
            cellSize = new Vector2(width / 3f, 48f);
            base.SetLayoutHorizontal();
        }
    }
}
