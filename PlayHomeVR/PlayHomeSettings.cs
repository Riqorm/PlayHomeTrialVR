using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRGIN.Core;
using static PlayHomeVR.PlayHomeInterpreter;

namespace PlayHomeVR
{
    [XmlRoot("Settings")]
    public class PlayHomeSettings : VRSettings
    {

        /// <summary>
        /// Valid targets for impersonation
        /// </summary>
        [XmlComment("Valid targets for impersonation. Values: Male, Female, Both")]
        public EImpersonationTarget ImpersonationTarget { get { return _ImpersonationTarget; } set { _ImpersonationTarget = value; TriggerPropertyChanged("ImpersonationTarget"); } }
        private EImpersonationTarget _ImpersonationTarget = EImpersonationTarget.Male;

        /// <summary>
        /// Gets or sets post processing graphic effects
        /// </summary>
        [XmlComment("Enables most post processing graphic effects, allowing you to adjust them in the game settings")]
        public bool PostProcessingEffects { get { return _PostProcessingEffects; } set { _PostProcessingEffects = value; TriggerPropertyChanged("PostProcessingEffects"); } }
        private bool _PostProcessingEffects = true;

        /// <summary>
        /// Allows SSAO to be enabled in the game menu. 
        /// </summary>
        [XmlComment("Allows SSAO to be enabled in the graphics menu. Not recommended because its ressource hungry and looks somewhat strange in VR")]
        public bool AllowSSAO { get { return _AllowSSAO; } set { _AllowSSAO = value; TriggerPropertyChanged("AllowSSAO"); } }
        private bool _AllowSSAO = false;

        /// <summary>
        /// Gets or sets EnableTouching
        /// </summary>
        [XmlComment("Makes dynamic/soft areas like breasts, hair and clothes react to touching them with the controller")]
        public bool EnableTouching { get { return _EnableTouching; } set { _EnableTouching = value; TriggerPropertyChanged("EnableTouching"); } }
        private bool _EnableTouching = true;
    }
}
