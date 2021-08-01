﻿using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using VRCLiveCaptionsMod.LiveCaptions.GameSpecific;

namespace VRCLiveCaptionsMod.LiveCaptions {
    /// <summary>
    /// Class for creating the floating subtitle UI over audio sources
    /// using TextMeshPro
    /// </summary>
    class SubtitleUi {

        /// <summary>
        /// The parent of all subtitle UIs
        /// </summary>
        private static GameObject subtitleParent;
        public static void Init() {
            subtitleParent = new GameObject();
            subtitleParent.name = "SubtitleUI";
            UnityEngine.Object.DontDestroyOnLoad(subtitleParent);
            //subtitleParent.transform.SetParent(GameUtils.GetSubtitleUiParent());
        }

        /// <summary>
        /// The TranscriptSession that this is associated with
        /// </summary>
        private TranscriptSession session;

        public SubtitleUi(TranscriptSession session) {
            this.session = session;
            InitText();
        }


        private TextMeshPro textMesh;
        private TextMeshPro textMeshBg;

        /// <summary>
        /// The GameObject that contains textMesh component,
        /// and one child containing textMeshBg component
        /// </summary>
        private GameObject textObj;
        
        private Vector3 remoteHeadPositionSmooth = Vector3.zero;
        private Vector3 forwardDirectionSmooth = Vector3.zero;

        /// <summary>
        /// Updates the position of textObj to match the audio source. 
        /// Should be called once every frame.
        /// </summary>
        private void UpdateSubtitleUiTransform() {
            Vector3 remoteHeadPosition = session.audioSource.GetPosition();
            Vector3 localHeadPosition = GameUtils.GetProvider().GetLocalHeadPosition();

            if(
                // when the position isn't initialized
                remoteHeadPositionSmooth.sqrMagnitude < 0.001f ||

                // OR when the player has been teleported (over 16 units)
                (remoteHeadPositionSmooth - remoteHeadPosition).sqrMagnitude > (16.0f*16.0f)
            ) {
                // don't smooth it
                remoteHeadPositionSmooth = remoteHeadPosition;
            } else {
                remoteHeadPositionSmooth = Vector3.Lerp(remoteHeadPositionSmooth, remoteHeadPosition, Time.deltaTime);
            }

            Vector3 forwardDirection = remoteHeadPositionSmooth - localHeadPosition;
            if(forwardDirectionSmooth.sqrMagnitude < 0.001f) {
                forwardDirectionSmooth = forwardDirection;
            } else {
                forwardDirectionSmooth = Vector3.Lerp(forwardDirectionSmooth, forwardDirection, Time.deltaTime * 3.0f);
                forwardDirectionSmooth.Normalize();
            }

            float distance = forwardDirection.sqrMagnitude;

            float dist_fw = -(1.0f / (distance + 1.0f)) + 0.75f;

            Quaternion quat = Quaternion.LookRotation(forwardDirectionSmooth);

            Vector3 offset = (quat * Vector3.right) * 0.0f;

            Vector3 frontVectorFlatPlane = (quat * Vector3.forward);
            frontVectorFlatPlane.Scale(new Vector3(1.0f, 0.0f, 1.0f));
            frontVectorFlatPlane.Normalize();

            offset = offset - frontVectorFlatPlane * dist_fw;
            offset = offset + (Vector3.down * 0.25f);

            Vector3 finalPosition = remoteHeadPositionSmooth + offset;
            Quaternion finalQuat = Quaternion.LookRotation(finalPosition - localHeadPosition);

            textObj.transform.rotation = finalQuat;
            textObj.transform.position = finalPosition;
            textObj.transform.localScale = new Vector3(0.014f, 0.014f, 0.014f) * Settings.TextScale;
        }

        /// <summary>
        /// Initializes the textObj
        /// </summary>
        public void InitText() {
            textObj = new GameObject();
            textObj.name = session.audioSource.GetUID();
            textMesh = textObj.AddComponent<TextMeshPro>();

            textObj.transform.SetParent(subtitleParent.transform);

            textObj.layer = 10;
            
            // TODO; one ui occluding another

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(42, 1);

            textMesh.fontSize /= 2.0f;   
            textMesh.autoSizeTextContainer = false;
            textMesh.richText = true;
            textMesh.enableWordWrapping = false;

            textMesh.fontMaterial.shader = Shader.Find("TextMeshPro/Mobile/Distance Field Overlay");

            GameObject clone = UnityEngine.Object.Instantiate(textObj);
            clone.transform.SetParent(textObj.transform);
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;

            textMeshBg = clone.GetComponent<TextMeshPro>();

            textMeshBg.sortingOrder = 2;
            textMesh.sortingOrder = 3;
        }

        /// <summary>
        /// Updates the text and calls for the position to be updated.
        /// Should be called once every frame.
        /// </summary>
        public void UpdateText() {
            if(textObj == null) return;
            textMesh.text = session.GetText();
            textMeshBg.text = "<mark=#000000aa padding=\"10,10,10,10\"><color=#00000000>" + session.GetText();
            UpdateSubtitleUiTransform();
        }


        public void Dispose() {
            if(textObj != null) {
                textObj.SetActive(false);
                UnityEngine.Object.Destroy(textObj);
                textObj = null;
            }
        }

        ~SubtitleUi() {
        }
    }
}
