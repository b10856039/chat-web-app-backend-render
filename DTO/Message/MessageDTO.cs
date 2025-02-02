namespace ChatAPI.DTO.Message;

public record class MessageDTO
(
    int Id,
    string Content, 
    DateTime SentAt, 
    int SenderId, 
    string SenderName,
    string SenderPhotoImg,
    int ChatRoomId 
);