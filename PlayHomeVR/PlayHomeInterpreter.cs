using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Character;
using VRGIN.Core;

namespace PlayHomeVR
{

    public class PlayHomeInterpreter : GameInterpreter
    {
        public enum EImpersonationTarget
        {
            Male,
            Female,
            Both
        };

        public H_Scene Scene { get; private set; }

        public bool IsLickingScene
        {
            get
            {
                if (Scene != null && Scene.mainMembers != null && Scene.mainMembers.param != null)
                    return Scene.mainMembers.param.mouth == H.H_MOUTH.LICK;
                else
                    return false;
            }
        }

        private List<PlayHomeActor> _Actors = new List<PlayHomeActor>();
        public override IEnumerable<IActor> Actors
        {
            get { return _Actors.Cast<IActor>(); }
        }

        public List<PlayHomeActor> FemaleMainActors { get; } = new List<PlayHomeActor>();

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (Scene)
            {
                RefreshActors();
                foreach (var actor in _Actors)
                    actor.OnUpdate();
            }
            else
            {
                _Actors.Clear();
            }
        }

        protected override void OnLateUpdate()
        {
            foreach (var actor in _Actors)
                actor.OnLateUpdate();
        }

        protected override void OnLevel(int level)
        {
            base.OnLevel(level);

            Scene = GameObject.FindObjectOfType<H_Scene>();
        }

        private void RefreshActors()
        {
            List<PlayHomeActor> newActors = new List<PlayHomeActor>();
            foreach (var memberList in Scene.members)
            {
                foreach (var member in memberList.GetHumans())
                {
                    if (member.isActiveAndEnabled)
                    {
                        newActors.Add(_Actors.Find(x => x.Actor == member) ?? new PlayHomeActor(member));
                    }
                }
            }
            _Actors = newActors;

            FemaleMainActors.Clear();
            foreach (var member in Scene.mainMembers.GetFemales())
            {
                if (member.isActiveAndEnabled)
                {
                    FemaleMainActors.Add(_Actors.Find(x => x.Actor == member) ?? new PlayHomeActor(member));
                }
            }
        }

        public override IActor FindImpersonatedActor()
        {
            return _Actors.FirstOrDefault(a => a.IsImpersonated);
        }

        public override IActor FindNextActorToImpersonate()
        {
            List<PlayHomeActor> actors;
            if (((PlayHomeSettings)VR.Context.Settings).ImpersonationTarget == EImpersonationTarget.Male)
                actors = _Actors.Where(x => x.IsMale).ToList();
            else if (((PlayHomeSettings)VR.Context.Settings).ImpersonationTarget == EImpersonationTarget.Female)
                actors = _Actors.Where(x => x.IsFemale).ToList();
            else
                actors = _Actors;

            var res = actors.OrderByDescending(actor => Vector3.Dot((actor.Eyes.position - VR.Camera.transform.position).normalized, VR.Camera.SteamCam.head.forward)).FirstOrDefault();
            VRGIN.Core.Logger.Debug("Impersonated Actor: " + res != null ? res.Actor.name : "None");
            return res;
        }

        public override bool IsAllowedEffect(MonoBehaviour effect)
        {
            return ((PlayHomeSettings)VR.Context.Settings).PostProcessingEffects
                && (effect.GetType().Name.Equals("ImageEffectConfigChanger")
                || (effect.GetType().Name.Equals("SSAOPro") && ((PlayHomeSettings)VR.Context.Settings).AllowSSAO));
        }

        protected override CameraJudgement JudgeCameraInternal(Camera camera)
        {
            // Never switch to coordinate camera, prevents jumping down below
            // the map when customising character during H scene (#18)
            if (camera.name == "Coordinate Camera")
            {
                return CameraJudgement.Ignore;
            }

            //
            return base.JudgeCameraInternal(camera);
        }
    }
}
