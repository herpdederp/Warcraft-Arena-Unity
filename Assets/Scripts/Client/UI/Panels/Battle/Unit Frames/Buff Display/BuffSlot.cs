﻿using Client.Spells;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class BuffSlot : MonoBehaviour
    {
        [SerializeField, UsedImplicitly] private RenderingReference rendering;
        [SerializeField, UsedImplicitly] private Image contentImage;
        [SerializeField, UsedImplicitly] private Image cooldownImage;
        [SerializeField, UsedImplicitly] private TextMeshProUGUI cooldownText;
        [SerializeField, UsedImplicitly] private CanvasGroup canvasGroup;
        [SerializeField, UsedImplicitly] private bool displayTimer;

        private readonly char[] timerText = { ' ', ' ', ' ' };
        private readonly char[] emptyTimerText = { ' ', ' ', ' ' };

        private IVisibleAura currentAura;

        public void UpdateAura(IVisibleAura visibleAura)
        {
            currentAura = visibleAura;

            if (currentAura == null || !visibleAura.HasActiveAura)
                canvasGroup.alpha = 0.0f;
            else
            {
                if (!displayTimer)
                    cooldownText.SetCharArray(emptyTimerText, 0, 0);

                canvasGroup.alpha = 1.0f;
                contentImage.sprite = rendering.AuraVisualSettingsById.TryGetValue(visibleAura.AuraId, out AuraVisualSettings settings)
                    ? settings.AuraIcon
                    : rendering.DefaultSpellIcon;
            }
        }

        public void DoUpdate()
        {
            if(currentAura == null)
                return;

            if (currentAura.MaxDuration == 0)
            {
                cooldownText.SetCharArray(timerText, 0, 0);
                cooldownImage.fillAmount = 0.0f;
            }
            else
            {
                if (displayTimer)
                {
                    if (currentAura.DurationLeft < 1000)
                        cooldownText.SetCharArray(timerText.SetSpellTimerNonAlloc(currentAura.DurationLeft, out int length), 0, length);
                    else
                        cooldownText.SetCharArray(emptyTimerText, 0, 0);
                }

                cooldownImage.fillAmount = (float)currentAura.DurationLeft / currentAura.MaxDuration;
            }
        }
    }
}
