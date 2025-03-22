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
        public void SvAddCurrentUser(UserData data)
        {
            UserData d = new UserData(data.Id, data.Nickname, data.NicknameColor);
            AllUserData.Add(d);

            RpcClearUserDataList();

            for (int i = 0; i < AllUserData.Count; i++)
                RpcAddCurrentUser(AllUserData[i]);
        }

        [Server]
        public void SvRemoveCurrentUser(UserData data)
        {
            for (int i = 0; i < AllUserData.Count; i++)
            {
                if (AllUserData[i].Id == data.Id)
                {
                    AllUserData.RemoveAt(i);
                    break;
                }
            }

            RpcRemoveCurrentUser(data);
        }

        [ClientRpc]
        private void RpcClearUserDataList()
        {
            AllUserData.Clear();
        }

        [ClientRpc]
        private void RpcAddCurrentUser(UserData data)
        {
            UserData d = new UserData(data.Id, data.Nickname, data.NicknameColor);
            AllUserData.Add(d);

            UpdateUserList?.Invoke(AllUserData);
        }

        [ClientRpc]
        private void RpcRemoveCurrentUser(UserData data)
        {
            for (int i = 0; i < AllUserData.Count; i++)
            {
                if (AllUserData[i].Id == data.Id)
                {
                    AllUserData.RemoveAt(i);
                    break;
                }
            }

            UpdateUserList?.Invoke(AllUserData);
        }

        // Public Methods
        public User GetUserByNickname(UserData data)
        {
            if (AllUserData == null || AllUserData.Count == 0)
            {
                Debug.LogError("UserList is empty or not initialized.");
                return null;
            }

            foreach (var userData in AllUserData)
            {
                Debug.Log($"Checking user: {userData.Nickname} (ID: {userData.Id})");
                if (userData.Nickname.Equals(data.Nickname, StringComparison.OrdinalIgnoreCase))
                {
                    if (NetworkServer.spawned.TryGetValue((uint)userData.Id, out NetworkIdentity identity))
                    {
                        User user = identity.GetComponent<User>();
                        if (user != null)
                        {
                            Debug.Log($"User found: {user.Data.Nickname} (ID: {user.Data.Id})");
                            return user;
                        }
                        else
                            Debug.LogError($"User component not found for ID: {userData.Id}");
                    }
                    else
                        Debug.LogError($"NetworkIdentity not found for ID: {userData.Id}");
                }
            }

            Debug.LogError($"User with nickname {data.Nickname} not found.");
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
                        return user;
                }
            }
            return null;
        }
    }
}