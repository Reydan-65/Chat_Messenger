using System.Linq;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static NetworkChat.User;

namespace NetworkChat
{
    public class UIMessageBox : MonoBehaviour
    {
        [SerializeField] private RectTransform m_BgImageTransform;
        [SerializeField] private Image m_BgImage;
        [SerializeField] private Color m_BgColorSelf;
        [SerializeField] private Color m_BgColorSender;

        [SerializeField] private TextMeshProUGUI m_Text;

        private RectTransform m_RectTransform;
        private RectTransform m_TextRectTransform;

        private void Awake()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_TextRectTransform = GetComponent<RectTransform>();
        }

        public void SetText(ChatMessageData messageData, bool isPrivate = false, bool isSender = false)
        {
            string hexSenderColor = ColorUtility.ToHtmlStringRGB(messageData.SenderColor);
            string hexReceiverColor = ColorUtility.ToHtmlStringRGB(messageData.ReceiverColor);
            string privateMessageColor = ColorUtility.ToHtmlStringRGB(Color.yellow);
            string privateNicknameColor = ColorUtility.ToHtmlStringRGB(Color.magenta);

            string formattedMessage;

            if (isPrivate)
            {
                string[] parts = messageData.Message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 3 && parts[0] == "/w")
                {
                    string receiverNickname = parts[1];
                    string message = string.Join(" ", parts.Skip(2));

                    if (isSender)
                    {
                        formattedMessage = $"<color=#{hexSenderColor}>{messageData.SenderNickname}</color>\n<color=#{privateNicknameColor}>[to <color=#{hexReceiverColor}>{receiverNickname}</color>]</color>: <color=#{privateMessageColor}>{message}</color>";
                    }
                    else
                    {
                        formattedMessage = $"<color=#{privateNicknameColor}>[from <color=#{hexSenderColor}>{messageData.SenderNickname}</color>]</color>\n<color=#{privateMessageColor}>{message}</color>";
                    }
                }
                else
                {
                    Debug.LogError("Ќеверный формат приватного сообщени€.");
                    formattedMessage = messageData.Message;
                }
            }
            else
            {
                formattedMessage = $"<color=#{hexSenderColor}>{messageData.SenderNickname}</color>\n{messageData.Message}";
            }

            m_Text.text = formattedMessage;

            UpdateRectTransformSize();
        }

        public void SetStyleBySelf(bool isRectStyle)
        {
            if (isRectStyle)
            {
                float textWidth = m_Text.preferredWidth;

                m_BgImageTransform.anchorMin = new Vector2(0.01f, 0);
                m_BgImageTransform.anchorMax = new Vector2(textWidth / m_RectTransform.rect.width + 0.025f, 1);

                if (m_BgImageTransform.anchorMax.x > 0.59f) m_BgImageTransform.anchorMax = new Vector2(0.59f, 1);

                m_Text.alignment = TextAlignmentOptions.Left;
            }
            else
                m_Text.alignment = TextAlignmentOptions.MidlineLeft;

            m_BgImage.color = m_BgColorSelf;
        }

        public void SetStyleBySender(bool isRectStyle)
        {
            if (isRectStyle)
            {
                float textWidth = m_Text.preferredWidth;

                m_BgImageTransform.anchorMin = new Vector2(1 - (textWidth / m_RectTransform.rect.width + 0.025f), 0);
                if (m_BgImageTransform.anchorMin.x < 0.41f) m_BgImageTransform.anchorMin = new Vector2(0.41f, 0);

                m_BgImageTransform.anchorMax = new Vector2(0.99f, 1);

                m_Text.alignment = TextAlignmentOptions.Right;
            }
            else
                m_Text.alignment = TextAlignmentOptions.MidlineLeft;

            m_BgImage.color = m_BgColorSender;
        }

        private void UpdateRectTransformSize()
        {
            float textHeight = m_Text.preferredHeight;

            Vector2 rectSize = m_RectTransform.sizeDelta;
            rectSize.y = textHeight + 10;
            m_RectTransform.sizeDelta = rectSize;
        }
    }
}
