using System.Collections.Generic;
using Burdened.Config;
using ProtoBuf;

namespace Burdened.Network;

/// <summary>
/// Server -> client on join: the effective sanitized config, so the
/// client HUD/dialog/rendering mirror exactly what the server enforces.
/// </summary>
[ProtoContract]
public class ConfigSyncPacket
{
    [ProtoMember(1)]
    public int HotbarSlots;

    [ProtoMember(2)]
    public int BagSlots;

    [ProtoMember(3)]
    public bool ImmersiveCarryingMode;

    [ProtoMember(4)]
    public bool HideBagContentsInDialog;

    [ProtoMember(5)]
    public bool OffhandHoldsAnything;

    [ProtoMember(6)]
    public bool ImprovedBagInteractions;

    //F03 / D13
    [ProtoMember(7)]
    public Dictionary<string, string>? BagRoleOverrides;

    // F11 / D14
    [ProtoMember(8)]
    public float OffhandHungerPenalty;

    public static ConfigSyncPacket From(BurdenedConfig config)
    {
        return new ConfigSyncPacket
        {
            HotbarSlots = config.HotbarSlots,
            BagSlots = config.BagSlots,
            ImmersiveCarryingMode = config.ImmersiveCarryingMode,
            HideBagContentsInDialog = config.HideBagContentsInDialog,
            OffhandHoldsAnything = config.OffhandHoldsAnything,
            ImprovedBagInteractions = config.ImprovedBagInteractions,
            BagRoleOverrides = config.BagRoleOverrides,
            OffhandHungerPenalty = config.OffhandHungerPenalty,
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
            ImprovedBagInteractions = ImprovedBagInteractions,
            BagRoleOverrides = BagRoleOverrides,
            OffhandHungerPenalty = OffhandHungerPenalty,
        };
        config.Sanitize();
        return config;
    }
}
