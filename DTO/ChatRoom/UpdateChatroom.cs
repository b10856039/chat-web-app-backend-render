namespace ChatAPI.DTO;

public record class UpdatePutChatroom
(
    int Id,
    string Roomname,
    string PhotoImg,
    int Creatbyuserid,
    bool IsDeleted
);

public record class UpdatePatchChatroom
(
    int UserId,
    string Roomname,
    string? PhotoImg
);