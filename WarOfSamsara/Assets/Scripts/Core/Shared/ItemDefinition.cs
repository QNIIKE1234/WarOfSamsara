using System;

namespace MapleMMO.Shared
{
    public enum InventoryType
    {
        Equip = 1,
        Use = 2,
        Setup = 3,
        Etc = 4,
        Cash = 5
    }

    public enum EquipSlot
    {
        None = 0,
        Hat = 1,
        FaceAccessory = 2,
        EyeAccessory = 3,
        Top = 4,
        Bottom = 5,
        Shoes = 6,
        Gloves = 7,
        Cape = 8,
        Shield = 9,
        Weapon = 10
    }

    [Serializable]
    public class ItemDefinition
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public InventoryType Tab { get; set; } = InventoryType.Etc;
        public EquipSlot Slot { get; set; } = EquipSlot.None;

        public int Price { get; set; }
        public int RequiredLevel { get; set; }
        public int RequiredStr { get; set; }
        public int RequiredDex { get; set; }
        public int RequiredInt { get; set; }
        public int RequiredLuk { get; set; }

        // Bonus stats if equip
        public int AddStr { get; set; }
        public int AddDex { get; set; }
        public int AddInt { get; set; }
        public int AddLuk { get; set; }
        public int AddWeaponAttack { get; set; }
        public int AddMagicAttack { get; set; }
        public int AddSpeed { get; set; }
        public int AddJump { get; set; }

        // Max stack size (Equip = 1, Use/Etc = 100-200)
        public int MaxStack { get; set; } = 100;
    }
}
