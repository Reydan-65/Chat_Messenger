using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkChat
{
    public class UIViewer : MonoBehaviour
    {
        public static UIViewer Instance;

        [SerializeField] private UIMessageBox m_MessageBox;
        [SerializeField] private Transform m_MessagePanel;
        [SerializeField] private Transform m_UserListPanel;
        [SerializeField] private Transform m_UserBox;
        [SerializeField] private Transform m_MainPanel;
        [SerializeField] private Transform m_LoginPanel;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogError("Another instance of UserList already exists! Destroying this one.");
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            User.RecieveMessageToChat += OnRecieveMessageToChat;
            User.RecievePrivateMessageToChat += OnRecievePrivateMessageToChat;
            UserList.UpdateUserList += OnUpdateUserList;

            if (NetworkServer.active)
            {
                m_MainPanel.gameObject.SetActive(true);
                m_LoginPanel.gameObject.SetActive(false);
            }
            else
            {
                m_MainPanel.gameObject.SetActive(false);
                m_LoginPanel.gameObject.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            User.RecieveMessageToChat -= OnRecieveMessageToChat;
            User.RecievePrivateMessageToChat -= OnRecievePrivateMessageToChat;
            UserList.UpdateUserList -= OnUpdateUserList;
        }

        // Handlers
        private void OnRecievePrivateMessageToChat(NetworkConnection target, UserData data, string message)
        {
            AppendMessage(data, message, target);
        }

        private void OnRecieveMessageToChat(UserData data, string message)
        {
            AppendMessage(data, message);
        }

        private void OnUpdateUserList(List<UserData> userList)
        {
            foreach (Transform child in m_UserListPanel)
                Destroy(child.gameObject);

            foreach (var userData in userList)
            {
                UIMessageBox userBox = Instantiate(m_UserBox, m_UserListPanel).GetComponent<UIMessageBox>();
                userBox.SetText(userData, "", isPrivate: false, isSender: false);
                userBox.transform.localScale = Vector3.one;

                if (userData.Id == User.Local.Data.Id)
                    userBox.SetStyleBySelf(false);
                else
                    userBox.SetStyleBySender(false);
            }
        }

        // Private Methods
        private void AppendMessage(UserData data, string message, NetworkConnection targetConnection = null)
        {
            bool isPrivate = message.StartsWith("/w ");

            if (isPrivate)
            {
                string[] parts = message.Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    string receiverNickname = parts[1];

                    bool isReceiver = User.Local.Data.Nickname.Equals(receiverNickname, StringComparison.OrdinalIgnoreCase);

                    if (!isReceiver && data.Id != User.Local.Data.Id)
                        return;
                }
                else
                {
                    Debug.LogError("Invalid private message format.");
                    return;
                }
            }

            if (targetConnection != null)
            {
                if (targetConnection != NetworkClient.connection && data.Id != User.Local.Data.Id)
                    return;
            }

            if (m_MessageBox == null)
            {
                Debug.LogError("UIMessageBox prefab not found!");
                return;
            }

            UIMessageBox messageBox = Instantiate(m_MessageBox);

            if (messageBox == null)
            {
                Debug.LogWarning("UIMessageBox component not found on the prefab!");
                return;
            }

            messageBox.transform.localScale = Vector3.one;
            messageBox.transform.SetParent(m_MessagePanel);

            bool isSender = data.Id == User.Local.Data.Id;

            messageBox.SetText(data, message, isPrivate, isSender);

            if (isSender)
                messageBox.SetStyleBySelf(true);
            else
                messageBox.SetStyleBySender(true);
        }
    }
}
