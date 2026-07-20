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
    public bool PlaceableBags;
    public bool RememberDialogPlacement;
    public bool OpenBagsFromHotbar;

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
            PlaceableBags = config.PlaceableBags,
            RememberDialogPlacement = config.RememberDialogPlacement,
            OpenBagsFromHotbar = config.OpenBagsFromHotbar,
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
            PlaceableBags = PlaceableBags,
            RememberDialogPlacement = RememberDialogPlacement,
            OpenBagsFromHotbar = OpenBagsFromHotbar,
        };
        config.Sanitize();
        return config;
    }
}
