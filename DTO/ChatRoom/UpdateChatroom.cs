namespace ChatAPI.DTO;

public record class UpdatePutChatroom
(
    int Id,
    string Roomname,
    int Creatbyuserid,
    bool IsDeleted
);

public record class UpdatePatchChatroom
(
    string Roomname
);