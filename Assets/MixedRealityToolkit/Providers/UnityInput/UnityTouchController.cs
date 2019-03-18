﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
    [MixedRealityController(
        SupportedControllerType.TouchScreen,
        new[] { Handedness.Any })]
    public class UnityTouchController : BaseController
    {
        public UnityTouchController(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
                : base(trackingState, controllerHandedness, inputSource, interactions)
        {
        }

        private const float K_CONTACT_EPSILON = 30.0f;

        /// <summary>
        /// Time in seconds to determine if the contact registers as a tap or a hold
        /// </summary>
        public float MaxTapContactTime { get; set; } = 0.5f;

        /// <summary>
        /// The threshold a finger must move before starting a manipulation gesture.
        /// </summary>
        public float ManipulationThreshold { get; set; } = 5f;

        /// <summary>
        /// Current Touch Data for the Controller.
        /// </summary>
        public Touch TouchData { get; internal set; }

        /// <summary>
        /// Current Screen point ray for the Touch.
        /// </summary>
        public Ray ScreenPointRay { get; internal set; }

        /// <summary>
        /// The current lifetime of the Touch.
        /// </summary>
        public float Lifetime { get; private set; } = 0.0f;

        /// <inheritdoc />
        public override Dictionary<MixedRealityInteractionMapping, Action<MixedRealityInteractionMapping>> DefaultInteractions => new Dictionary<MixedRealityInteractionMapping, Action<MixedRealityInteractionMapping>>()
        {
            { new MixedRealityInteractionMapping(0, "Touch Pointer Delta", AxisType.DualAxis, DeviceInputType.PointerPosition, MixedRealityInputAction.None), PositionChanged },
            { new MixedRealityInteractionMapping(1, "Touch Pointer Position", AxisType.SixDof, DeviceInputType.SpatialPointer, MixedRealityInputAction.None), PoseChanged },
        };

        private KeyValuePair<MixedRealityInteractionMapping, Action<MixedRealityInteractionMapping>> pointerDownInteraction => new KeyValuePair<MixedRealityInteractionMapping, Action<MixedRealityInteractionMapping>>(new MixedRealityInteractionMapping(2, "Touch Press", AxisType.Digital, DeviceInputType.PointerClick, MixedRealityInputAction.None), null);

        private bool isTouched;
        private MixedRealityInputAction holdingAction;
        private bool isHolding;
        private MixedRealityInputAction manipulationAction;
        private bool isManipulating;
        private MixedRealityPose lastPose = MixedRealityPose.ZeroIdentity;

        /// <inheritdoc />
        public override void SetupDefaultInteractions(Handedness controllerHandedness)
        {
            AssignControllerMappings(DefaultInteractions);

            var activeProfiles = MixedRealityToolkit.Instance.ActiveProfile;

            if (activeProfiles.IsInputSystemEnabled &&
                activeProfiles.InputSystemProfile.GesturesProfile != null)
            {
                for (int i = 0; i < activeProfiles.InputSystemProfile.GesturesProfile.Gestures.Length; i++)
                {
                    var gesture = activeProfiles.InputSystemProfile.GesturesProfile.Gestures[i];

                    switch (gesture.GestureType)
                    {
                        case GestureInputType.Hold:
                            holdingAction = gesture.Action;
                            break;
                        case GestureInputType.Manipulation:
                            manipulationAction = gesture.Action;
                            break;
                    }
                }
            }
        }

        public void UpdateControllerData(Touch touch, Ray ray)
        {
            TouchData = touch;
            var pointer = (IMixedRealityTouchPointer)InputSource.Pointers[0];
            ScreenPointRay = pointer.TouchRay = ray;
        }

        /// <summary>
        /// Start the touch.
        /// </summary>
        public void StartTouch()
        {
            MixedRealityToolkit.InputSystem?.RaisePointerDown(InputSource.Pointers[0], pointerDownInteraction.Key.MixedRealityInputAction);
            isTouched = true;
            MixedRealityToolkit.InputSystem?.RaiseGestureStarted(this, holdingAction);
            isHolding = true;
        }

        public void UpdateTransform()
        {
            if (!isTouched) { return; }

            Lifetime += Time.deltaTime;

            if (TouchData.phase != TouchPhase.Moved) { return; }

            foreach(var interaction in Interactions)
            {
                interaction.Value(interaction.Key);
            }
        }

        /// <summary>
        /// Update the touch data.
        /// </summary>
        public void UpdateController()
        {
            if (!isTouched) { return; }

            if (TouchData.phase != TouchPhase.Moved) { return; }

            if (!isManipulating)
            {
                if (Mathf.Abs(TouchData.deltaPosition.x) > ManipulationThreshold ||
                    Mathf.Abs(TouchData.deltaPosition.y) > ManipulationThreshold)
                {
                    MixedRealityToolkit.InputSystem?.RaiseGestureCanceled(this, holdingAction);
                    isHolding = false;

                    MixedRealityToolkit.InputSystem?.RaiseGestureStarted(this, manipulationAction);
                    isManipulating = true;
                }
            }
            else
            {
                MixedRealityToolkit.InputSystem?.RaiseGestureUpdated(this, manipulationAction, TouchData.deltaPosition);
            }
        }

        /// <summary>
        /// End the touch.
        /// </summary>
        public void EndTouch()
        {
            if (TouchData.phase == TouchPhase.Ended)
            {
                if (Lifetime < K_CONTACT_EPSILON)
                {
                    if (isHolding)
                    {
                        MixedRealityToolkit.InputSystem?.RaiseGestureCanceled(this, holdingAction);
                        isHolding = false;
                    }

                    if (isManipulating)
                    {
                        MixedRealityToolkit.InputSystem?.RaiseGestureCanceled(this, manipulationAction);
                        isManipulating = false;
                    }
                }
                else if (Lifetime < MaxTapContactTime)
                {
                    if (isHolding)
                    {
                        MixedRealityToolkit.InputSystem?.RaiseGestureCanceled(this, holdingAction);
                        isHolding = false;
                    }

                    if (isManipulating)
                    {
                        MixedRealityToolkit.InputSystem?.RaiseGestureCanceled(this, manipulationAction);
                        isManipulating = false;
                    }

                    MixedRealityToolkit.InputSystem?.RaisePointerClicked(InputSource.Pointers[0], pointerDownInteraction.Key.MixedRealityInputAction, TouchData.tapCount);
                }

                if (isHolding)
                {
                    MixedRealityToolkit.InputSystem?.RaiseGestureCompleted(this, holdingAction);
                    isHolding = false;
                }

                if (isManipulating)
                {
                    MixedRealityToolkit.InputSystem?.RaiseGestureCompleted(this, manipulationAction, TouchData.deltaPosition);
                    isManipulating = false;
                }
            }

            if (isHolding)
            {
                MixedRealityToolkit.InputSystem?.RaiseGestureCompleted(this, holdingAction);
                isHolding = false;
            }

            Debug.Assert(!isHolding);

            if (isManipulating)
            {
                MixedRealityToolkit.InputSystem?.RaiseGestureCompleted(this, manipulationAction, TouchData.deltaPosition);
                isManipulating = false;
            }

            Debug.Assert(!isManipulating);

            MixedRealityToolkit.InputSystem?.RaisePointerUp(InputSource.Pointers[0], pointerDownInteraction.Key.MixedRealityInputAction);

            Lifetime = 0.0f;
            isTouched = false;

            foreach (var interaction in Interactions)
            {
                switch (interaction.Key.AxisType)
                {
                    case AxisType.DualAxis:
                        interaction.Key.Vector2Data = Vector2.zero;
                        break;
                    case AxisType.SixDof:
                        interaction.Key.PoseData = MixedRealityPose.ZeroIdentity;
                        break;
                }
            }
        }

        private void PositionChanged(MixedRealityInteractionMapping interaction)
        {
            interaction.Vector2Data = TouchData.deltaPosition;

            if (interaction.Changed)
            {
                MixedRealityToolkit.InputSystem?.RaisePositionInputChanged(InputSource, interaction.MixedRealityInputAction, TouchData.deltaPosition);
            }
        }

        private void PoseChanged(MixedRealityInteractionMapping interaction)
        {
            lastPose.Position = InputSource.Pointers[0].BaseCursor.Position;
            lastPose.Rotation = InputSource.Pointers[0].BaseCursor.Rotation;
            MixedRealityToolkit.InputSystem?.RaiseSourcePoseChanged(InputSource, this, lastPose);

            interaction.PoseData = lastPose;

            if (interaction.Changed)
            {
                MixedRealityToolkit.InputSystem?.RaisePoseInputChanged(InputSource, interaction.MixedRealityInputAction, lastPose);
            }
        }
    }
}