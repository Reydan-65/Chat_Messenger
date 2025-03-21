using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using static NetworkChat.User;

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
        private void OnRecievePrivateMessageToChat(NetworkConnection target, ChatMessageData messageData)
        {
            AppendMessage(messageData,target);
        }

        private void OnRecieveMessageToChat(ChatMessageData messageData)
        {
            AppendMessage(messageData);
        }

        private void OnUpdateUserList(List<UserData> userList)
        {
            for (int i = 0; i < m_UserListPanel.childCount; i++)
                Destroy(m_UserListPanel.GetChild(i).gameObject);

            for (int i = 0; i < userList.Count; i++)
            {
                ChatMessageData userData = new ChatMessageData(
                    userList[i].Id,             // ID ������������
                    userList[i].Nickname,       // ��� ������������
                    0,                          // ID ���������� (�� ������������)
                    "",                         // ��� ���������� (�� ������������)
                    "",                         // ��������� (�� ������������)
                    userList[i].NicknameColor,  // ���� ���� ������������
                    Color.black                 // ���� ���� ���������� (�� ������������)
                );

                UIMessageBox userBox = Instantiate(m_UserBox.GetComponent<UIMessageBox>());

                userBox.SetText(userData, isPrivate: false, isSender: false);
                userBox.transform.SetParent(m_UserListPanel);
                userBox.transform.localScale = Vector3.one;

                if (userList[i].Id == User.Local.Data.Id) userBox.SetStyleBySelf(false);
                else userBox.SetStyleBySender(false);
            }
        }

        // Private Methods
        private void AppendMessage(ChatMessageData messageData, NetworkConnection targetConnection = null)
        {
            // ���������, �������� �� ��������� ���������
            bool isPrivate = messageData.Message.StartsWith("/w ");

            // ���� ��������� ���������
            if (isPrivate)
            {
                // ��������� ��������� �� �����: "/w", "�������", "���������"
                string[] parts = messageData.Message.Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    string receiverNickname = parts[1]; // ������� ����������

                    // ���������, �������� �� ������� ������������ �����������
                    bool isReceiver = User.Local.Data.Nickname.Equals(receiverNickname, StringComparison.OrdinalIgnoreCase);

                    // ���� ������� ������������ �� �������� ����������� � �� �������� ������������, �� ������ messageBox
                    if (!isReceiver && messageData.SenderId != User.Local.Data.Id)
                    {
                        return;
                    }
                }
                else
                {
                    Debug.LogError("�������� ������ ���������� ���������.");
                    return;
                }
            }

            // ���� targetConnection ������, ���������, ������ �� ������� ������ ���������� ���������
            if (targetConnection != null)
            {
                // ���� ������� ������ �� �������� ������� (target) � �� �������� ������������, �� ������ messageBox
                if (targetConnection != NetworkClient.connection && messageData.SenderId != User.Local.Data.Id)
                {
                    return;
                }
            }

            // ���������, ��� ������ UIMessageBox ����������
            if (m_MessageBox == null)
            {
                Debug.LogError("������ UIMessageBox �� ������!");
                return;
            }

            // ������� ��������� UIMessageBox
            UIMessageBox messageBox = Instantiate(m_MessageBox);

            if (messageBox == null)
            {
                Debug.LogWarning("��������� UIMessageBox �� ������ �� �������!");
                return;
            }

            // ����������� UIMessageBox
            messageBox.transform.localScale = Vector3.one;
            messageBox.transform.SetParent(m_MessagePanel);

            // ���������, �������� �� ������� ������������ ������������
            bool isSender = messageData.SenderId == User.Local.Data.Id;

            // ������������� ����� � ����� ���������
            messageBox.SetText(messageData, isPrivate, isSender);

            // ��������� ����� � ����������� �� ����, �������� �� ������������ ������������
            if (isSender)
                messageBox.SetStyleBySelf(true);
            else
                messageBox.SetStyleBySender(true);
        }
    }
}
