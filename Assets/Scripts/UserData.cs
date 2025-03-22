using UnityEngine;
using Mirror;

namespace NetworkChat
{
    public class UserData
    {
        public int Id;
        public string Nickname;
        public Color NicknameColor;

        public UserData(int id, string nickname, Color nicknameColor)
        {
            Id = id;
            Nickname = nickname;
            NicknameColor = nicknameColor;
        }
    }

    public static class UserDataWriteRead
    {
        public static void WriteUserData(this NetworkWriter writer, UserData data)
        {
            writer.WriteInt(data.Id);
            writer.WriteString(data.Nickname);
            writer.WriteColor(data.NicknameColor);
        }

        public static UserData ReadUserData(this NetworkReader reader)
        {
            return new UserData(reader.ReadInt(), reader.ReadString(), reader.ReadColor());
        }
    }

}
