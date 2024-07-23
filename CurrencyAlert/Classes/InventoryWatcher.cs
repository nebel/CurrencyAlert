using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;

namespace CurrencyAlert.Classes;

public sealed class InventoryWatcher : IDisposable
{
    public ulong Version { get; private set; } = 1L;

    private readonly Hook<CurrencyManager.Delegates.SetItemData> setItemDataHook;

    public unsafe InventoryWatcher()
    {
        Service.GameInventory.InventoryChanged += OnInventoryChanged;
        setItemDataHook =
            Service.GameInterop.HookFromAddress<CurrencyManager.Delegates.SetItemData>(
                (nint)CurrencyManager.MemberFunctionPointers.SetItemData, OnSetItemData);
        setItemDataHook.Enable();
    }

    private unsafe void OnSetItemData(CurrencyManager* thisPtr, sbyte specialId, uint itemId, uint maxCount, uint count,
        bool isUnlimited)
    {
        setItemDataHook.Original.Invoke(thisPtr, specialId, itemId, maxCount, count, isUnlimited);
        // Service.Log.Debug($"Refreshing currencies (OnSetItemData) ::: {specialId}, {itemId}, {maxCount}, {count}, {isUnlimited}");
        Invalidate();
    }

    private void OnInventoryChanged(IReadOnlyCollection<InventoryEventArgs> events)
    {
        // Service.Log.Debug("Refreshing currencies (OnInventoryChanged)");
        Invalidate();
    }

    public void Invalidate()
    {
        Version += 1;
    }

    public void Dispose()
    {
        Service.GameInventory.InventoryChanged -= OnInventoryChanged;
        setItemDataHook.Dispose();
    }
}