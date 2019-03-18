// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Attributes;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Core.Providers.UnityInput
{
    /// <summary>
    /// Xbox Controller using Unity Input System
    /// </summary>
    [MixedRealityController(
        SupportedControllerType.Xbox,
        new[] { Handedness.None },
        "StandardAssets/Textures/XboxController")]
    public class XboxController : GenericJoystickController
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="trackingState"></param>
        /// <param name="controllerHandedness"></param>
        /// <param name="inputSource"></param>
        /// <param name="interactions"></param>
        public XboxController(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
            : base(trackingState, controllerHandedness, inputSource, interactions)
        {
        }

        /// <summary>
        /// Default interactions for Xbox Controller using Unity Input System.
        /// </summary>
        public override Dictionary<MixedRealityInteractionMapping, Action<MixedRealityInteractionMapping>> DefaultInteractions => new Dictionary<MixedRealityInteractionMapping, Action<MixedRealityInteractionMapping>>()
        {
            { new MixedRealityInteractionMapping(0, "Left Thumbstick", AxisType.DualAxis, DeviceInputType.ThumbStick, ControllerMappingLibrary.AXIS_1, ControllerMappingLibrary.AXIS_2, false, true), UpdateDualAxisData },
            { new MixedRealityInteractionMapping(1, "Left Thumbstick Click", AxisType.Digital, DeviceInputType.ButtonPress,KeyCode.JoystickButton8), UpdateButtonData },
            { new MixedRealityInteractionMapping(2, "Right Thumbstick", AxisType.DualAxis, DeviceInputType.ThumbStick, ControllerMappingLibrary.AXIS_4, ControllerMappingLibrary.AXIS_5, false, true), UpdateDualAxisData },
            { new MixedRealityInteractionMapping(3, "Right Thumbstick Click", AxisType.Digital, DeviceInputType.ButtonPress,KeyCode.JoystickButton9), UpdateButtonData },
            { new MixedRealityInteractionMapping(4, "D-Pad", AxisType.DualAxis, DeviceInputType.DirectionalPad, ControllerMappingLibrary.AXIS_6, ControllerMappingLibrary.AXIS_7, false, true), null },
            { new MixedRealityInteractionMapping(5, "Shared Trigger", AxisType.SingleAxis, DeviceInputType.Trigger, ControllerMappingLibrary.AXIS_3), UpdateSingleAxisData },
            { new MixedRealityInteractionMapping(6, "Left Trigger", AxisType.SingleAxis, DeviceInputType.Trigger, ControllerMappingLibrary.AXIS_9), UpdateSingleAxisData },
            { new MixedRealityInteractionMapping(7, "Right Trigger", AxisType.SingleAxis, DeviceInputType.Trigger, ControllerMappingLibrary.AXIS_10), UpdateSingleAxisData },
            { new MixedRealityInteractionMapping(8, "View", AxisType.Digital, DeviceInputType.ButtonPress,KeyCode.JoystickButton6), UpdateButtonData },
            { new MixedRealityInteractionMapping(9, "Menu", AxisType.Digital, DeviceInputType.ButtonPress,KeyCode.JoystickButton7), UpdateButtonData },
            { new MixedRealityInteractionMapping(10, "Left Bumper", AxisType.Digital, DeviceInputType.ButtonPress,KeyCode.JoystickButton4), UpdateButtonData },
            { new MixedRealityInteractionMapping(11, "Right Bumper", AxisType.Digital, DeviceInputType.ButtonPress,KeyCode.JoystickButton5), UpdateButtonData },
            { new MixedRealityInteractionMapping(12, "A", AxisType.Digital, DeviceInputType.ButtonPress,KeyCode.JoystickButton0), UpdateButtonData },
            { new MixedRealityInteractionMapping(13, "B", AxisType.Digital, DeviceInputType.ButtonPress,KeyCode.JoystickButton1), UpdateButtonData },
            { new MixedRealityInteractionMapping(14, "X", AxisType.Digital, DeviceInputType.ButtonPress,KeyCode.JoystickButton2), UpdateButtonData },
            { new MixedRealityInteractionMapping(15, "Y", AxisType.Digital, DeviceInputType.ButtonPress,KeyCode.JoystickButton3), UpdateButtonData },
        };

        /// <inheritdoc />
        public override void SetupDefaultInteractions(Handedness controllerHandedness)
        {
            AssignControllerMappings(DefaultInteractions);
        }
    }
}