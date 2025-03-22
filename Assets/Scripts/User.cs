using UnityEngine;
using Mirror;
using UnityEngine.Events;
using System;

namespace NetworkChat
{
    public class User : NetworkBehaviour
    {
        public static User Local
        {
            get
            {
                var x = NetworkClient.localPlayer;

                if (x != null)
                    return x.GetComponent<User>();

                return null;
            }
        }

        private UserData userData;
        private UIUserInput m_InputField;

        public static event Action<string> OnUserReady;
        public static event UnityAction<UserData, string> RecieveMessageToChat;
        public static event UnityAction<NetworkConnection, UserData, string> RecievePrivateMessageToChat;

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

                if (!string.IsNullOrEmpty(message))
                    SendMessageToChat(message);
            }
        }

        // Handler

        public override void OnStopServer()
        {
            base.OnStopServer();

            UserList.Instance.SvRemoveCurrentUser(Data);
        }

        // Join
        public void JoinToChat(string nickname)
        {
            Data.Nickname = nickname;

            CmdAddUser(Data);
        }

        [Command]
        private void CmdAddUser(UserData data)
        {
            UserList.Instance.SvAddCurrentUser(data);
        }

        [Command]
        private void CmdRemoveUser(UserData data)
        {
            UserList.Instance.SvRemoveCurrentUser(data);
        }

        // Chat
        public void SendMessageToChat(string message)
        {
            if (!isLocalPlayer) return;
            if (string.IsNullOrEmpty(message)) return;

            UserData d = new UserData(userData.Id, userData.Nickname, userData.NicknameColor);

            CmdSendMessageToChat(d, message);
            m_InputField.ClearString();
        }

        [Command]
        private void CmdSendMessageToChat(UserData data, string message)
        {
            SvPostMessage(data, message);
        }

        [Server]
        private void SvPostMessage(UserData data, string message)
        {
            RpcRecieveMessage(data, message);
        }

        [ClientRpc]
        private void RpcRecieveMessage(UserData data, string message)
        {
            RecieveMessageToChat?.Invoke(data, message);
        }

        // Private Chat
        public void SendPrivateMessageToUserByNickname(UserData data, string message)
        {
            if (!isLocalPlayer) return;
            if (string.IsNullOrEmpty(message)) return;

            Debug.Log($"Поиск пользователя с ником: {data.Nickname}");

            User receiver = UserList.Instance.GetUserByNickname(data);
            if (receiver == null)
            {
                Debug.LogError($"Пользователь с ником {data} не найден.");
                return;
            }

            Debug.Log($"Пользователь найден: {receiver.Data.Nickname} (ID: {receiver.Data.Id})");

            if (receiver.Data == null)
            {
                Debug.LogError($"Данные пользователя с ником {data} не найдены.");
                return;
            }

            CmdSendPrivateMessageToUserByNickname(data, message);
        }

        [Command]
        private void CmdSendPrivateMessageToUserByNickname(UserData data, string message)
        {
            SvPostPrivateMessage(data, message);
        }

        [Server]
        private void SvPostPrivateMessage(UserData data, string message)
        {
            User receiver = UserList.Instance.GetUserById(data.Id);
            if (receiver != null)
                receiver.TargetReceivePrivateMessage(receiver.connectionToClient, data, message);
            else
                Debug.LogError($"User with ID {data} not found.");

           User sender = UserList.Instance.GetUserById(data.Id);
            if (sender != null)
                sender.TargetReceivePrivateMessage(sender.connectionToClient, data, message);
            else
                Debug.LogError($"User with ID {data} not found.");
        }

        [TargetRpc]
        private void TargetReceivePrivateMessage(NetworkConnection target, UserData data, string message)
        {
            RecievePrivateMessageToChat?.Invoke(target, data, message);
        }
    }
}