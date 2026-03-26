using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MintySpire2.util;

namespace MintySpire2.relicreminders;

/// <summary>
///     Credits to Book and erasels.
///     Adds relic reminder icons to cards in hand when threshold relics are primed.
///     Also makes affected cards glow gold.
/// </summary>
[HarmonyPatch]
public static class ThresholdRelicCardOverlay
{
    private const string IconContainerNodeName = "MintyThresholdRelicIcons";
    private static readonly WeakNodeRegistry<NCard> TrackedCards = new();

    private const string PenNibIconPath = "res://images/atlases/relic_atlas.sprites/pen_nib.tres";
    private const string PenNibOutlinePath = "res://images/atlases/relic_outline_atlas.sprites/pen_nib.tres";

    private const string TuningForkIconPath = "res://images/atlases/relic_atlas.sprites/tuning_fork.tres";
    private const string TuningForkOutlinePath = "res://images/atlases/relic_outline_atlas.sprites/tuning_fork.tres";

    private const string GalacticDustIconPath = "res://images/atlases/relic_atlas.sprites/galactic_dust.tres";
    private const string GalacticDustOutlinePath = "res://images/atlases/relic_outline_atlas.sprites/galactic_dust.tres";

    /// <summary>
    ///     UpdateVisuals is called after pile assignment is finalised, so pileType is reliable here.
    /// </summary>
    [HarmonyPatch(typeof(NCard), "UpdateVisuals")]
    [HarmonyPostfix]
    public static void UpdateVisuals_Postfix(NCard __instance, PileType pileType)
    {
        var me =  LocalContext.GetMe(RunManager.Instance?.State);
        if (me != null && me.Relics.Any(r => r is PenNib or TuningFork or GalacticDust))
        {
            TrackedCards.Register(__instance);
            RefreshCardOverlay(__instance, pileType == PileType.Hand);
        }
    }

    [HarmonyPatch]
    class CatchCardPlays
    {
        [HarmonyTargetMethods]
        static IEnumerable<MethodBase> TargetMethods() {
            yield return AccessTools.Method(typeof(PenNib), nameof(PenNib.AfterCardPlayed));
            yield return AccessTools.Method(typeof(TuningFork), nameof(TuningFork.AfterCardPlayed));
            yield return AccessTools.Method(typeof(GalacticDust), nameof(GalacticDust.AfterStarsSpent));
        }

        [HarmonyPostfix]
        static void CatchAfterCardPlayed() => RefreshTrackedCardOverlays();
    }

    [HarmonyPatch(typeof(CardModel), "ShouldGlowGoldInternal", MethodType.Getter)]
    [HarmonyPostfix]
    public static void ShouldGlowGoldInternal_Postfix(CardModel __instance, ref bool __result)
    {
        if (!__result)
            __result = GetActiveIconLayers(__instance).Count > 0;
    }


    private static void RefreshTrackedCardOverlays()
    {
        TrackedCards.ForEachLive(card => RefreshCardOverlay(card, IsInHand(card.Model)));
    }

    private static void RefreshCardOverlay(NCard card, bool isInHand)
    {
        var model = card.Model;
        if (model == null || !isInHand)
        {
            RemoveIconsIfExist(card);
            return;
        }

        var iconLayers = GetActiveIconLayers(model);
        if (iconLayers.Count == 0)
        {
            RemoveIconsIfExist(card);
            return;
        }

        AddIcons(card, iconLayers);
    }

    private static bool IsInHand(CardModel? card)
    {
        if (card == null)
            return false;

        var me = LocalContext.GetMe(RunManager.Instance?.State);
        if (me == null)
            return false;

        return PileType.Hand.GetPile(me).Cards.Contains(card);
    }

    private static List<IconLayerData> GetActiveIconLayers(CardModel card)
    {
        var iconLayers = new List<IconLayerData>(3);

        // TODO: Optimize this a bit 
        if (card.Type == CardType.Attack && GetRelic<PenNib>()?.Status == RelicStatus.Active)
            iconLayers.Add(new IconLayerData(PenNibIconPath, PenNibOutlinePath));

        if (card is { Type: CardType.Skill, GainsBlock: true } && GetRelic<TuningFork>()?.Status == RelicStatus.Active)
            iconLayers.Add(new IconLayerData(TuningForkIconPath, TuningForkOutlinePath));

        if (ShouldShowGalacticDust(card))
            iconLayers.Add(new IconLayerData(GalacticDustIconPath, GalacticDustOutlinePath));

        return iconLayers;
    }

    private static bool ShouldShowGalacticDust(CardModel card)
    {
        var galacticDust = GetRelic<GalacticDust>();
        if (galacticDust == null)
            return false;

        var threshold = galacticDust.DynamicVars.Stars.IntValue;
        if (threshold <= 0 || card.CurrentStarCost <= 0)
            return false;

        return (galacticDust.StarsSpent % threshold) + card.CurrentStarCost >= threshold;
    }

    private static void AddIcons(NCard card, IReadOnlyList<IconLayerData> iconLayers)
    {
        RemoveIconsIfExist(card);

        var container = new Control
        {
            Name = IconContainerNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorLeft = 1f,
            AnchorRight = 1f,
            AnchorTop = 0f,
            AnchorBottom = 0f,
        };

        for (var i = 0; i < iconLayers.Count; i++)
        {
            var iconContainer = MakeIconContainer(i);
            var iconLayer = iconLayers[i];

            // TODO: Optimize?
            var iconTexture = GD.Load<Texture2D>(iconLayer.IconPath);
            var outlineTexture = GD.Load<Texture2D>(iconLayer.OutlinePath);
            
            if (outlineTexture != null)
                iconContainer.AddChild(MakeLayer(outlineTexture, Colors.Black));

            iconContainer.AddChild(MakeLayer(iconTexture));
            container.AddChild(iconContainer);
        }

        if (container.GetChildCount() > 0)
            card.Body.AddChild(container);
        else
            container.QueueFree();
    }

    private static Control MakeIconContainer(int index)
    {
        const float horizontalSpacing = 32f;
        var horizontalOffset = index * horizontalSpacing;

        return new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorLeft = 1f,
            AnchorRight = 1f,
            AnchorTop = 0f,
            AnchorBottom = 0f,
            OffsetLeft = 112f - horizontalOffset,
            OffsetRight = 160f - horizontalOffset,
            OffsetTop = -218f,
            OffsetBottom = -170f,
        };
    }

    private static TextureRect MakeLayer(Texture2D texture, Color? modulate = null) => new()
    {
        Texture = texture,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        MouseFilter = Control.MouseFilterEnum.Ignore,
        AnchorRight = 1f,
        AnchorBottom = 1f,
        SelfModulate = modulate ?? Colors.White,
    };

    private static void RemoveIconsIfExist(NCard card) => card.Body?.GetNodeOrNull(IconContainerNodeName)?.Free();

    private static TRelic? GetRelic<TRelic>() where TRelic : RelicModel => LocalContext.GetMe(RunManager.Instance?.State)?.GetRelic<TRelic>();

    private readonly record struct IconLayerData(string IconPath, string OutlinePath);
}
