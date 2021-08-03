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

namespace VRCLiveCaptionsMod.LiveCaptions {
    /// <summary>
    /// Holds a whitelist of audio sources for transcribing.
    /// </summary>
    public class AudioSourceOverrides {
        /// <summary>
        /// The dictionary which stores the overrides
        /// </summary>
        private static Dictionary<string, bool> overrides = new Dictionary<string, bool>();

        /// <summary>
        /// Emitted when a UID is removed from the whitelist
        /// </summary>
        public static event Action<string> OnRemovedFromWhitelist;

        /// <summary>
        /// Emitted when a UID is added to the whitelist
        /// </summary>
        public static event Action<string> OnAddedToWhitelist;
        
        /// <summary>
        /// Static initializer
        /// </summary>
        public static void Init() {
            // TODO: Load/save from file
        }

        /// <summary>
        /// This method changesthe override state of a UID
        /// </summary>
        /// <param name="uid">UID to affect (from IAudioSource.GetUID())</param>
        /// <param name="is_whitelisted">Whether to whitelist the audio source or not</param>
        public static void SetOverride(string uid, bool is_whitelisted) {
            overrides[uid] = is_whitelisted;

            if(is_whitelisted) OnAddedToWhitelist?.DelegateSafeInvoke(uid);
            else OnRemovedFromWhitelist?.DelegateSafeInvoke(uid);

            // TODO: save config
        }

        /// <summary>
        /// Check whether or not a UID is whitelisted
        /// </summary>
        /// <param name="uid">The UID to check (from IAudioSource.GetUID())</param>
        /// <returns>Whether or not the UID has transcription whitelisted</returns>
        public static bool IsWhitelisted(string uid) {
            return overrides.ContainsKey(uid) && (overrides[uid] == true);
        }
    }
}