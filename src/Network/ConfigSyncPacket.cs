using Burdened.Config;
using ProtoBuf;

namespace Burdened.Network;

/// <summary>
/// Server -> client on join: the effective sanitized config, so the
/// client HUD/dialog/rendering mirror exactly what the server enforces.
/// </summary>
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ConfigSyncPacket
{
    public int HotbarSlots;
    public int BagSlots;
    public bool ImmersiveCarryingMode;
    public bool HideBagContentsInDialog;
    public bool OffhandHoldsAnything;
    public bool AutoPickupToBags;
    public bool ImprovedBagInteractions;
    public bool RememberDialogPlacement;

    public static ConfigSyncPacket From(BurdenedConfig config)
    {
        return new ConfigSyncPacket
        {
            HotbarSlots = config.HotbarSlots,
            BagSlots = config.BagSlots,
            ImmersiveCarryingMode = config.ImmersiveCarryingMode,
            HideBagContentsInDialog = config.HideBagContentsInDialog,
            OffhandHoldsAnything = config.OffhandHoldsAnything,
            AutoPickupToBags = config.AutoPickupToBags,
            ImprovedBagInteractions = config.ImprovedBagInteractions,
            RememberDialogPlacement = config.RememberDialogPlacement,
        };
    }
        
    public BurdenedConfig ToConfig()
    {
        var config = new BurdenedConfig
        {
            HotbarSlots = HotbarSlots,
            BagSlots = BagSlots,
            ImmersiveCarryingMode = ImmersiveCarryingMode,
            HideBagContentsInDialog = HideBagContentsInDialog,
            OffhandHoldsAnything = OffhandHoldsAnything,
            AutoPickupToBags = AutoPickupToBags,
            ImprovedBagInteractions = ImprovedBagInteractions,
            RememberDialogPlacement = RememberDialogPlacement,
        };
        config.Sanitize();
        return config;
    }
}
