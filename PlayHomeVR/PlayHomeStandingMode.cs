using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Controls;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Modes;


namespace PlayHomeVR
{
    class PlayHomeStandingMode : StandingMode
    {
        protected override IEnumerable<IShortcut> CreateShortcuts()
        {
            return base.CreateShortcuts().Concat(new IShortcut[] {
                new MultiKeyboardShortcut(new KeyStroke("Ctrl+C"), new KeyStroke("Ctrl+C"), () => { VR.Manager.SetMode<PlayHomeSeatedMode>(); })
            });
        }

        public override IEnumerable<Type> Tools
        {
            get
            {
                return base.Tools.Concat(new Type[] { typeof(PlayHomePlayTool), typeof(PlayHomeObeyTool) });
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            DynamicColliderRegistry.Clear();
        }

        protected override void CreateControllers()
        {
            base.CreateControllers();

            foreach (var controller in new Controller[] { Left, Right })
            {
                var boneCollider = CreateCollider(controller.transform, 0.06f);
                boneCollider.m_Center.y = -0.03f;
                boneCollider.m_Center.z = 0.01f;
                DynamicColliderRegistry.RegisterCollider(boneCollider, (b) => !IsNotBust(b));

                boneCollider = CreateCollider(controller.transform, 0.08f);
                boneCollider.m_Center.y = -0.03f;
                boneCollider.m_Center.z = 0.01f;
                DynamicColliderRegistry.RegisterCollider(boneCollider, IsNotBust);
            }
        }

        private DynamicBoneCollider CreateCollider(Transform parent, float radius)
        {
            var collider = UnityHelper.CreateGameObjectAsChild("Dynamic Collider", parent).gameObject.AddComponent<DynamicBoneCollider>();
            collider.m_Radius = radius;
            collider.m_Bound = DynamicBoneCollider.Bound.Outside;
            collider.m_Direction = DynamicBoneCollider.Direction.X;
            collider.m_Center.y = 0;
            collider.m_Center.z = 0;
            return collider;
        }

        private static bool IsNotBust(IDynamicBoneWrapper wrapper)
        {
            return !(wrapper.Bone is DynamicBone_Ver02 bonev2 && bonev2.Comment != null && bonev2.Comment.StartsWith("mune"));
        }
    }
}
