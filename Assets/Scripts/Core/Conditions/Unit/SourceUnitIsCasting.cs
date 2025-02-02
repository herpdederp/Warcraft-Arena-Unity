﻿using JetBrains.Annotations;
using UnityEngine;

namespace Core.Conditions
{
    [UsedImplicitly, CreateAssetMenu(fileName = "Source Unit Is Casting", menuName = "Game Data/Conditions/Unit/Source Unit Is Casting", order = 1)]
    public sealed class SourceUnitIsCasting : Condition
    {
        protected internal override bool IsApplicable => SourceUnit != null && base.IsApplicable;

        protected internal override bool IsValid
        {
            get
            {
                bool isValid = IsApplicable && SourceUnit.SpellCast.IsCasting;
                return base.IsValid && isValid;
            }
        }
    }
}
