using System;
using System.Runtime.CompilerServices;
using ChatAPI.DTO;
using ChatAPI.Entities;

namespace ChatAPI.Mapping;

public static class UserMapping
{
    public static UserDTO ToUserDTO(this User user)
    {
        return new(
            user.Id,
            user.Username,
            user.Email,
            user.Phone,
            user.State,
            user.PhotoImg != null ? Convert.ToBase64String(user.PhotoImg) : "",
            user.Role,
            user.CreateAt,
            user.UpdateAt,
            user.IsDeleted
        );
    }

    public static User ToEntity(this CreateUserDTO user)
    {
        return new User(){
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            Password = user.Password,
            UserChatRooms = [] 
        };
    }

    public static User ToEntity(this UpdatePutUserDTO user, int id)
    {
        return new User(){
            Id = id,
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            Password = user.Password,
            State = user.State,
            PhotoImg = user.PhotoImg != null ? Convert.FromBase64String(user.PhotoImg) : null ,
            Role = user.Role,
            UserChatRooms = new List<UserChatRoom>(),
            UpdateAt = ConvertToTaipeiTime(DateTime.UtcNow),
        };
    }

    private static DateTime ConvertToTaipeiTime(DateTime utcDateTime)
    {
        // 台北時區
        var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        // 轉換為台北時間
        var taipeiTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, taipeiTimeZone);
        // 去掉毫秒部分
        return taipeiTime.AddTicks(-(taipeiTime.Ticks % TimeSpan.TicksPerSecond));
    }

}
