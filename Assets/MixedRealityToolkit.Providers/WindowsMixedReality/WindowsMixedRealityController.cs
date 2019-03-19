// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Attributes;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Core.Definitions.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Providers;
using Microsoft.MixedReality.Toolkit.Core.Services;
using System.Collections.Generic;

#if UNITY_WSA
using UnityEngine;
using UnityEngine.XR.WSA.Input;
#endif

namespace Microsoft.MixedReality.Toolkit.Providers.WindowsMixedReality
{
    /// <summary>
    /// A Windows Mixed Reality Controller Instance.
    /// </summary>
    [MixedRealityController(
        SupportedControllerType.WindowsMixedReality,
        new[] { Handedness.Left, Handedness.Right, Handedness.None },
        "StandardAssets/Textures/MotionController")]
    public class WindowsMixedRealityController : BaseController
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="trackingState"></param>
        /// <param name="controllerHandedness"></param>
        /// <param name="inputSource"></param>
        /// <param name="interactions"></param>
        public WindowsMixedRealityController(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
                : base(trackingState, controllerHandedness, inputSource, interactions) { }

        /// <summary>
        /// The Windows Mixed Reality Controller default interactions.
        /// </summary>
        /// <remarks>A single interaction mapping works for both left and right controllers.</remarks>
        public override MixedRealityInteractionMapping[] DefaultInteractions { get; } = new[]
        {
            new MixedRealityInteractionMapping(0, "Spatial Pointer", AxisType.SixDof, DeviceInputType.SpatialPointer, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(1, "Spatial Grip", AxisType.SixDof, DeviceInputType.SpatialGrip, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(2, "Grip Press", AxisType.SingleAxis, DeviceInputType.TriggerPress, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(3, "Trigger Position", AxisType.SingleAxis, DeviceInputType.Trigger, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(4, "Trigger Touch", AxisType.Digital, DeviceInputType.TriggerTouch, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(5, "Trigger Press (Select)", AxisType.Digital, DeviceInputType.Select, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(6, "Touchpad Position", AxisType.DualAxis, DeviceInputType.Touchpad, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(7, "Touchpad Touch", AxisType.Digital, DeviceInputType.TouchpadTouch, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(8, "Touchpad Press", AxisType.Digital, DeviceInputType.TouchpadPress, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(9, "Menu Press", AxisType.Digital, DeviceInputType.Menu, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(10, "Thumbstick Position", AxisType.DualAxis, DeviceInputType.ThumbStick, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(11, "Thumbstick Press", AxisType.Digital, DeviceInputType.ThumbStickPress, MixedRealityInputAction.None),
        };

#if !UNITY_WSA
        protected override int[] SetupControllerActions(MixedRealityInteractionMapping[] mappings) { return null; }
#else
        protected override int[] SetupControllerActions(MixedRealityInteractionMapping[] mappings)
        {
            List<int> positionalIndices = new List<int>();

            for (int i = 0; i < mappings.Length; i++)
            {
                switch (mappings[i].InputType)
                {
                    case DeviceInputType.SpatialPointer:
                        UpdatePointerData(interactionSourceState, Interactions[i]);
                        break;
                }
            }
        }

        /// <summary>
        /// Update the controller data from the provided platform state
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        public void UpdateController(InteractionSourceState interactionSourceState)
        {
            if (!Enabled) { return; }

            if (Interactions == null)
            {
                Debug.LogError($"No interaction configuration for Windows Mixed Reality Motion Controller {ControllerHandedness}");
                Enabled = false;
            }

            for (int i = 0; i < Interactions?.Length; i++)
            {
                switch (Interactions[i].InputType)
                {
                    case DeviceInputType.None:
                    case DeviceInputType.SpatialPointer:
                        break;
                    case DeviceInputType.Select:
                    case DeviceInputType.Trigger:
                    case DeviceInputType.TriggerTouch:
                    case DeviceInputType.TriggerPress:
                        mappings[i].ControllerAction = UpdateTriggerData;
                    break;
                    case DeviceInputType.SpatialGrip:
                        mappings[i].ControllerAction = UpdateGripData;
                    break;
                    case DeviceInputType.ThumbStick:
                    case DeviceInputType.ThumbStickPress:
                        mappings[i].ControllerAction = UpdateThumbStickData;
                    break;
                    case DeviceInputType.Touchpad:
                    case DeviceInputType.TouchpadTouch:
                    case DeviceInputType.TouchpadPress:
                        mappings[i].ControllerAction = UpdateTouchPadData;
                    break;
                    case DeviceInputType.Menu:
                        mappings[i].ControllerAction = UpdateMenuData;
                    break;
                    default:
                        Debug.LogError($"Input [{mappings[i].InputType}] is not handled for this controller [WindowsMixedRealityController]");
                        Enabled = false;
                    break;
                }
            }

            return positionalIndices.ToArray();
        }

        /// <summary>
        /// The last updated source state reading for this Windows Mixed Reality Controller.
        /// </summary>
        public InteractionSourceState LastSourceStateReading { get; private set; }

        private Vector3 currentControllerPosition = Vector3.zero;
        private Quaternion currentControllerRotation = Quaternion.identity;
        private MixedRealityPose lastControllerPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentControllerPose = MixedRealityPose.ZeroIdentity;

        private Vector3 currentPointerPosition = Vector3.zero;
        private Quaternion currentPointerRotation = Quaternion.identity;
        private MixedRealityPose currentPointerPose = MixedRealityPose.ZeroIdentity;

        private Vector3 currentGripPosition = Vector3.zero;
        private Quaternion currentGripRotation = Quaternion.identity;
        private MixedRealityPose currentGripPose = MixedRealityPose.ZeroIdentity;

        #region Update data functions

        /// <summary>
        /// Update the "Controller" input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        public void UpdateControllerData(InteractionSourceState interactionSourceState)
        {
            if (!Enabled) { return; }

            LastSourceStateReading = interactionSourceState;

            var lastState = TrackingState;
            var sourceKind = interactionSourceState.source.kind;

            lastControllerPose = currentControllerPose;

            if (sourceKind == InteractionSourceKind.Hand ||
               (sourceKind == InteractionSourceKind.Controller && interactionSourceState.source.supportsPointing))
            {
                // The source is either a hand or a controller that supports pointing.
                // We can now check for position and rotation.
                IsPositionAvailable = interactionSourceState.sourcePose.TryGetPosition(out currentControllerPosition);

                IsPositionApproximate = IsPositionAvailable && interactionSourceState.sourcePose.positionAccuracy == InteractionSourcePositionAccuracy.Approximate;

                IsRotationAvailable = interactionSourceState.sourcePose.TryGetRotation(out currentControllerRotation);

                // Devices are considered tracked if we receive position OR rotation data from the sensors.
                TrackingState = (IsPositionAvailable || IsRotationAvailable) ? TrackingState.Tracked : TrackingState.NotTracked;
            }
            else
            {
                // The input source does not support tracking.
                TrackingState = TrackingState.NotApplicable;
            }

            currentControllerPose.Position = currentControllerPosition;
            currentControllerPose.Rotation = currentControllerRotation;

            // Raise input system events if it is enabled.
            if (lastState != TrackingState)
            {
                MixedRealityToolkit.InputSystem?.RaiseSourceTrackingStateChanged(InputSource, this, TrackingState);
            }

            if (TrackingState == TrackingState.Tracked && lastControllerPose != currentControllerPose)
            {
                if (IsPositionAvailable && IsRotationAvailable)
                {
                    MixedRealityToolkit.InputSystem?.RaiseSourcePoseChanged(InputSource, this, currentControllerPose);
                }
                else if (IsPositionAvailable && !IsRotationAvailable)
                {
                    MixedRealityToolkit.InputSystem?.RaiseSourcePositionChanged(InputSource, this, currentControllerPosition);
                }
                else if (!IsPositionAvailable && IsRotationAvailable)
                {
                    MixedRealityToolkit.InputSystem?.RaiseSourceRotationChanged(InputSource, this, currentControllerRotation);
                }
            }
        }

        /// <summary>
        /// Update the "Spatial Pointer" input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        /// <param name="interactionMapping"></param>
        private void UpdatePointerData(MixedRealityInteractionMapping interactionMapping)
        {
            LastSourceStateReading.sourcePose.TryGetPosition(out currentPointerPosition, InteractionSourceNode.Pointer);
            LastSourceStateReading.sourcePose.TryGetRotation(out currentPointerRotation, InteractionSourceNode.Pointer);

            currentPointerPose.Position = currentPointerPosition;
            currentPointerPose.Rotation = currentPointerRotation;

            // Update the interaction data source
            interactionMapping.PoseData = currentPointerPose;

            // If our value changed raise it.
            if (interactionMapping.Changed)
            {
                // Raise input system Event if it enabled
                MixedRealityToolkit.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, currentPointerPose);
            }
        }

        /// <summary>
        /// Update the "Spatial Grip" input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        /// <param name="interactionMapping"></param>
        private void UpdateGripData(MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.AxisType)
            {
                case AxisType.SixDof:
                    {
                        LastSourceStateReading.sourcePose.TryGetPosition(out currentGripPosition, InteractionSourceNode.Grip);
                        LastSourceStateReading.sourcePose.TryGetRotation(out currentGripRotation, InteractionSourceNode.Grip);

                        if (MixedRealityToolkit.Instance.MixedRealityPlayspace != null)
                        {
                            currentGripPose.Position = MixedRealityToolkit.Instance.MixedRealityPlayspace.TransformPoint(currentGripPosition);
                            currentGripPose.Rotation = Quaternion.Euler(MixedRealityToolkit.Instance.MixedRealityPlayspace.TransformDirection(currentGripRotation.eulerAngles));
                        }
                        else
                        {
                            currentGripPose.Position = currentGripPosition;
                            currentGripPose.Rotation = currentGripRotation;
                        }

                        // Update the interaction data source
                        interactionMapping.PoseData = currentGripPose;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            MixedRealityToolkit.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, currentGripPose);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Update the Touchpad input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        /// <param name="interactionMapping"></param>
        private void UpdateTouchPadData(MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.InputType)
            {
                case DeviceInputType.TouchpadTouch:
                    {
                        // Update the interaction data source
                        interactionMapping.BoolData = LastSourceStateReading.touchpadTouched;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            if (LastSourceStateReading.touchpadTouched)
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                            else
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                        }
                        break;
                    }
                case DeviceInputType.TouchpadPress:
                    {
                        //Update the interaction data source
                        interactionMapping.BoolData = LastSourceStateReading.touchpadPressed;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            if (LastSourceStateReading.touchpadPressed)
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                            else
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                        }
                        break;
                    }
                case DeviceInputType.Touchpad:
                    {
                        // Update the interaction data source
                        interactionMapping.Vector2Data = LastSourceStateReading.touchpadPosition;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            MixedRealityToolkit.InputSystem?.RaisePositionInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, LastSourceStateReading.touchpadPosition);
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Update the Thumbstick input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        /// <param name="interactionMapping"></param>
        private void UpdateThumbStickData(MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.InputType)
            {
                case DeviceInputType.ThumbStickPress:
                    {
                        // Update the interaction data source
                        interactionMapping.BoolData = LastSourceStateReading.thumbstickPressed;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            if (LastSourceStateReading.thumbstickPressed)
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                            else
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                        }
                        break;
                    }
                case DeviceInputType.ThumbStick:
                    {
                        // Update the interaction data source
                        interactionMapping.Vector2Data = LastSourceStateReading.thumbstickPosition;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            MixedRealityToolkit.InputSystem?.RaisePositionInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, LastSourceStateReading.thumbstickPosition);
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Update the Trigger input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        /// <param name="interactionMapping"></param>
        private void UpdateTriggerData(MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.InputType)
            {
                case DeviceInputType.TriggerPress:
                    // Update the interaction data source
                    interactionMapping.BoolData = LastSourceStateReading.grasped;

                    // If our value changed raise it.
                    if (interactionMapping.Changed)
                    {
                        // Raise input system Event if it enabled
                        if (LastSourceStateReading.grasped)
                        {
                            MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                        }
                        else
                        {
                            MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                        }
                    }

                    break;
                case DeviceInputType.Select:
                    {
                        bool selectPressed = LastSourceStateReading.selectPressed;

                        // BEGIN WORKAROUND: Unity issue #1033526
                        // See https://issuetracker.unity3d.com/issues/hololens-interactionsourcestate-dot-selectpressed-is-false-when-air-tap-and-hold
                        // Bug was discovered May 2018 and still exists as of today Feb 2019 in version 2018.3.4f1, timeline for fix is unknown
                        // The bug only affects the development workflow via Holographic Remoting or Simulation
                        if (LastSourceStateReading.source.kind == InteractionSourceKind.Hand)
                        {
                            Debug.Assert(!(UnityEngine.XR.WSA.HolographicRemoting.ConnectionState == UnityEngine.XR.WSA.HolographicStreamerConnectionState.Connected
                                           && LastSourceStateReading.selectPressed),
                                         "Unity issue #1033526 seems to have been resolved. Please remove this ugly workaround!");

                            // This workaround is safe as long as all these assumptions hold:
                            Debug.Assert(!LastSourceStateReading.source.supportsGrasp);
                            Debug.Assert(!LastSourceStateReading.source.supportsMenu);
                            Debug.Assert(!LastSourceStateReading.source.supportsPointing);
                            Debug.Assert(!LastSourceStateReading.source.supportsThumbstick);
                            Debug.Assert(!LastSourceStateReading.source.supportsTouchpad);

                            selectPressed = LastSourceStateReading.anyPressed;
                        }
                        // END WORKAROUND: Unity issue #1033526

                        // Update the interaction data source
                        interactionMapping.BoolData = selectPressed;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            if (selectPressed)
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                            else
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                        }
                        break;
                    }
                case DeviceInputType.Trigger:
                    {
                        // Update the interaction data source
                        interactionMapping.FloatData = LastSourceStateReading.selectPressedAmount;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            MixedRealityToolkit.InputSystem?.RaiseOnInputPressed(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, LastSourceStateReading.selectPressedAmount);
                        }
                        break;
                    }
                case DeviceInputType.TriggerTouch:
                    {
                        // Update the interaction data source
                        interactionMapping.BoolData = LastSourceStateReading.selectPressedAmount > 0;

                        // If our value changed raise it.
                        if (interactionMapping.Changed)
                        {
                            // Raise input system Event if it enabled
                            if (LastSourceStateReading.selectPressedAmount > 0)
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                            else
                            {
                                MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                            }
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Update the Menu button state.
        /// </summary>
        /// <param name="interactionSourceState"></param>
        /// <param name="interactionMapping"></param>
        private void UpdateMenuData(MixedRealityInteractionMapping interactionMapping)
        {
            //Update the interaction data source
            interactionMapping.BoolData = LastSourceStateReading.menuPressed;

            // If our value changed raise it.
            if (interactionMapping.Changed)
            {
                // Raise input system Event if it enabled
                if (LastSourceStateReading.menuPressed)
                {
                    MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                }
                else
                {
                    MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                }
            }
        }

        #endregion Update data functions

#endif // UNITY_WSA
    }
}
