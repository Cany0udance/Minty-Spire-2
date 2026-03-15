using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace MintySpire2;

/// <summary>
///     Adds a pen nib icon to attack cards in hand when Pen Nib is active.
///     Also makes those cards glow gold.
/// </summary>
[HarmonyPatch]
public static class PenNibCardOverlay
{
    private const string IconNodeName = "MintyPenNibIcon";

    /// <summary>
    ///     UpdateVisuals is called after pile assignment is finalised, so pileType is reliable here.
    /// </summary>
    [HarmonyPatch(typeof(NCard), "UpdateVisuals")]
    [HarmonyPostfix]
    public static void UpdateVisuals_Postfix(NCard __instance, PileType pileType)
    {
        if (pileType == PileType.Hand &&
            __instance.Model?.Type == CardType.Attack &&
            GetPenNib()?.Status == RelicStatus.Active)
            AddIconIfNeeded(__instance);
        else
            RemoveIconIfExists(__instance);
    }

    /// <summary>
    ///     Makes the attack cards glow gold when Pen Nib is active.
    /// </summary>
    [HarmonyPatch(typeof(CardModel), "ShouldGlowGoldInternal", MethodType.Getter)]
    [HarmonyPostfix]
    public static void ShouldGlowGoldInternal_Postfix(CardModel __instance, ref bool __result)
    {
        if (!__result && __instance.Type == CardType.Attack)
            __result = GetPenNib()?.Status == RelicStatus.Active;
    }

    private static void AddIconIfNeeded(NCard card)
    {
        if (card.Body == null || card.Body.HasNode(IconNodeName)) return;

        var iconTexture = GD.Load<Texture2D>("res://images/atlases/relic_atlas.sprites/pen_nib.tres");
        if (iconTexture == null) return;
        var outlineTexture = GD.Load<Texture2D>("res://images/atlases/relic_outline_atlas.sprites/pen_nib.tres");

        var container = new Control
        {
            Name = IconNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorLeft = 1f, AnchorRight = 1f,
            AnchorTop = 0f, AnchorBottom = 0f,
            OffsetLeft = 112f, OffsetRight = 160f,
            OffsetTop = -218f, OffsetBottom = -170f,
        };

        if (outlineTexture != null)
            container.AddChild(MakeLayer(outlineTexture, Colors.Black));
        container.AddChild(MakeLayer(iconTexture));
        card.Body.AddChild(container);
    }

    private static TextureRect MakeLayer(Texture2D texture, Color? modulate = null) => new()
    {
        Texture = texture,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        MouseFilter = Control.MouseFilterEnum.Ignore,
        AnchorRight = 1f, AnchorBottom = 1f,
        SelfModulate = modulate ?? Colors.White,
    };

    private static void RemoveIconIfExists(NCard card) =>
        card.Body?.GetNodeOrNull(IconNodeName)?.QueueFree();

    private static PenNib? GetPenNib() =>
        LocalContext.GetMe(RunManager.Instance?.State)?.Relics.OfType<PenNib>().FirstOrDefault();
}
