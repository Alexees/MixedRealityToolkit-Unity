// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Attributes;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Core.Definitions.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Core.Providers.UnityInput
{
    /// <summary>
    /// Manages the mouse using unity input system.
    /// </summary>
    [MixedRealityController(SupportedControllerType.Mouse, new[] { Handedness.Any })]
    public class MouseController : BaseController
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="trackingState"></param>
        /// <param name="controllerHandedness"></param>
        /// <param name="inputSource"></param>
        /// <param name="interactions"></param>
        public MouseController(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
            : base(trackingState, controllerHandedness, inputSource, interactions) { }

        /// <inheritdoc />
        public override Dictionary<MixedRealityInteractionMapping, Action<MixedRealityInteractionMapping>> DefaultInteractions => new Dictionary<MixedRealityInteractionMapping, Action<MixedRealityInteractionMapping>>()
        {
            { new MixedRealityInteractionMapping(0, "Spatial Mouse Position", AxisType.SixDof, DeviceInputType.SpatialPointer, MixedRealityInputAction.None), UpdateMousePosition },
            { new MixedRealityInteractionMapping(1, "Mouse Delta Position", AxisType.DualAxis, DeviceInputType.PointerPosition, MixedRealityInputAction.None), UpdateDeltaPositon },
            { new MixedRealityInteractionMapping(2, "Mouse Scroll Position", AxisType.DualAxis, DeviceInputType.Scroll, ControllerMappingLibrary.AXIS_3), UpdateScrollPositon },
            { new MixedRealityInteractionMapping(3, "Left Mouse Button", AxisType.Digital, DeviceInputType.ButtonPress, MixedRealityInputAction.None, KeyCode.Mouse0), UpdateButton },
            { new MixedRealityInteractionMapping(4, "Right Mouse Button", AxisType.Digital, DeviceInputType.ButtonPress, MixedRealityInputAction.None, KeyCode.Mouse1), UpdateButton },
            { new MixedRealityInteractionMapping(5, "Mouse Button 2", AxisType.Digital, DeviceInputType.ButtonPress, MixedRealityInputAction.None, KeyCode.Mouse2), UpdateButton },
            { new MixedRealityInteractionMapping(6, "Mouse Button 3", AxisType.Digital, DeviceInputType.ButtonPress, MixedRealityInputAction.None, KeyCode.Mouse3), UpdateButton },
            { new MixedRealityInteractionMapping(7, "Mouse Button 4", AxisType.Digital, DeviceInputType.ButtonPress, MixedRealityInputAction.None, KeyCode.Mouse4), UpdateButton },
            { new MixedRealityInteractionMapping(8, "Mouse Button 5", AxisType.Digital, DeviceInputType.ButtonPress, MixedRealityInputAction.None, KeyCode.Mouse5), UpdateButton },
            { new MixedRealityInteractionMapping(9, "Mouse Button 6", AxisType.Digital, DeviceInputType.ButtonPress, MixedRealityInputAction.None, KeyCode.Mouse6), UpdateButton },
        };

        /// <inheritdoc />
        public override void SetupDefaultInteractions(Handedness controllerHandedness)
        {
            AssignControllerMappings(DefaultInteractions);
        }

        private MixedRealityPose controllerPose = MixedRealityPose.ZeroIdentity;
        private Vector2 mouseDelta;
        private bool IsMousePresentAndVisible;

        public void UpdateTransform()
        {
            IsMousePresentAndVisible =
                Input.mousePresent &&
                Input.mousePosition.x >= 0 ||
                Input.mousePosition.y >= 0 ||
                Input.mousePosition.x <= Screen.width ||
                Input.mousePosition.y <= Screen.height;

            if (!IsMousePresentAndVisible)
            {
                return;
            }

            if (InputSource.Pointers[0].BaseCursor != null)
            {
                controllerPose.Position = InputSource.Pointers[0].BaseCursor.Position;
                controllerPose.Rotation = InputSource.Pointers[0].BaseCursor.Rotation;
            }

            foreach (var interaction in Interactions)
            {
                if (interaction.Key.AxisType == AxisType.SixDof)
                {
                    interaction.Value(interaction.Key);
                }
            }
        }

        /// <summary>
        /// Update controller.
        /// </summary>
        public void UpdateController()
        {
            if (!IsMousePresentAndVisible)
            {
                return;
            }

            mouseDelta.x = -Input.GetAxis("Mouse Y");
            mouseDelta.y = Input.GetAxis("Mouse X");
            MixedRealityToolkit.InputSystem?.RaiseSourcePositionChanged(InputSource, this, mouseDelta);
            MixedRealityToolkit.InputSystem?.RaiseSourcePoseChanged(InputSource, this, controllerPose);
            MixedRealityToolkit.InputSystem?.RaiseSourcePositionChanged(InputSource, this, Input.mouseScrollDelta);

            foreach (var interaction in Interactions)
            {
                if (interaction.Key.AxisType != AxisType.SixDof)
                {
                    interaction.Value(interaction.Key);
                }
            }
        }

        private void UpdateMousePosition(MixedRealityInteractionMapping interaction)
        {
            if (InputSource.Pointers[0].BaseCursor != null)
            {
                controllerPose.Position = InputSource.Pointers[0].BaseCursor.Position;
                controllerPose.Rotation = InputSource.Pointers[0].BaseCursor.Rotation;
            }

            interaction.PoseData = controllerPose;

            if (interaction.Changed)
            {
                MixedRealityToolkit.InputSystem?.RaisePoseInputChanged(InputSource, interaction.MixedRealityInputAction, interaction.PoseData);
            }
        }

        private void UpdateDeltaPositon(MixedRealityInteractionMapping interaction)
        {
            interaction.Vector2Data = mouseDelta;

            if (interaction.Changed)
            {
                MixedRealityToolkit.InputSystem?.RaisePositionInputChanged(InputSource, interaction.MixedRealityInputAction, interaction.Vector2Data);
            }
        }

        private void UpdateScrollPositon(MixedRealityInteractionMapping interaction)
        {
            interaction.Vector2Data = Input.mouseScrollDelta;

            if (interaction.Changed)
            {
                MixedRealityToolkit.InputSystem?.RaisePositionInputChanged(InputSource, interaction.MixedRealityInputAction, interaction.Vector2Data);
            }
        }
        private void UpdateButton(MixedRealityInteractionMapping interaction)
        {
            var keyButton = Input.GetKey(interaction.KeyCode);

            // Update the interaction data source
            interaction.BoolData = keyButton;

            // If our value changed raise it.
            if (interaction.Changed)
            {
                // Raise input system Event if it enabled
                if (interaction.BoolData)
                {
                    MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interaction.MixedRealityInputAction);
                }
                else
                {
                    MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interaction.MixedRealityInputAction);
                }
            }
            else
            {
                if (interaction.BoolData)
                {
                    MixedRealityToolkit.InputSystem?.RaiseOnInputPressed(InputSource, ControllerHandedness, interaction.MixedRealityInputAction);
                }
            }
        }
    }
}
