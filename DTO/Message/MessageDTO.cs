namespace ChatAPI.DTO.Message;

public record class MessageDTO
(
    int Id,
    string Content, 
    int SenderId, 
    string SenderName,
    string SenderPhotoImg,
    int ChatRoomId,
    DateTime SentAt, 
    DateTime UpdateAt,
    bool IsDeleted
);