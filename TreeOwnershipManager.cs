using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using PersonalTrees.Constants;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace PersonalTrees;

public static class TreeOwnershipManager
{
    private const string RequestType = "treeOwnershipToggleRequest";
    private const string ResponseType = "treeOwnershipToggleResponse";
    private static readonly Dictionary<string, PendingRequest> PendingRequests = new();
    private static readonly TimeSpan PendingTimeout = TimeSpan.FromSeconds(15);

    public static bool TryToggleOwnership(GameLocation location, Vector2 tile, Farmer player)
    {
        PrunePendingRequests();
        if (!Context.IsWorldReady || !TryGetTree(location, tile, out var tree))
            return false;

        if (!IsValidCallerTarget(location, tile, player, out var x, out var y))
        {
            ShowMessage(ModConstants.TranslationKeys.ReleaseUnavailable);
            return true;
        }

        var expectedOwner = tree.modData.TryGetValue(ModConstants.TreeOwnerKey, out var ownerId)
            ? ownerId
            : null;
        if (expectedOwner != null && !string.Equals(expectedOwner, player.UniqueMultiplayerID.ToString(), StringComparison.Ordinal))
        {
            ShowMessage(ModConstants.TranslationKeys.NotYourTree);
            return true;
        }

        var targetKey = MakeTargetKey(location.NameOrUniqueName, x, y);
        if (PendingRequests.Values.Any(pending => pending.TargetKey == targetKey))
            return true;

        if (Context.IsMainPlayer || Game1.IsMasterGame)
        {
            ApplyToggle(tree, player.UniqueMultiplayerID, expectedOwner);
            ShowMessage(expectedOwner == null
                ? ModConstants.TranslationKeys.TreeClaimed
                : ModConstants.TranslationKeys.TreeReleased);
            return true;
        }

        if (Game1.MasterPlayer == null || !IsHostModAvailable(Game1.MasterPlayer.UniqueMultiplayerID))
        {
            ShowMessage(ModConstants.TranslationKeys.ReleaseUnavailable);
            return true;
        }

        var requestId = Guid.NewGuid().ToString("N");
        PendingRequests[requestId] = new PendingRequest(targetKey, DateTime.UtcNow);
        ModEntry.StaticHelper.Multiplayer.SendMessage(
            message: new OwnershipToggleRequest
            {
                RequestId = requestId,
                LocationName = location.NameOrUniqueName,
                TileX = x,
                TileY = y,
                ExpectedOwnerId = expectedOwner
            },
            messageType: RequestType,
            modIDs: new[] { ModEntry.StaticHelper.ModRegistry.ModID },
            playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID }
        );
        return true;
    }

    internal static void HandleMessage(ModMessageReceivedEventArgs e)
    {
        if (!Context.IsWorldReady || e.FromModID != ModEntry.StaticHelper.ModRegistry.ModID)
            return;

        if (e.Type == RequestType)
            HandleRequest(e);
        else if (e.Type == ResponseType)
            HandleResponse(e);
    }

    private static void HandleRequest(ModMessageReceivedEventArgs e)
    {
        if (!Context.IsMainPlayer && !Game1.IsMasterGame)
            return;

        OwnershipToggleRequest request;
        try
        {
            request = e.ReadAs<OwnershipToggleRequest>();
        }
        catch
        {
            return;
        }

        if (request == null || string.IsNullOrWhiteSpace(request.RequestId) ||
            !TryGetIntegerTile(new Vector2(request.TileX, request.TileY), out var x, out var y))
            return;

        var requestingFarmer = Game1.getOnlineFarmers()
            .FirstOrDefault(farmer => farmer.UniqueMultiplayerID == e.FromPlayerID);
        var status = OwnershipStatus.Denied;

        if (requestingFarmer != null &&
            requestingFarmer.currentLocation != null &&
            string.Equals(requestingFarmer.currentLocation.NameOrUniqueName, request.LocationName, StringComparison.Ordinal) &&
            IsWithinOneTile(requestingFarmer.Tile, x, y) &&
            TryGetTree(requestingFarmer.currentLocation, new Vector2(x, y), out var tree))
        {
            var currentOwner = tree.modData.TryGetValue(ModConstants.TreeOwnerKey, out var ownerId) ? ownerId : null;
            if (string.Equals(currentOwner, request.ExpectedOwnerId, StringComparison.Ordinal) &&
                (currentOwner == null || currentOwner == requestingFarmer.UniqueMultiplayerID.ToString()))
            {
                ApplyToggle(tree, requestingFarmer.UniqueMultiplayerID, currentOwner);
                status = currentOwner == null ? OwnershipStatus.Claimed : OwnershipStatus.Released;
            }
        }

        ModEntry.StaticHelper.Multiplayer.SendMessage(
            message: new OwnershipToggleResponse { RequestId = request.RequestId, Status = status },
            messageType: ResponseType,
            modIDs: new[] { ModEntry.StaticHelper.ModRegistry.ModID },
            playerIDs: new[] { e.FromPlayerID }
        );
    }

    private static void HandleResponse(ModMessageReceivedEventArgs e)
    {
        if (Game1.MasterPlayer == null || e.FromPlayerID != Game1.MasterPlayer.UniqueMultiplayerID)
            return;

        OwnershipToggleResponse response;
        try
        {
            response = e.ReadAs<OwnershipToggleResponse>();
        }
        catch
        {
            return;
        }

        if (response == null || string.IsNullOrWhiteSpace(response.RequestId) ||
            !PendingRequests.Remove(response.RequestId))
            return;

        var messageKey = response.Status switch
        {
            OwnershipStatus.Claimed => ModConstants.TranslationKeys.TreeClaimed,
            OwnershipStatus.Released => ModConstants.TranslationKeys.TreeReleased,
            _ => ModConstants.TranslationKeys.ReleaseUnavailable
        };
        ShowMessage(messageKey);
    }

    private static bool IsHostModAvailable(long hostId)
    {
        var peer = ModEntry.StaticHelper.Multiplayer.GetConnectedPlayer(hostId);
        return peer != null && peer.HasSmapi &&
               peer.Mods.Any(mod => mod.ID == ModEntry.StaticHelper.ModRegistry.ModID);
    }

    private static bool IsValidCallerTarget(GameLocation location, Vector2 tile, Farmer player, out int x, out int y)
    {
        return TryGetIntegerTile(tile, out x, out y) &&
               player != null &&
               player.currentLocation == location &&
               IsWithinOneTile(player.Tile, x, y);
    }

    private static void ApplyToggle(Tree tree, long playerId, string? expectedOwner)
    {
        if (expectedOwner == null)
            tree.modData[ModConstants.TreeOwnerKey] = playerId.ToString();
        else
            tree.modData.Remove(ModConstants.TreeOwnerKey);
    }

    private static bool TryGetTree(GameLocation location, Vector2 tile, out Tree tree)
    {
        tree = null!;
        if (location != null && location.terrainFeatures.TryGetValue(tile, out var feature) && feature is Tree found)
        {
            tree = found;
            return true;
        }

        return false;
    }

    private static bool TryGetIntegerTile(Vector2 tile, out int x, out int y)
    {
        x = y = 0;
        if (float.IsNaN(tile.X) || float.IsInfinity(tile.X) || float.IsNaN(tile.Y) || float.IsInfinity(tile.Y) ||
            tile.X != Math.Truncate(tile.X) || tile.Y != Math.Truncate(tile.Y) ||
            tile.X < int.MinValue || tile.X > int.MaxValue || tile.Y < int.MinValue || tile.Y > int.MaxValue)
            return false;

        x = (int)tile.X;
        y = (int)tile.Y;
        return true;
    }

    private static bool IsWithinOneTile(Vector2 playerTile, int x, int y) =>
        Math.Abs((double)playerTile.X - x) <= 1 && Math.Abs((double)playerTile.Y - y) <= 1;

    private static string MakeTargetKey(string location, int x, int y) => $"{location}|{x}|{y}";

    private static void PrunePendingRequests()
    {
        var cutoff = DateTime.UtcNow - PendingTimeout;
        foreach (var requestId in PendingRequests.Where(pair => pair.Value.CreatedAt < cutoff).Select(pair => pair.Key).ToArray())
            PendingRequests.Remove(requestId);
    }

    private static void ShowMessage(string key) =>
        Game1.addHUDMessage(new HUDMessage(ModEntry.StaticHelper.Translation.Get(key)));

    public enum OwnershipStatus
    {
        Denied,
        Claimed,
        Released
    }

    private sealed class PendingRequest
    {
        public PendingRequest(string targetKey, DateTime createdAt) { TargetKey = targetKey; CreatedAt = createdAt; }
        public string TargetKey { get; }
        public DateTime CreatedAt { get; }
    }

    public sealed class OwnershipToggleRequest
    {
        public string RequestId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int TileX { get; set; }
        public int TileY { get; set; }
        public string? ExpectedOwnerId { get; set; }
    }

    public sealed class OwnershipToggleResponse
    {
        public string RequestId { get; set; } = string.Empty;
        public OwnershipStatus Status { get; set; }
    }
}
