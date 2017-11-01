using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Helpers;

namespace PlayHomeVR
{
    public static class DynamicColliderRegistry
    {
        private static IDictionary<DynamicBoneCollider, Predicate<IDynamicBoneWrapper>> _Colliders = new Dictionary<DynamicBoneCollider, Predicate<IDynamicBoneWrapper>>();
        private static IList<IDynamicBoneWrapper> _Bones = new List<IDynamicBoneWrapper>();

        public static IEnumerable<IDynamicBoneWrapper> Bones { get { return _Bones; } }

        public static void RegisterCollider(DynamicBoneCollider collider, Predicate<IDynamicBoneWrapper> targetSelector = null)
        {
            if (targetSelector == null) targetSelector = (bone) => true;

            _Colliders[collider] = targetSelector;

            foreach (var bone in _Bones)
            {
                Correlate(bone, collider, targetSelector);
            }
        }

        private static void Correlate(IDynamicBoneWrapper bone, DynamicBoneCollider collider, Predicate<IDynamicBoneWrapper> targetSelector)
        {
            if (targetSelector(bone))
            {
                if (!bone.Colliders.Contains(collider))
                {
                    bone.Colliders.Add(collider);
                }
            }
            else
            {
                bone.Colliders.Remove(collider);
            }
        }

        private static void Register(IDynamicBoneWrapper wrapper)
        {
            _Bones.Add(wrapper);

            foreach (var colliderPair in _Colliders)
            {
                Correlate(wrapper, colliderPair.Key, colliderPair.Value);
            }
        }

        public static void RegisterDynamicBone(DynamicBone bone)
        {
            Register(new DynamicBoneWrapper(bone));
        }

        public static void RegisterDynamicBone(DynamicBone_Ver01 bone)
        {
            Register(new DynamicBone_V1Wrapper(bone));
        }

        public static void RegisterDynamicBone(DynamicBone_Ver02 bone)
        {
            Register(new DynamicBone_V2Wrapper(bone));
        }

        public static void Clear()
        {
            _Colliders.Clear();
            _Bones.Clear();
        }

        public static Predicate<IDynamicBoneWrapper> GetCondition(DynamicBoneCollider key)
        {
            return _Colliders[key];
        }
    }


    public interface IDynamicBoneWrapper
    {
        MonoBehaviour Bone
        {
            get;
        }

        Transform Root
        {
            get;
        }

        Transform Rim
        {
            get;
        }

        IEnumerable<Transform> Nodes { get; }


        List<DynamicBoneCollider> Colliders { get; }
    }

    public class DynamicBoneWrapper : IDynamicBoneWrapper
    {
        public MonoBehaviour Bone
        {
            get;
            private set;
        }

        public List<DynamicBoneCollider> Colliders
        {
            get
            {
                return (Bone as DynamicBone).m_Colliders;
            }
        }

        public Transform Root
        {
            get
            {
                return (Bone as DynamicBone).m_Root;
            }
        }

        public Transform Rim
        {
            get
            {
                return (Bone as DynamicBone).m_Root.gameObject.Descendants().Last().transform;
            }
        }

        public IEnumerable<Transform> Nodes
        {
            get
            {
                return Root.gameObject.Descendants().Select(d => d.transform).Concat(new Transform[] { Root });
            }
        }

        public DynamicBoneWrapper(DynamicBone bone)
        {
            Bone = bone;
        }
    }

    public class DynamicBone_V1Wrapper : IDynamicBoneWrapper
    {
        public MonoBehaviour Bone
        {
            get;
            private set;
        }

        public List<DynamicBoneCollider> Colliders
        {
            get
            {
                return (Bone as DynamicBone_Ver01).m_Colliders;
            }
        }

        public Transform Root
        {
            get
            {
                return (Bone as DynamicBone_Ver01).m_Root;
            }
        }

        public Transform Rim
        {
            get
            {
                return (Bone as DynamicBone_Ver01).m_Nodes.Last().Transform;
            }
        }

        public IEnumerable<Transform> Nodes
        {
            get
            {
                // could use a wrapper instead, but I don't think V1 dynamic bones are actually used at all
                // so keep it simple for now
                return (Bone as DynamicBone_Ver01).m_Nodes.Select(x => x.Transform).ToList();
            }
        }

        public DynamicBone_V1Wrapper(DynamicBone_Ver01 bone)
        {
            Bone = bone;
        }
    }

    public class DynamicBone_V2Wrapper : IDynamicBoneWrapper
    {
        public MonoBehaviour Bone
        {
            get;
            private set;
        }

        public List<DynamicBoneCollider> Colliders
        {
            get
            {
                return (Bone as DynamicBone_Ver02).Colliders;
            }
        }

        public Transform Root
        {
            get
            {
                return (Bone as DynamicBone_Ver02).Root;
            }
        }

        public Transform Rim
        {
            get
            {
                return (Bone as DynamicBone_Ver02).Bones.Last();
            }
        }

        public IEnumerable<Transform> Nodes
        {
            get
            {
                return (Bone as DynamicBone_Ver02).Bones;
            }
        }

        public DynamicBone_V2Wrapper(DynamicBone_Ver02 bone)
        {
            Bone = bone;
        }
    }
}
