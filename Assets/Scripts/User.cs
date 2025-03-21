using UnityEngine;
using Mirror;
using UnityEngine.Events;
using System;

namespace NetworkChat
{
    [System.Serializable]
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

    public class User : NetworkBehaviour
    {
        public static User Local
        {
            get
            {
                var x = NetworkClient.localPlayer;

                if (x != null)
                {
                    return x.GetComponent<User>();
                }
                return null;
            }
        }

        private UserData userData;
        private UIUserInput m_InputField;

        public static event Action<string> OnUserReady;
        [System.Serializable]
        public class ChatMessageData
        {
            public int SenderId;
            public string SenderNickname;
            public int ReceiverId;
            public string ReceiverNickname;
            public string Message;
            public Color SenderColor;
            public Color ReceiverColor;

            // Конструктор по умолчанию (обязателен для Mirror)
            public ChatMessageData()
            {
                SenderId = 0;
                SenderNickname = string.Empty;
                ReceiverId = 0;
                ReceiverNickname = string.Empty;
                Message = string.Empty;
                SenderColor = Color.white;
                ReceiverColor = Color.white;
            }

            // Параметризованный конструктор для удобства
            public ChatMessageData(int senderId, string senderNickname,
                                   int receiverId, string receiverNickname,
                                   string message, Color senderColor, Color receiverColor)
            {
                SenderId = senderId;
                SenderNickname = senderNickname;
                ReceiverId = receiverId;
                ReceiverNickname = receiverNickname;
                Message = message;
                SenderColor = senderColor;
                ReceiverColor = receiverColor;
            }
        }

        // Используем UnityAction с одним параметром
        public static event UnityAction<ChatMessageData> RecieveMessageToChat;
        public static event UnityAction<NetworkConnection, ChatMessageData> RecievePrivateMessageToChat;

        public UserData Data => userData;

        private void Start()
        {
            m_InputField = UIUserInput.Instance;

            Color randomColor = new Color(
            UnityEngine.Random.Range(0.5f, 1f),
            UnityEngine.Random.Range(0.5f, 1f),
            UnityEngine.Random.Range(0.5f, 1f)
        );

            userData = new UserData((int)netId, "Nickname", randomColor);

            if (m_InputField != null && isLocalPlayer)
            {
                OnUserReady?.Invoke(userData.Nickname);
            }
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            if (Input.GetKeyDown(KeyCode.Return))
            {
                string message = UIUserInput.Instance.GetString();

                // Проверяем, не пустое ли сообщение
                if (!string.IsNullOrEmpty(message))
                {
                    SendMessageToChat(message);
                }
            }
        }

        // Handler

        public override void OnStopServer()
        {
            base.OnStopServer();

            UserList.Instance.SvRemoveCurrentUser(Data.Id);
        }

        // Join
        public void JoinToChat(string nickname)
        {
            Data.Nickname = nickname;

            CmdAddUser(Data.Id, Data.Nickname, Data.NicknameColor);
        }

        [Command]
        private void CmdAddUser(int id, string nickname, Color nicknameColor)
        {
            UserList.Instance.SvAddCurrentUser(id, nickname, nicknameColor);
        }

        [Command]
        private void CmdRemoveUser(int id)
        {
            UserList.Instance.SvRemoveCurrentUser(id);
        }

        // Chat
        public void SendMessageToChat(string message)
        {
            if (!isLocalPlayer) return;
            if (string.IsNullOrEmpty(message)) return;

            // Создаем объект ChatMessageData для общего сообщения
            ChatMessageData messageData = new ChatMessageData(
                userData.Id, // ID отправителя
                userData.Nickname, // Ник отправителя
                0, // ID получателя (0 для общего чата)
                "", // Ник получателя (пусто для общего чата)
                message, // Сообщение
                userData.NicknameColor, // Цвет ника отправителя
                Color.white // Цвет ника получателя (не используется для общего чата)
            );

            // Отправляем сообщение
            CmdSendMessageToChat(messageData);
            m_InputField.ClearString();
        }

        [Command]
        private void CmdSendMessageToChat(ChatMessageData messageData)
        {
            SvPostMessage(messageData);
        }

        [Server]
        private void SvPostMessage(ChatMessageData messageData)
        {
            RpcRecieveMessage(messageData);
        }

        [ClientRpc]
        private void RpcRecieveMessage(ChatMessageData messageData)
        {
            RecieveMessageToChat?.Invoke(messageData);
        }

        // Private Chat
        public void SendPrivateMessageToUserByNickname(ChatMessageData messageData)
        {
            if (!isLocalPlayer) return;
            if (string.IsNullOrEmpty(messageData.Message)) return;

            Debug.Log($"Поиск пользователя с ником: {messageData.ReceiverNickname}");

            // Ищем получателя по никнейму
            User receiver = UserList.Instance.GetUserByNickname(messageData.ReceiverNickname);
            if (receiver == null)
            {
                Debug.LogError($"Пользователь с ником {messageData.ReceiverNickname} не найден.");
                return;
            }

            Debug.Log($"Пользователь найден: {receiver.Data.Nickname} (ID: {receiver.Data.Id})");

            // Убедимся, что получатель существует
            if (receiver.Data == null)
            {
                Debug.LogError($"Данные пользователя с ником {messageData.ReceiverNickname} не найдены.");
                return;
            }

            messageData.ReceiverColor = receiver.Data.NicknameColor;

            // Отправляем сообщение только отправителю и получателю
            CmdSendPrivateMessageToUserByNickname(messageData, receiver.netId);
        }

        [Command]
        private void CmdSendPrivateMessageToUserByNickname(ChatMessageData messageData, uint receiverNetId)
        {
            SvPostPrivateMessage(messageData);
        }

        [Server]
        private void SvPostPrivateMessage(ChatMessageData messageData)
        {
            // Ищем получателя по ID
            User receiver = UserList.Instance.GetUserById(messageData.ReceiverId);
            if (receiver != null)
            {
                // Отправляем сообщение только получателю
                receiver.TargetReceivePrivateMessage(receiver.connectionToClient, messageData);
            }
            else
            {
                Debug.LogError($"User with ID {messageData.ReceiverId} not found.");
            }

           // Отправляем сообщение отправителю
           User sender = UserList.Instance.GetUserById(messageData.SenderId);
            if (sender != null)
            {
                sender.TargetReceivePrivateMessage(sender.connectionToClient, messageData);
            }
            else
            {
                Debug.LogError($"User with ID {messageData.SenderId} not found.");
            }
        }

        [TargetRpc]
        private void TargetReceivePrivateMessage(NetworkConnection target, ChatMessageData messageData)
        {
            RecievePrivateMessageToChat?.Invoke(target, messageData);
        }
    }
}