using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;

namespace CurrencyAlert.Classes;

public sealed class InventoryWatcher : IDisposable
{
    public ulong Version = 1L;
    private readonly Hook<SetItemDataDelegate> setItemDataHook;

    private delegate void SetItemDataDelegate(IntPtr manager, sbyte specialId, uint itemId, uint maxCount, uint count, bool isUnlimited);

    public unsafe InventoryWatcher()
    {
        Service.GameInventory.InventoryChanged += OnInventoryChanged;
        setItemDataHook = Service.GameInterop.HookFromAddress<SetItemDataDelegate>((nint)CurrencyManager.MemberFunctionPointers.SetItemData, OnSetItemData);
        setItemDataHook.Enable();
    }

    private void OnSetItemData(IntPtr manager, sbyte specialId, uint itemId, uint maxCount, uint count, bool isUnlimited)
    {
        setItemDataHook.Original.Invoke(manager, specialId, itemId, maxCount, count, isUnlimited);
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