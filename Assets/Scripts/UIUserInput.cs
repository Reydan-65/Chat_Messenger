using TMPro;
using UnityEngine;

namespace NetworkChat
{
    public class UIUserInput : MonoBehaviour
    {
        public static UIUserInput Instance;

        [SerializeField] private TMP_InputField m_MessageInputField;
        [SerializeField] private TMP_InputField m_LoginInputField;

        public bool IsEmpty => m_MessageInputField.text == "";

        private void Awake()
        {
            Instance = this;

            User.OnUserReady += OnUserReady;
        }

        private void OnDestroy()
        {
            User.OnUserReady -= OnUserReady;
        }

        // Handlers
        private void OnUserReady(string nickname)
        {
            JoinToChat(nickname);
        }

        // Public Methods
        public string GetString()
        {
            return m_MessageInputField.text;
        }

        public void ClearString()
        {
            m_MessageInputField.text = "";
        }

        public void SendMessageToChat()
        {
            string inputText = m_MessageInputField.text;

            if (string.IsNullOrEmpty(inputText)) return;

            User.Local.SendMessageToChat(inputText);

            ClearString();
        }

        public void JoinToChat(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                Debug.LogError("Nickname cannot be empty");
                return;
            }

            nickname = GetNickname();
            User.Local.JoinToChat(nickname);
        }

        public string GetNickname()
        {
            return m_LoginInputField.text;
        }
    }
}
