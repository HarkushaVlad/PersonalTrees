using StardewModdingAPI.Events;
using StardewValley;

namespace PersonalTrees.Messages;

public static class MessageManager
{
    private const string InfoMessageType = "infoMessage";

    public static void SendGlobalInfoMessage(string message)
    {
        Game1.chatBox.addInfoMessage(message);

        ModEntry.StaticHelper.Multiplayer.SendMessage(
            message: message,
            messageType: InfoMessageType,
            modIDs: new[] { ModEntry.StaticHelper.ModRegistry.ModID }
        );
    }

    public static void HandleMessage(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != ModEntry.StaticHelper.ModRegistry.ModID)
            return;

        switch (e.Type)
        {
            case InfoMessageType:
                var message = e.ReadAs<string>();
                Game1.chatBox.addInfoMessage(message);
                break;
        }
    }
}