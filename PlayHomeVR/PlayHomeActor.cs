using Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Core;

namespace PlayHomeVR
{
    public class PlayHomeActor : DefaultActor<Human>
    {
        public enum EBodyArea
        {
            BREASTS,
            CROTCH,
            THIGHS,
        }

        //private Transform _EyeTransform;
        private Quaternion m_lastHeadRotation;

        public PlayHomeActor(Human nativeActor) : base(nativeActor)
        {
        }

        public override Transform Eyes
        {
            get
            {
                // FIXME get eye position (should be pretty much the same though)
                return Actor.HeadPosTrans;
            }
        }

        private bool m_bHasHead = true;
        public override bool HasHead
        {
            get
            {
                return m_bHasHead;
            }
            set
            {
                m_bHasHead = value;
                SetHeadVisible(value);
            }
        }

        public IEnumerable<DynamicBone> SoftBones
        {
            get
            {
                return Actor.GetComponentsInChildren<DynamicBone>();
            }
        }

        public IEnumerable<DynamicBone_Ver01> SoftBones_V1
        {
            get
            {
                return Actor.GetComponentsInChildren<DynamicBone_Ver01>();
            }
        }

        public IEnumerable<DynamicBone_Ver02> SoftBones_V2
        {
            get
            {
                return Actor.GetComponentsInChildren<DynamicBone_Ver02>();
            }
        }

        public bool IsImpersonated => !HasHead;
        public bool IsMale => Actor is Male;
        public bool IsFemale => Actor is Female;

        protected override void Initialize(Human actor)
        {
            base.Initialize(actor);

            // Register bones
            if (((PlayHomeSettings)VR.Settings).EnableTouching)
            {
                int dbg1 = 0, dbg2 = 0, dbg3 = 0;
                foreach (var bone in SoftBones_V2)
                {
                    dbg1++;
                    DynamicColliderRegistry.RegisterDynamicBone(bone);
                }
                foreach (var bone in SoftBones_V1)
                {
                    dbg2++;
                    DynamicColliderRegistry.RegisterDynamicBone(bone);
                }
                foreach (var bone in SoftBones)
                {
                    dbg3++;
                    DynamicColliderRegistry.RegisterDynamicBone(bone);
                }

                VRLog.Debug("Got " + DynamicColliderRegistry.Bones.Count(x => true) + " Dynamic Bones registered, found " +
                   dbg1 + " V2 Bones, " + dbg2 + " V1 Bones, "
                    + dbg3 + " V0 Bones");
            }
        }

        public void OnUpdate()
        {
            CheckLookAt();
        }

        public void OnLateUpdate()
        {
            // PlayHome is resetting the visibility during loops, overwrite it again during the lateupdate
            if (!HasHead)
                SetHeadVisible(false);
        }

        private void SetHeadVisible(bool bVisible)
        {
            Actor.head.ChangeShow(bVisible);
            Actor.hairs.ChangeShow(bVisible);
            bool bShowTongue = bVisible || (IsMale && ((PlayHomeInterpreter)VR.Interpreter).IsLickingScene);
            if (bShowTongue)
            {
                Actor.SetTongueType(Actor.TongueType);
            }
            else
            {
                Actor.head.SetShowTongue(false);
                Actor.body.SetShowTongue(false);
            }
        }

        public void SetLookAt(Transform target)
        {
            if (Actor.NeckLook.CalcType == LookAtRotator.TYPE.NO)
            {
                Actor.NeckLook.Init();
            }
            Actor.ChangeNeckLook(LookAtRotator.TYPE.TARGET, target, false);
            Actor.ChangeEyeLook(LookAtRotator.TYPE.TARGET, target, false);
            VRGIN.Core.Logger.Debug("Forcing " + Actor.name + " to look");
        }

        public void ClearLookAt()
        {
            m_lastHeadRotation.Set(0, 0, 0, 0);
            Actor.ChangeNeckLook(LookAtRotator.TYPE.FORWARD, null, false);
            VRGIN.Core.Logger.Debug("Start ClearLookAt");
            Actor.ChangeEyeLook(LookAtRotator.TYPE.NO, null, false);
        }

        private void CheckLookAt()
        {
            if (Actor.NeckLook.CalcType == LookAtRotator.TYPE.FORWARD)
            {
                if (Quaternion.Angle(m_lastHeadRotation, Actor.HeadPosTrans.rotation) < 0.0001f)
                {
                    Actor.ChangeNeckLook(LookAtRotator.TYPE.NO, null, false);
                    VRGIN.Core.Logger.Debug("ClearLookAt finished");
                }
                else
                    m_lastHeadRotation = Actor.HeadPosTrans.rotation;
            }
        }

        public bool IsWearingAtLeast(EBodyArea target, WEAR_SHOW min)
        {
            if (target == EBodyArea.BREASTS)
                return (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.BRA) && Actor.wears.GetShow(WEAR_SHOW_TYPE.BRA, false) <= min)
                    || (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.TOPUPPER) && Actor.wears.GetShow(WEAR_SHOW_TYPE.TOPUPPER, false) <= min)
                    || (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.TOPLOWER) && Actor.wears.GetShow(WEAR_SHOW_TYPE.TOPLOWER, false) <= min)
                    || (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SWIM_TOPUPPER) && Actor.wears.GetShow(WEAR_SHOW_TYPE.SWIM_TOPUPPER, false) <= min)
                    || (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SWIM_TOPLOWER) && Actor.wears.GetShow(WEAR_SHOW_TYPE.SWIM_TOPLOWER, false) <= min)
                    || (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SWIMUPPER) && Actor.wears.GetShow(WEAR_SHOW_TYPE.SWIMUPPER, false) <= min);
            else if (target == EBodyArea.CROTCH)
                return (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.BOTTOM) && Actor.wears.GetShow(WEAR_SHOW_TYPE.BOTTOM, false) <= min)
                            || (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.PANST) && Actor.wears.GetShow(WEAR_SHOW_TYPE.PANST, false) <= WEAR_SHOW.ALL)
                            || (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SWIM_BOTTOM) && Actor.wears.GetShow(WEAR_SHOW_TYPE.SWIM_BOTTOM, false) <= min)
                            || (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SWIMLOWER) && Actor.wears.GetShow(WEAR_SHOW_TYPE.SWIMLOWER, false) <= min)
                            || (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SHORTS) && Actor.wears.GetShow(WEAR_SHOW_TYPE.SHORTS, false) <= min);
            else if (target == EBodyArea.THIGHS)
                return (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.BOTTOM) && Actor.wears.GetShow(WEAR_SHOW_TYPE.BOTTOM, false) <= min)
                            || (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.PANST) && Actor.wears.GetShow(WEAR_SHOW_TYPE.PANST, false) <= min);


            return false;
        }

        public bool CanShoveAsidePanties()
        {
            return IsWearingFullPanties() && UndressStep(WEAR_SHOW_TYPE.SHORTS, Actor) == WEAR_SHOW.HALF
                && (!Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.BOTTOM) || Actor.wears.GetShow(WEAR_SHOW_TYPE.BOTTOM, false) >= WEAR_SHOW.HALF)
                && (!Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.PANST) || Actor.wears.GetShow(WEAR_SHOW_TYPE.PANST, false) >= WEAR_SHOW.HALF)
                && (!Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SWIM_BOTTOM) || Actor.wears.GetShow(WEAR_SHOW_TYPE.SWIM_BOTTOM, false) >= WEAR_SHOW.HALF)
                && (!Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SWIMLOWER) || Actor.wears.GetShow(WEAR_SHOW_TYPE.SWIMLOWER, false) >= WEAR_SHOW.HALF);
        }

        public bool IsAnythingButShovingPantiesForCrotch()
        {
            return (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.BOTTOM) && Actor.wears.GetShow(WEAR_SHOW_TYPE.BOTTOM, false) <= WEAR_SHOW.HALF)
                || (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.PANST) && Actor.wears.GetShow(WEAR_SHOW_TYPE.PANST, false) <= WEAR_SHOW.ALL)
                || (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SWIM_BOTTOM) && Actor.wears.GetShow(WEAR_SHOW_TYPE.SWIM_BOTTOM, false) <= WEAR_SHOW.HALF)
                || (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SWIMLOWER) && Actor.wears.GetShow(WEAR_SHOW_TYPE.SWIMLOWER, false) <= WEAR_SHOW.HALF)
                || (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SHORTS) && Actor.wears.GetShow(WEAR_SHOW_TYPE.SHORTS, false) == WEAR_SHOW.HALF);
        }

        public bool IsWearingFullPanties()
        {
            return Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SHORTS) && Actor.wears.GetShow(WEAR_SHOW_TYPE.SHORTS, false) == WEAR_SHOW.ALL;
        }

        public void DoUndressStep(EBodyArea target, bool bShovePantiesAside = false)
        {
            if (bShovePantiesAside && CanShoveAsidePanties())
            {
                Actor.wears.ChangeShow(WEAR_SHOW_TYPE.SHORTS, UndressStep(WEAR_SHOW_TYPE.SHORTS, Actor));
                return;
            }

            if (!DoUndressStep(target, WEAR_SHOW.ALL, !bShovePantiesAside))
                DoUndressStep(target, WEAR_SHOW.HALF, false);
        }

        public void ResetClothes(WEAR_SHOW newState = WEAR_SHOW.ALL)
        {
            for (WEAR_SHOW_TYPE type = WEAR_SHOW_TYPE.TOPUPPER; type < WEAR_SHOW_TYPE.SHOES; ++type)
                Actor.wears.ChangeShow(type, newState);
            Actor.CheckShow();
        }

        private bool DoUndressStep(EBodyArea target, WEAR_SHOW currentStep, bool bNoPanties)
        {
            // Order matters to get the proper undress sequence
            if (target == EBodyArea.BREASTS)
            {
                if ((Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.TOPUPPER) && Actor.wears.GetShow(WEAR_SHOW_TYPE.TOPUPPER, false) == currentStep)
                     || (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.TOPLOWER) && Actor.wears.GetShow(WEAR_SHOW_TYPE.TOPLOWER, false) == currentStep))
                {
                    Actor.wears.ChangeShow(WEAR_SHOW_TYPE.TOPUPPER, UndressStep(WEAR_SHOW_TYPE.TOPUPPER, Actor));
                    Actor.wears.ChangeShow(WEAR_SHOW_TYPE.TOPLOWER, UndressStep(WEAR_SHOW_TYPE.TOPLOWER, Actor));
                }
                else if ((Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SWIM_TOPUPPER) && Actor.wears.GetShow(WEAR_SHOW_TYPE.SWIM_TOPUPPER, false) == currentStep)
                    || (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SWIM_TOPLOWER) && Actor.wears.GetShow(WEAR_SHOW_TYPE.SWIM_TOPLOWER, false) == currentStep))
                {
                    Actor.wears.ChangeShow(WEAR_SHOW_TYPE.SWIM_TOPUPPER, UndressStep(WEAR_SHOW_TYPE.SWIM_TOPUPPER, Actor));
                    Actor.wears.ChangeShow(WEAR_SHOW_TYPE.SWIM_TOPLOWER, UndressStep(WEAR_SHOW_TYPE.SWIM_TOPLOWER, Actor));
                }
                else if (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SWIMUPPER) && Actor.wears.GetShow(WEAR_SHOW_TYPE.SWIMUPPER, false) == currentStep)
                {
                    Actor.wears.ChangeShow(WEAR_SHOW_TYPE.SWIMUPPER, UndressStep(WEAR_SHOW_TYPE.SWIMUPPER, Actor));
                }
                else if (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.BRA) && Actor.wears.GetShow(WEAR_SHOW_TYPE.BRA, false) == currentStep)
                {
                    Actor.wears.ChangeShow(WEAR_SHOW_TYPE.BRA, UndressStep(WEAR_SHOW_TYPE.BRA, Actor));
                }
                else
                {
                    return false;
                }
                Actor.CheckShow();
                return true;
            }
            else if (target == EBodyArea.CROTCH)
            {
                if (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.BOTTOM) && Actor.wears.GetShow(WEAR_SHOW_TYPE.BOTTOM, false) == currentStep)
                {
                    Actor.wears.ChangeShow(WEAR_SHOW_TYPE.BOTTOM, UndressStep(WEAR_SHOW_TYPE.BOTTOM, Actor));
                }
                else if (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.PANST) && Actor.wears.GetShow(WEAR_SHOW_TYPE.PANST, false) == WEAR_SHOW.ALL)
                {
                    Actor.wears.ChangeShow(WEAR_SHOW_TYPE.PANST, UndressStep(WEAR_SHOW_TYPE.PANST, Actor));
                }
                else if (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SWIM_BOTTOM) && Actor.wears.GetShow(WEAR_SHOW_TYPE.SWIM_BOTTOM, false) == currentStep)
                {
                    Actor.wears.ChangeShow(WEAR_SHOW_TYPE.SWIM_BOTTOM, UndressStep(WEAR_SHOW_TYPE.SWIM_BOTTOM, Actor));
                }
                else if (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SWIMLOWER) && Actor.wears.GetShow(WEAR_SHOW_TYPE.SWIMLOWER, false) == currentStep)
                {
                    Actor.wears.ChangeShow(WEAR_SHOW_TYPE.SWIMLOWER, UndressStep(WEAR_SHOW_TYPE.SWIMLOWER, Actor));
                }
                else if (!bNoPanties && Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.SHORTS) && Actor.wears.GetShow(WEAR_SHOW_TYPE.SHORTS, false) == currentStep)
                {
                    Actor.wears.ChangeShow(WEAR_SHOW_TYPE.SHORTS, UndressStep(WEAR_SHOW_TYPE.SHORTS, Actor));
                }
                else
                {
                    return false;
                }
                Actor.CheckShow();
                return true;
            }
            else if (target == EBodyArea.THIGHS)
            {
                if (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.BOTTOM) && Actor.wears.GetShow(WEAR_SHOW_TYPE.BOTTOM, false) == currentStep)
                {
                    Actor.wears.ChangeShow(WEAR_SHOW_TYPE.BOTTOM, UndressStep(WEAR_SHOW_TYPE.BOTTOM, Actor));
                }
                else if (Actor.wears.IsEquiped(Actor.customParam, WEAR_SHOW_TYPE.PANST) && Actor.wears.GetShow(WEAR_SHOW_TYPE.PANST, false) == currentStep)
                {
                    Actor.wears.ChangeShow(WEAR_SHOW_TYPE.PANST, UndressStep(WEAR_SHOW_TYPE.PANST, Actor));
                }
                else
                {
                    return false;
                }
                Actor.CheckShow();
                return true;
            }

            return false;
        }

        private static WEAR_SHOW UndressStep(WEAR_SHOW_TYPE type, Human human)
        {
            WEAR_SHOW now = human.wears.GetShow(type, false);
            WEAR_SHOW res = now;
            switch (human.wears.GetWearShowNum(type))
            {
                case 2:
                    res = (WEAR_SHOW)((int)(now + 1) % 3);
                    break;
                case 1:
                    res = now != WEAR_SHOW.ALL ? WEAR_SHOW.ALL : WEAR_SHOW.HIDE;
                    break;
            }
            return now > res ? now : res;
        }
    }
}
