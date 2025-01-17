﻿// VRCLiveCaptionsMod - a mod for providing voice chat live captions
// Copyright(C) 2021  gt0777
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.If not, see<https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRChatUtilityKit.Ui;
using System.Runtime.InteropServices;
using VRCLiveCaptionsMod.LiveCaptions.TranscriptData;

namespace VRCLiveCaptionsMod.LiveCaptions.GameSpecific.VRChat {
    class SettingsMenuContents {
        static ToggleButton killswitch;
        static ToggleButton range_transcribe;

        static ToggleButton filter_words;

        static QuarterButton[] size_buttons = new QuarterButton[2];


        static Label model_info;
        static Label update_info;

        static SubMenu submenu;

        static bool was_initialized = false;

        
        public static void Init(SubMenu sub) {
            submenu = sub;

            killswitch = new ToggleButton(submenu.gameObject, new Vector3(0, 0), "Killswitch", "Off", KillChanged,
                "Enabling killswitch will instantly disable live captioning and terminate all captioning sessions.",
                "Enabling killswitch will instantly disable live captioning and terminate all captioning sessions.",
                "killswitchtoggle",
                Settings.Disabled);

            range_transcribe = new ToggleButton(submenu.gameObject, new Vector3(1, 0), "Range auto-transcribe", "Disabled", RangeChanged,
                "Enable this option if you want close players to be automatically transcribed, without needing to manually enable each player. This may consume a lot of memory.",
                "Players close to you will be automatically transcribed.",
                "rangetranscribetoggle",
                Settings.AutoTranscribeWhenInRange);

            filter_words = new ToggleButton(submenu.gameObject, new Vector3(2, 0), "Swear filter", "Disabled", 
                (state) => {
                    Settings.ProfanityFilterLevel = state ? ProfanityFilter.FilterLevel.ALL : ProfanityFilter.FilterLevel.NONE;
                },
                "Enable swear-word filtering.",
                "Swear words will be filtered and replaced with [_].",
                "rangetranscribetoggle",
                Settings.ProfanityFilterLevel == ProfanityFilter.FilterLevel.ALL);

            
            size_buttons[0] = new QuarterButton(submenu.gameObject, new Vector3(3, 0), new Vector2(0, 0), "-",
                () => SizeChange(false),
                "Decrease subtitle size",
                "subtitleSizeDecrease");

            size_buttons[1] = new QuarterButton(submenu.gameObject, new Vector3(3, 0), new Vector2(1, 0), "+",
                () => SizeChange(true),
                "Increase subtitle size",
                "subtitleSizeIncrease");

            model_info = new Label(submenu.gameObject, new Vector3(4, 0), "", "model_info");

            update_info = new Label(submenu.gameObject, new Vector3(5, 0), "", "update_info");
            UiTooltip tooltip = update_info.gameObject.AddComponent<UiTooltip>();

            string UpdateText = UpdateChecker.Check();
            tooltip.field_Public_String_0 = UpdateText;

            if(UpdateText.Contains("up-to-date")) {
                update_info.TextComponent.text = "Up-to-date";
            }else if(UpdateText.Contains("out of date")) {
                update_info.TextComponent.text = "Out-of-date! Hover for more details.";
                GameUtils.LogWarn(UpdateText);
            } else if(UpdateText.Contains("failed")) {
                update_info.TextComponent.text = "Failed to check for updates";
                GameUtils.LogWarn(UpdateText);
            }


            Settings.ModelName = "english-light";


            Settings.ModelChanged += () => {
                UpdateStatusText();
            };
        }

        private static void SizeChange(bool increase) {
            Settings.TextScale = Settings.TextScale + (increase ? 0.05f : -0.05f);
        }

        private static void RangeChanged(bool to) {
            Settings.AutoTranscribeWhenInRange = to;
            Update();
        }

        private static void KillChanged(bool to) {
            Settings.Disabled = to;
            Update();
        }

        static List<GameObject> trash = new List<GameObject>();

        private static int PopulateModelButtons() {
            if(!Directory.Exists(Settings.model_directory)) return -1;
            string[] model_dirs = Directory.GetDirectories(Settings.model_directory);

            int currModel = 0;
            for(int y = 1; y < 3; y++) {
                for(int x = 0; x < 4; x++) {
                    if(currModel >= model_dirs.Length) break;
                    string currModelName = model_dirs[currModel].Replace(Settings.model_directory, "");
                    SingleButton button = new SingleButton(
                        submenu.gameObject,
                        new Vector3(x+1, y),
                        "Use " + currModelName,
                        () => {
                            Settings.ModelName = currModelName;
                            Update();
                        },
                        "Set active model to " + model_dirs[currModel],
                        "modelselector"
                    );

                    GameObject.DontDestroyOnLoad(button.gameObject);

                    trash.Add(button.gameObject);
                    currModel++;
                }
            }

            return model_dirs.Length;
        }

        public static void UpdateStatusText() {
            string modelInfoTxt = "";

            if(!Settings.ModelExists()) {
                modelInfoTxt = "Couldn't find model " + Settings.ModelName;
            } else {
                if(Settings.Vosk_model == null) {
                    if(Settings.Loading) {
                        modelInfoTxt = "Loading model " + Settings.ModelName;
                    } else {
                        modelInfoTxt = "Failed to load model " + Settings.ModelName;
                    }
                } else {
                    modelInfoTxt = "Using model " + Settings.ModelName;
                }
            }

            model_info.TextComponent.text = modelInfoTxt;
        }

        /// <summary>
        /// Sets the current model to the first one in existence by default
        /// </summary>
        private static void FirstInitialize() {
            if(Settings.ModelExists() && (Settings.Vosk_model != null)) return;
            if(!Directory.Exists(Settings.model_directory)) return;
            string[] model_dirs = Directory.GetDirectories(Settings.model_directory);

            if(model_dirs.Length == 0) return;

            Settings.ModelName = model_dirs[0].Replace(Settings.model_directory, "");
        }

        public static void Update() {
            if(!was_initialized) {
                FirstInitialize();
                was_initialized = true;
            }

            foreach(GameObject garbage in trash) {
                GameObject.Destroy(garbage);
            }

            killswitch.State = Settings.Disabled;
            range_transcribe.State = Settings.AutoTranscribeWhenInRange;

            UpdateStatusText();

            int models = PopulateModelButtons();

            string modelInfoTxt = "";
            if(models == -1) {
                modelInfoTxt = "Model directory " + Settings.model_directory + " doesn't exist!";
            } else if(models == 0) {
                modelInfoTxt = "No models found in " + Settings.model_directory + ", please add some.";
            }

            if(modelInfoTxt.Length > 1)
                model_info.TextComponent.text = modelInfoTxt;
        }
    }

    class SettingsTabMenu {
        private static SubMenu menu;
        private static TabButton tabButton;
        

        public static void Init() {
            // Create menu and tab button
            menu = new SubMenu("UserInterface/QuickMenu", "VRCLiveCaptionSettings");
            
            Sprite liveCaptionSprite = GetSpriteFromResource(Properties.Resources.LiveCaptionIcon);
            tabButton = new TabButton(liveCaptionSprite, menu, () => {
                OnTabButtonClick();
            });

            tabButton.gameObject.GetComponent<UiTooltip>().field_Public_String_0 = "Live Captioning settings";

            // Populate
            SettingsMenuContents.Init(menu);
        }

        public static void OnTabButtonClick() {
            SettingsMenuContents.Update();
            tabButton.OpenTabMenu();
        }


        public static Sprite GetSpriteFromResource(System.Drawing.Bitmap resource) {
            MemoryStream ms = new MemoryStream();
            resource.Save(ms, resource.RawFormat);

            Texture2D tex = new Texture2D(resource.Width, resource.Height);
            ImageConversion.LoadImage(tex, ms.ToArray());

            Sprite sprite;
            {
                Rect size = new Rect(0.0f, 0.0f, tex.width, tex.height);
                Vector2 pivot = new Vector2(0.5f, 0.5f);
                Vector4 border = Vector4.zero;
                sprite = Sprite.CreateSprite_Injected(tex, ref size, ref pivot, 100.0f, 0u, SpriteMeshType.Tight, ref border, false);
            }
            return sprite;
        }
    }
}
