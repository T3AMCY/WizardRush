using UnityEngine;

public class ArmorItem : EquipmentItem
{
    public ArmorItem(ItemData itemData)
        : base(itemData)
    {
        if (itemData.Type != ItemType.Armor)
        {
            Logger.LogError(
                typeof(ArmorItem),
                $"Attempted to create ArmorItem with non-armor ItemData: {itemData.Type}"
            );
        }
    }

    public override void Initialize(ItemData data)
    {
        base.Initialize(data);
    }

    protected override void ValidateItemType(ItemType type)
    {
        if (type != ItemType.Armor)
        {
            Logger.LogError(
                typeof(ArmorItem),
                $"잘못된 아이템 타입입니다: {type}. ArmorItem은 ItemType.Armor이어야 합니다."
            );
        }
    }
}
