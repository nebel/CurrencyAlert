using System;
using System.Linq;
using CurrencyAlert.Controllers;
using CurrencyAlert.Models.Enums;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace CurrencyAlert.Models;

public unsafe class TrackedCurrency {
    private IDalamudTextureWrap? iconTexture;
    private uint? itemId;
    private int? maxCount;
    private string? label;

    public required CurrencyType Type { get; init; }

    [JsonIgnore] 
    public IDalamudTextureWrap? Icon => GetIcon();

    public uint ItemId {
        get => GetItemId();
        init => itemId = value;
    }

    public required int Threshold { get; set; }

    public bool Enabled { get; set; } = true;

    public bool ChatWarning { get; set; }
    
    public bool ShowInOverlay { get; set; }

    public bool Invert { get; set; } = false;
    
    [JsonIgnore] 
    public string Name => label ??= Service.DataManager.GetExcelSheet<Item>()!.GetRow(ItemId)?.Name ?? "Unable to read name";

    [JsonIgnore] 
    public bool CanRemove => Type is not (CurrencyType.LimitedTomestone or CurrencyType.NonLimitedTomestone);

    [JsonIgnore]
    public int CurrentCount { get; private set; }

    [JsonIgnore]
    public int MaxCount => maxCount ??= GetMaxCount();

    [JsonIgnore]
    public bool HasWarning { get; private set; }

    public void Refresh()
    {
        Service.Log.Debug($"  Refreshed {Name} ({ItemId})");
        CurrentCount = InventoryManager.Instance()->GetInventoryItemCount(ItemId, Type is CurrencyType.HighQualityItem, false, false);
        HasWarning = Invert ? CurrentCount < Threshold : CurrentCount > Threshold;

        if (ItemId is 20 or 21 or 22) {
            maxCount = GetMaxCountSeal();
        }
    }

    private uint GetItemId() {
        itemId ??= Type switch {
            CurrencyType.NonLimitedTomestone => Service.DataManager.GetExcelSheet<TomestonesItem>()!.First(item => item.Tomestones.Row is 2).Item.Row,
            CurrencyType.LimitedTomestone => Service.DataManager.GetExcelSheet<TomestonesItem>()!.First(item => item.Tomestones.Row is 3).Item.Row,
            _ => throw new Exception($"ItemId not initialized for type: {Type}")
        };

        return itemId.Value;
    }

    private IDalamudTextureWrap? GetIcon() {
        if (iconTexture is null && Service.DataManager.GetExcelSheet<Item>()!.GetRow(ItemId) is { Icon: var iconId }) {
            var iconFlags = Type switch {
                CurrencyType.HighQualityItem => ITextureProvider.IconFlags.HiRes | ITextureProvider.IconFlags.ItemHighQuality,
                _ => ITextureProvider.IconFlags.HiRes,
            };
            
            return iconTexture ??= Service.TextureProvider.GetIcon(iconId, iconFlags);
        }

        return iconTexture;
    }

    private int GetMaxCount()
    {
        return ItemId is 20 or 21 or 22 ? GetMaxCountSeal() : GetMaxCountDefault();
    }

    private int GetMaxCountDefault()
    {
        var item = Service.DataManager.GetExcelSheet<Item>()!.GetRow(ItemId);
        if (ItemId < 100 || item?.ItemUICategory.Row == 100) {
            return (int)(Service.DataManager.GetExcelSheet<Item>()!.GetRow(ItemId)?.StackSize ?? 0);
        }
        return 0;
    }

    private int GetMaxCountSeal()
    {
        try {
            var playerState = PlayerState.Instance();
            var rank = ItemId switch
            {
                20 => playerState->GCRankMaelstrom,
                21 => playerState->GCRankTwinAdders,
                22 => playerState->GCRankImmortalFlames,
                _ => throw new ArgumentOutOfRangeException()
            };
            return (int)CurrencyAlertSystem.MaxSealsByRank[rank];
        }
        catch (Exception e) {
            Service.Log.Warning(e, $"Failed to get GC Seal cap for itemId {ItemId}");
            return GetMaxCountDefault();
        }
    }
}