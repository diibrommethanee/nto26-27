using System;

namespace _Project.Develop.Gameplay.Tea
{
    public class NextDayBuffService
    {
        public const float MovementSpeedMultiplier = 1.15f;
        public const float AbilityRadiusMultiplier = 1.2f;

        public TeaIngredientType PendingBuff { get; private set; } = TeaIngredientType.None;

        public bool MintActiveThisDay { get; private set; }
        public bool LemonBalmActiveThisDay { get; private set; }
        public bool ThymeActiveThisDay { get; private set; }

        public int? DialoguePointCostOverride => MintActiveThisDay ? 15 : null;

        public void SetPendingBuff(TeaIngredientType type)
        {
            PendingBuff = type;
        }

        public void ApplyPendingForNewDay()
        {
            MintActiveThisDay = PendingBuff == TeaIngredientType.Mint;
            LemonBalmActiveThisDay = PendingBuff == TeaIngredientType.LemonBalm;
            ThymeActiveThisDay = PendingBuff == TeaIngredientType.Thyme;
            PendingBuff = TeaIngredientType.None;
        }

        public void ClearCurrentDayBuffs()
        {
            MintActiveThisDay = false;
            LemonBalmActiveThisDay = false;
            ThymeActiveThisDay = false;
        }
    }
}


