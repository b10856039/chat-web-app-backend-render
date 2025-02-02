using System;
using ChatAPI.DTO.Message;
using ChatAPI.Entities;

namespace ChatAPI.Mapping;

public static class MessageMapping
{
    public static MessageDTO ToMessageDTO(this Message message)
    {
        return new(
            message.Id,
            message.Content,
            message.SentAt,
            message.UserId,
            message.User!.Username,
            message.User.PhotoImg != null ? Convert.ToBase64String(message.User.PhotoImg) : "",
            message.ChatRoomId
        );
    }
}
