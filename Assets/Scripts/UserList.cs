using Mirror;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using System;

namespace NetworkChat
{
    public class UserList : NetworkBehaviour
    {
        public static UserList Instance;

        private void Awake()
        {
            Instance = this;
        }

        public List<UserData> AllUserData = new List<UserData>();
        public static UnityAction<List<UserData>> UpdateUserList;

        public override void OnStartClient()
        {
            base.OnStartClient();

            AllUserData.Clear();
        }

        [Server]
        public void SvAddCurrentUser(int userId, string userNickname, Color nicknameColor)
        {
            UserData data = new UserData(userId, userNickname, nicknameColor);
            AllUserData.Add(data);

            RpcClearUserDataList();
            for (int i = 0; i < AllUserData.Count; i++)
            {
                RpcAddCurrentUser(AllUserData[i].Id, AllUserData[i].Nickname, AllUserData[i].NicknameColor);
            }
        }

        [Server]
        public void SvRemoveCurrentUser(int userId)
        {
            for (int i = 0; i < AllUserData.Count; i++)
            {
                if (AllUserData[i].Id == userId)
                {
                    AllUserData.RemoveAt(i);
                    break;
                }
            }

            RpcRemoveCurrentUser(userId);
        }

        [ClientRpc]
        private void RpcClearUserDataList()
        {
            AllUserData.Clear();
        }

        [ClientRpc]
        private void RpcAddCurrentUser(int userId, string userNickname, Color nicknameColor)
        {
            UserData data = new UserData(userId, userNickname, nicknameColor);
            AllUserData.Add(data);

            UpdateUserList?.Invoke(AllUserData);
        }

        [ClientRpc]
        private void RpcRemoveCurrentUser(int userId)
        {
            for (int i = 0; i < AllUserData.Count; i++)
            {
                if (AllUserData[i].Id == userId)
                {
                    AllUserData.RemoveAt(i);
                    break;
                }
            }

            UpdateUserList?.Invoke(AllUserData);
        }

        // Public Methods
        public User GetUserByNickname(string nickname)
        {
            // Убедимся, что список пользователей не пуст
            if (AllUserData == null || AllUserData.Count == 0)
            {
                Debug.LogError("UserList is empty or not initialized.");
                return null;
            }

            // Ищем пользователя по нику (без учета регистра)
            foreach (var userData in AllUserData)
            {
                Debug.Log($"Checking user: {userData.Nickname} (ID: {userData.Id})");
                if (userData.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase))
                {
                    // Проверяем, существует ли объект пользователя в NetworkServer.spawned
                    if (NetworkServer.spawned.TryGetValue((uint)userData.Id, out NetworkIdentity identity))
                    {
                        User user = identity.GetComponent<User>();
                        if (user != null)
                        {
                            Debug.Log($"User found: {user.Data.Nickname} (ID: {user.Data.Id})");
                            return user;
                        }
                        else
                        {
                            Debug.LogError($"User component not found for ID: {userData.Id}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"NetworkIdentity not found for ID: {userData.Id}");
                    }
                }
            }

            Debug.LogError($"User with nickname {nickname} not found.");
            return null;
        }

        public User GetUserById(int userId)
        {
            foreach (var userData in AllUserData)
            {
                if (NetworkServer.spawned.TryGetValue((uint)userData.Id, out NetworkIdentity identity))
                {
                    User user = NetworkServer.spawned[(uint)userData.Id].GetComponent<User>();

                    if (user != null && user.Data.Id == userId)
                    {
                        return user;
                    }
                }
            }
            return null;
        }
    }
}