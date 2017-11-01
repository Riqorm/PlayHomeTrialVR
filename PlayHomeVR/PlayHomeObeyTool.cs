using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRGIN.Controls;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Helpers;
using static PlayHomeVR.PlayHomeActor;
using Character;
using H;
using UnityEngine.UI;
using Valve.VR;

namespace PlayHomeVR
{
    class PlayHomeObeyTool : Tool
    {
        private enum EHandMode
        {
            NONE,
            SHOW,
            GRABBING
        }
        const float GRABRANGE = 0.2f;
        const float GRABCURSOR_DISTANCE = 0.05f;
        const float GRABCOMPLETEMOVE = 0.1f;
        private EBodyArea m_eGrabTarget;
        private EHandMode m_eHandMode;
        private Canvas m_handCanvas;
        private Image m_handOpen;
        private Image m_handGrab;
        private PlayHomeActor m_grabbedActor;
        private Transform m_raycastTransform;
        private Vector3 m_grabStartControllerPos;
        private Vector3 m_grabStartHitPos;
        private Vector3 m_grabStartGravityL;
        private Vector3 m_grabStartGravityR;
        private bool m_bGravityChanged;
        private TravelDistanceRumble m_rumble;

        public override Texture2D Image
        {
            get
            {
                return UnityHelper.LoadImage("icon_maestro.png");
            }
        }

        protected override void OnDestroy()
        {
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EnterHandMode(EHandMode.NONE);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetupHandCursors();
        }

        protected override void OnStart()
        {
            base.OnStart();
            
            m_rumble = new TravelDistanceRumble(500, GRABCOMPLETEMOVE / 5, m_handCanvas.transform);
            m_rumble.UseLocalPosition = false;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            H_Scene scene = ((PlayHomeInterpreter)VR.Interpreter).Scene;
            if (scene != null)
            {
                var device = this.Controller;
                var tGrip = device.GetPressUp(EVRButtonId.k_EButton_Grip);
                if (tGrip)
                {
                    EnterHandMode(EHandMode.NONE);
                    foreach (var actor in ((PlayHomeInterpreter)VR.Interpreter).FemaleMainActors)
                        actor.ResetClothes();
                }

                UpdateHand();
            }
        }


        private void SetupHandCursors()
        {

            var canvas = m_handCanvas = new GameObject().AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.SetParent(transform, false);

            // Copied straight out of Unity
            canvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200);
            canvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 200);

            canvas.transform.localPosition = new Vector3(0, -0.02725995f, 0.2f);
            canvas.transform.localRotation = Quaternion.Euler(0, 0, 0);
            canvas.transform.localScale = new Vector3(4.930151e-05f, 4.930148e-05f, 0);
            canvas.gameObject.layer = 0;
            m_handOpen = LoadHandCursor("hand.png");
            m_handGrab = LoadHandCursor("hand_grab.png");

            m_raycastTransform = new GameObject().transform;
            m_raycastTransform.SetParent(transform, false);
            if (SteamVR.instance.hmd_TrackingSystemName == "lighthouse")
            {
                m_raycastTransform.localRotation = Quaternion.Euler(60, 0, 0);
                m_raycastTransform.position += m_raycastTransform.forward * 0.06f;
            }

            m_eHandMode = EHandMode.NONE;
        }

        private Image LoadHandCursor(String strImgName)
        {
            var textHand = UnityHelper.LoadImage(strImgName);
            var img = new GameObject().AddComponent<Image>();
            img.transform.SetParent(m_handCanvas.transform, false);

            img.sprite = Sprite.Create(textHand, new Rect(0, 0, textHand.width, textHand.height), new Vector2(0.5f, 0.5f));

            // Maximize
            img.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
            img.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            img.gameObject.layer = 0;
            img.gameObject.SetActive(false);
            return img;
        }

        private void UpdateHand()
        {
            var tTrigger = Controller.GetPress(EVRButtonId.k_EButton_SteamVR_Trigger);

            if (m_eHandMode != EHandMode.NONE)
                m_handCanvas.transform.LookAt(VRCamera.Instance.SteamCam.head);

            if (m_eHandMode == EHandMode.NONE || m_eHandMode == EHandMode.SHOW)
            {
                // pointing and near anything?
                float fRange = Mathf.Clamp(transform.localScale.magnitude * GRABRANGE, GRABRANGE, GRABRANGE * 5) * VR.Settings.IPDScale;
                if (Physics.Raycast(m_raycastTransform.position, m_raycastTransform.forward, out var hit, fRange))
                {
                    // is it a girl?
                    if (hit.collider.GetComponentInParent<Female>() is Female female && VR.Interpreter.Actors.FirstOrDefault(x => ((PlayHomeActor)x).Actor == female) is PlayHomeActor actor)
                    {
                        Vector3 bestHitPoint = hit.point;
                        Transform newParent = null;

                        if (hit.collider.name.Contains("Spine03") && actor.IsWearingAtLeast(EBodyArea.BREASTS, WEAR_SHOW.HALF))
                        {
                            bestHitPoint = GetBreastCollision(female, hit, fRange);
                            EnterHandMode(EHandMode.SHOW);

                            newParent = female.BrestPosTrans;
                            m_eGrabTarget = EBodyArea.BREASTS;
                            m_grabStartGravityL = female.body.bustDynamicBone_L.Gravity;
                            m_grabStartGravityR = female.body.bustDynamicBone_R.Gravity;
                            m_bGravityChanged = false;
                        }
                        else if (hit.collider.name.Contains("kosi") && actor.IsWearingAtLeast(EBodyArea.CROTCH, WEAR_SHOW.HALF))
                        {
                            bestHitPoint = GetBreastCollision(female, hit, fRange);
                            EnterHandMode(EHandMode.SHOW);

                            newParent = female.CrotchTrans;
                            m_eGrabTarget = EBodyArea.CROTCH;
                        }
                        else if (hit.collider.name.Contains("LegUp") && actor.IsWearingAtLeast(EBodyArea.THIGHS, WEAR_SHOW.HALF))
                        {
                            bestHitPoint = GetBreastCollision(female, hit, fRange);
                            EnterHandMode(EHandMode.SHOW);

                            newParent = female.CrotchTrans;
                            m_eGrabTarget = EBodyArea.THIGHS;
                        }
                        else
                        {
                            EnterHandMode(EHandMode.NONE);
                            return;
                        }

                        m_handCanvas.transform.position = Vector3.MoveTowards(bestHitPoint, m_raycastTransform.position, GRABCURSOR_DISTANCE * VR.Settings.IPDScale);

                        if (m_eHandMode == EHandMode.SHOW && tTrigger)
                        {
                            VRLog.Debug("Grabbing " + m_eGrabTarget);
                            EnterHandMode(EHandMode.GRABBING);
                            m_grabbedActor = actor;
                            m_handCanvas.transform.SetParent(newParent);
                            m_grabStartHitPos = m_handCanvas.transform.position;
                        }
                        return;
                    }
                }
                EnterHandMode(EHandMode.NONE);
            }
            else if (m_eHandMode == EHandMode.GRABBING)
            {
                if (!tTrigger)
                {
                    EnterHandMode(EHandMode.NONE);
                    return;
                }

                bool bShovePanties = false;
                float fDist = 0;
                if (m_eGrabTarget == EBodyArea.BREASTS)
                {
                    fDist = m_handCanvas.transform.parent.InverseTransformDirection(m_raycastTransform.position).y - m_handCanvas.transform.parent.InverseTransformDirection(m_grabStartControllerPos).y;
                    m_handCanvas.transform.position = m_grabStartHitPos + (m_handCanvas.transform.parent.up * Math.Max(fDist, 0));

                    // Are the breasts covered with clothes which "pull" on them while we grab?
                    if (m_grabbedActor.IsWearingAtLeast(EBodyArea.BREASTS, WEAR_SHOW.ALL))
                    {
                        m_bGravityChanged = true;
                        m_grabbedActor.Actor.body.bustDynamicBone_L.setGravity(0, m_grabStartGravityL + (m_handCanvas.transform.parent.up * Mathf.Clamp(fDist * 0.03f, 0, GRABCOMPLETEMOVE * 0.03f)));
                        m_grabbedActor.Actor.body.bustDynamicBone_R.setGravity(0, m_grabStartGravityR + (m_handCanvas.transform.parent.up * Mathf.Clamp(fDist * 0.03f, 0, GRABCOMPLETEMOVE * 0.03f)));
                    }
                }
                else if (m_eGrabTarget == EBodyArea.THIGHS)
                {
                    fDist = m_handCanvas.transform.parent.InverseTransformDirection(m_grabStartControllerPos).y - m_handCanvas.transform.parent.InverseTransformDirection(m_raycastTransform.position).y;
                    m_handCanvas.transform.position = m_grabStartHitPos - (m_handCanvas.transform.parent.up * Math.Max(fDist, 0));
                }
                else if (m_eGrabTarget == EBodyArea.CROTCH)
                {
                    // Two Options: Right for panties, down for other things
                    fDist = m_grabbedActor.IsAnythingButShovingPantiesForCrotch() ? m_handCanvas.transform.parent.InverseTransformDirection(m_grabStartControllerPos).y - m_handCanvas.transform.parent.InverseTransformDirection(m_raycastTransform.position).y : 0;
                    float fDistAlt = m_grabbedActor.CanShoveAsidePanties() ? m_handCanvas.transform.parent.InverseTransformDirection(m_grabStartControllerPos).x - m_handCanvas.transform.parent.InverseTransformDirection(m_raycastTransform.position).x : 0;

                    if (fDistAlt > fDist)
                    {
                        fDist = fDistAlt;
                        bShovePanties = true;
                        m_handCanvas.transform.position = m_grabStartHitPos - (m_handCanvas.transform.parent.right * Math.Max(fDist, 0));
                    }
                    else
                    {
                        m_handCanvas.transform.position = m_grabStartHitPos - (m_handCanvas.transform.parent.up * Math.Max(fDist, 0));
                    }
                }

                if (fDist >= GRABCOMPLETEMOVE * VR.Settings.IPDScale)
                {
                    VRLog.Debug("Grab complete for" + m_grabbedActor.Actor.name + " Area " + m_eGrabTarget);
                    m_grabbedActor.DoUndressStep(m_eGrabTarget, bShovePanties);
                    EnterHandMode(EHandMode.NONE);
                }

            }
        }

        private Vector3 GetBreastCollision(Female female, RaycastHit hit, float fRange)
        {
            // FIXME: Being a complete noob with unity (or 3D engines at all actually)
            // I can't figure out how to get the collision point with skin of the breasts (which sizes are dynamic)
            // so this is a somewhat bad replacement. Got more experience? Commit a patch or open an issue

            var ob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            RaycastHit bestHit = hit;
            var bones = female.body.bustDynamicBone_L.Bones;
            if (bones.Count > 2)
            {
                ob.transform.position = Vector3.Lerp(bones[bones.Count - 1].transform.position, bones[1].transform.position, 0.5f);
                float fScale = Vector3.Distance(bones[1].transform.position, bones[bones.Count - 1].transform.position) * 1.2f;
                ob.transform.localScale = new Vector3(fScale, fScale, fScale);
                if (ob.GetComponent<SphereCollider>().Raycast(new Ray(m_raycastTransform.position, m_raycastTransform.forward), out var breastHit, fRange))
                {
                    if (breastHit.distance < bestHit.distance)
                        bestHit = breastHit;
                }
            }
            bones = female.body.bustDynamicBone_R.Bones;
            if (bones.Count > 2)
            {
                ob.transform.position = Vector3.Lerp(bones[bones.Count - 1].transform.position, bones[1].transform.position, 0.5f);
                float fScale = Vector3.Distance(bones[1].transform.position, bones[bones.Count - 1].transform.position) * 1.2f;
                ob.transform.localScale = new Vector3(fScale, fScale, fScale);
                if (ob.GetComponent<SphereCollider>().Raycast(new Ray(m_raycastTransform.position, m_raycastTransform.forward), out var breastHit, fRange))
                {
                    if (breastHit.distance < bestHit.distance)
                        bestHit = breastHit;
                }
            }
            DestroyImmediate(ob);
            return bestHit.point;
        }

        private void EnterHandMode(EHandMode mode)
        {
            if (m_eHandMode == mode)
                return;

            if (m_eHandMode == EHandMode.GRABBING)
            {
                if (m_bGravityChanged)
                {
                    m_grabbedActor.Actor.body.bustDynamicBone_L.setGravity(0, m_grabStartGravityL);
                    m_grabbedActor.Actor.body.bustDynamicBone_R.setGravity(0, m_grabStartGravityR);
                    m_bGravityChanged = false;
                }
                Owner?.StopRumble(m_rumble);
            }

            m_eHandMode = mode;

            if (mode == EHandMode.GRABBING)
            {
                m_grabStartControllerPos = m_raycastTransform.position;
                m_rumble.Reset();
                Owner?.StartRumble(m_rumble);
            }

            m_handOpen.gameObject.SetActive(mode == EHandMode.SHOW);
            m_handGrab.gameObject.SetActive(mode == EHandMode.GRABBING);
        }

        public override List<HelpText> GetHelpTexts()
        {
            return new List<HelpText>(new HelpText[] {
                HelpText.Create("Grab clothes", FindAttachPosition("trigger"), new Vector3(0.06f, 0.04f, -0.05f)),
                HelpText.Create("Reset clothes", FindAttachPosition("lgrip"), new Vector3(-0.06f, 0.0f, -0.05f))
            });
        }
    }
}
