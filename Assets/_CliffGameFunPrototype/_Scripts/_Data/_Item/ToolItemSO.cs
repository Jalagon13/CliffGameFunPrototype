using UnityEngine;

namespace CliffGame
{
    public enum ToolType
    {
        Hammer,
        Axe,
        Pickaxe,
        Spear,
        None
    }

    [CreateAssetMenu(fileName = "New Tool Item Data", menuName = "Item/Tool")]
    public class ToolItemSO : ItemSO
    {
        [field: SerializeField]
        public ToolType ToolType { get; private set; }

        [field: SerializeField]
        public float SwingCooldownInSeconds { get; private set; } = 0.325f;

        [field: SerializeField]
        public int NpcDamageAmount { get; private set; } = 2;

        [field: Header("Resource Damage")]
        [field: SerializeField]
        public int ResourceDamageMin { get; private set; } = 1;

        [field: SerializeField]
        public int ResourceDamageMax { get; private set; } = 1;

        [field: Header("Durability")]
        [field: SerializeField]
        public int MaxDurability { get; private set; } = 10;

        [field: SerializeField, Tooltip("Can repair amount depending on tool type")]
        public int IntValue { get; private set; }

        [field: SerializeField]
        public GameObject HeldToolPrefab { get; private set; }

        [field: Header("Spear Tether")]
        [field: SerializeField]
        public bool CanThrowTethered { get; private set; } = false;

        [field: SerializeField]
        public float TetherChargeDuration { get; private set; } = 1f;

        [field: SerializeField]
        public float TetherRange { get; private set; } = 15f;

        [field: SerializeField]
        public float TetherSequenceDuration { get; private set; } = 1.85f;

        [field: SerializeField, Tooltip("How strongly gravity pulls this spear while it's flying outward. 1 = normal gravity.")]
        public float TetherGravityMultiplier { get; private set; } = 1f;

        public override InventoryItem CreateInventoryItem(int quantity)
        {
            return new(this, quantity);
        }

        public override string GetDescription()
        {
            string description = GetDescriptionBreak();
            description += $"<br>Resource Damage: {Mathf.Min(ResourceDamageMin, ResourceDamageMax)}-{Mathf.Max(ResourceDamageMin, ResourceDamageMax)}";
            description += $"<br>Max Durability: {Mathf.Max(1, MaxDurability)}";
            return description;
        }
    }
}
