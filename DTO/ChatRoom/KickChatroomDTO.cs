namespace ChatAPI.DTO;

public record class KickChatroomDTO
(
    int RequestUserId,
    int KickUserId
);