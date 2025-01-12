using OwlLogging;
using Server;

public class ChatModule
{
    public class ChatMessageRequestData
    {
        public int SenderId;
        public GridEntity Sender;
        public string Message;
        public string TargetName;
    }

    private ServerMapModule _mapModule;
    private AServer _server;

    public int Initialize(ServerMapModule mapModule, AServer server)
    {
        // TODO: Validate
        _mapModule = mapModule;
        _server = server;
        return 0;
    }

    public int HandleChatMessage(ChatMessageRequestData chatMessage)
    {
        // Only logged in players are allowed to send a chat-message to the server
        // TODO: This means NPCs can't send messages right now. Also, Announcements may be a problem?
        // Need an alternative flow for those, or adjust this restriction
        if (!_server.TryGetLoggedInCharacter(chatMessage.SenderId, out CharacterRuntimeData charData))
        {
            OwlLogger.LogError($"Cannot send chat message for sender id {chatMessage.SenderId} - not a logged in character!", GameComponent.Chat);
            return -1;
        }

        chatMessage.Sender = charData;

        if(!CanChat(chatMessage))
        {
            OwlLogger.Log($"Chat Message request denied: Character id {chatMessage.SenderId} can't chat.", GameComponent.Chat, LogSeverity.Verbose);
            return -2;
        }

        chatMessage.Message = TrimNetworkPadding(chatMessage.Message);
        chatMessage.TargetName = TrimNetworkPadding(chatMessage.TargetName);

        if (chatMessage.TargetName == ChatMessageRequestPacket.TARGET_GLOBAL)
        {
            return HandleGlobalMessage(chatMessage) * 10;
        }

        if(chatMessage.TargetName == ChatMessageRequestPacket.TARGET_PROX)
        {
            return HandleProximityMessage(chatMessage) * 10;
        }
        
        return HandleWhisperMessage(chatMessage) * 10;
    }

    public bool CanChat(ChatMessageRequestData message)
    {
        if (message.Sender == null)
            return false;
        // TODO: Check mute, chat cooldown, etc stuff
        return true;
    }

    private string TrimNetworkPadding(string rawMessage)
    {
        return rawMessage.Trim('.');
    }

    private int HandleGlobalMessage(ChatMessageRequestData chatMessage)
    {
        ChatMessagePacket packet = new()
        {
            SenderId = chatMessage.SenderId,
            Message = chatMessage.Message,
            SenderName = chatMessage.Sender.Name,
            MessageScope = ChatMessagePacket.Scope.Global
        };

        foreach(CharacterRuntimeData charData in _server.LoggedInCharacters)
        {
            charData.Connection.Send(packet);
        }
        
        return 0;
    }

    private int HandleProximityMessage(ChatMessageRequestData chatMessage)
    {
        const int PROXIMITY_CHAT_RANGE = 20;
        ServerMapInstance map = _mapModule.GetMapInstance(chatMessage.Sender.MapId);
        if(map == null)
        {
            OwlLogger.LogError($"Map {chatMessage.Sender.MapId} not available for Proximity chat!", GameComponent.Chat);
            return -1;
        }

        ChatMessagePacket packet = new()
        {
            SenderId = chatMessage.SenderId,
            Message = chatMessage.Message,
            SenderName = chatMessage.Sender.Name,
            MessageScope = ChatMessagePacket.Scope.Proximity
        };

        var charlist = map.Grid.GetOccupantsInRangeSquare<CharacterRuntimeData>(chatMessage.Sender.Coordinates, PROXIMITY_CHAT_RANGE);
        foreach (CharacterRuntimeData charData in charlist)
        {
            charData.Connection.Send(packet);
        }

        return 0;
    }

    private int HandleWhisperMessage(ChatMessageRequestData chatMessage)
    {
        // TODO: Allow whispers only to players for now. if we ever have whispers to NPCs, an additional system is needed here - we can't iterate over all Entities on all maps
        ChatMessagePacket packet = new()
        {
            SenderId = chatMessage.SenderId,
            Message = chatMessage.Message,
            SenderName = chatMessage.Sender.Name,
            MessageScope = ChatMessagePacket.Scope.Whisper
        };

        foreach (CharacterRuntimeData charData in _server.LoggedInCharacters)
        {
            if(charData.Name == chatMessage.TargetName)
            {
                charData.Connection.Send(packet);
                return SendSenderFeedback(chatMessage);
            }
        }

        OwlLogger.Log($"Tried to send Whisper to character {chatMessage.TargetName} that wasn't found!", GameComponent.Chat, LogSeverity.Verbose);
        return -1;
    }

    private int SendSenderFeedback(ChatMessageRequestData chatMessage)
    {
        if (chatMessage.Sender is not CharacterRuntimeData charSender)
            return 0;

        ChatMessagePacket feedbackMessagePacket = new()
        {
            SenderId = chatMessage.SenderId,
            Message = chatMessage.Message,
            SenderName = $"To {chatMessage.TargetName}",
            MessageScope = ChatMessagePacket.Scope.Whisper
        };

        charSender.Connection.Send(feedbackMessagePacket);
        return 0;
    }
}
