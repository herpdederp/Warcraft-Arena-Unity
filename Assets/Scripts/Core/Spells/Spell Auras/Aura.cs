﻿using System;
using System.Collections.Generic;
using Common;

namespace Core
{
    public class Aura
    {
        private const int UpdateTargetInterval = 500;
        private static int AuraAliveCount;

        private readonly List<Unit> tempRemovableTargets = new List<Unit>();
        private readonly HashSet<Unit> tempUpdatedTargets = new HashSet<Unit>();
        private readonly List<AuraEffect> effects = new List<AuraEffect>();
        private readonly List<AuraEffectInfo> effectInfos = new List<AuraEffectInfo>();
        private readonly List<AuraApplication> applications = new List<AuraApplication>();
        private readonly Dictionary<ulong, AuraApplication> applicationsByTargetId = new Dictionary<ulong, AuraApplication>();

        private int updateInvervalLeft;

        internal bool Updated { get; private set; }

        public IReadOnlyList<AuraEffectInfo> EffectsInfos => effectInfos;
        public IReadOnlyList<AuraEffect> Effects => effects;

        public Unit Owner { get; }
        public Unit Caster { get; }
        public AuraInfo Info { get; }
        public ulong CasterId { get; }

        public int Duration { get; private set; }
        public int MaxDuration { get; private set; }

        public int RefreshDuration { get; private set; }
        public int RefreshServerFrame { get; private set; }
        public bool IsRemoved { get; private set; }
        public bool IsExpired => Duration == 0;

        internal Aura(AuraInfo auraInfo, Unit owner, Unit caster)
        {
            Info = auraInfo;
            Caster = caster;
            Owner = owner;
            CasterId = caster?.Id ?? 0;

            UpdateDuration(auraInfo.Duration, auraInfo.MaxDuration);

            effectInfos.AddRange(auraInfo.AuraEffects);

            for (int index = 0; index < effectInfos.Count; index++)
                effects.Add(effectInfos[index].CreateEffect(this, Caster, index));

            Logging.LogAura($"Created aura {Info.name} for target: {Owner.Name}, current count: {++AuraAliveCount}");
        }

        ~Aura()
        {
            Logging.LogAura($"Finalized aura, current count: {--AuraAliveCount}");
        }

        internal void DoUpdate(int deltaTime)
        {
            if (Duration > 0 && (Duration -= deltaTime) < 0)
                Duration = 0;

            if (updateInvervalLeft <= deltaTime)
                UpdateTargets();
            else
                updateInvervalLeft -= deltaTime;

            foreach (AuraEffect effect in effects)
                effect.Update(deltaTime);

            Updated = true;
        }

        internal void LateUpdate()
        {
            Updated = false;
        }

        internal void RegisterForTarget(Unit target, AuraApplication auraApplication)
        {
            applications.Add(auraApplication);
            applicationsByTargetId.Add(target.Id, auraApplication);
        }

        internal void UnregisterForTarget(Unit target, AuraApplication auraApplication)
        {
            applications.Remove(auraApplication);
            applicationsByTargetId.Remove(target.Id);
        }

        internal void Remove(AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            IsRemoved = true;

            while (applications.Count > 0)
            {
                AuraApplication applicationToRemove = applications[0];
                Unit target = applicationToRemove.Target;

                target.Auras.UnapplyAuraApplication(applicationToRemove, removeMode);
            }
        }

        internal void UpdateTargets()
        {
            updateInvervalLeft = UpdateTargetInterval;

            switch (Info.TargetingMode)
            {
                case AuraTargetingMode.Single:
                    AddUnitToAura(Owner);
                    break;
                case AuraTargetingMode.AreaFriend:
                    throw new NotImplementedException();
                case AuraTargetingMode.AreaEnemy:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // remove targets not present in new update
            for (int i = applications.Count - 1; i >= 0; i--)
                if (!tempUpdatedTargets.Contains(applications[i].Target))
                    tempRemovableTargets.Add(applications[i].Target);

            // unapply aura for removed targets
            foreach (Unit removableUnit in tempRemovableTargets)
                if (applicationsByTargetId.TryGetValue(removableUnit.Id, out AuraApplication removableApplication))
                    removableUnit.Auras.UnapplyAuraApplication(removableApplication, AuraRemoveMode.Default);

            tempUpdatedTargets.Clear();
            tempRemovableTargets.Clear();
        }

        internal void UpdateDuration(int duration, int maxDuration)
        {
            Duration = duration;
            MaxDuration = maxDuration;

            RefreshServerFrame = BoltNetwork.ServerFrame;
            RefreshDuration = Duration;

            Owner.VisibleAuras.NeedUpdate = true;
        }

        internal bool CanStackWith(Aura existingAura)
        {
            if (this == existingAura)
                return true;

            bool sameCaster = CasterId == existingAura.CasterId;
            if (!sameCaster && Info == existingAura.Info && !Info.HasAttribute(AuraAttributes.StackForAnyCasters))
                return false;

            return true;
        }

        private void AddUnitToAura(Unit unit)
        {
            tempUpdatedTargets.Add(unit);

            if (applicationsByTargetId.ContainsKey(unit.Id) || unit.IsImmuneToAura(Info, Caster))
                return;

            // check effect for immunity
            int auraEffectMask = 0;
            for (int i = 0; i < effectInfos.Count; i++)
                if (!unit.IsImmuneToAuraEffect(effectInfos[i], Caster))
                    auraEffectMask = auraEffectMask.SetBit(i);

            if (auraEffectMask == 0)
                return;

            // check for non stackable auras
            if (unit != Owner)
                for (int i = 0; i < unit.AuraApplications.Count; i++)
                    if (!CanStackWith(unit.AuraApplications[i].Aura))
                        return;

            unit.Auras.ApplyAuraApplication(new AuraApplication(unit, Caster, this, auraEffectMask));
        }
    }
}