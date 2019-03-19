// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Attributes;
using Microsoft.MixedReality.Toolkit.Core.Definitions;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.Core.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Core.Providers.UnityInput
{
    /// <summary>
    /// Manages Touch devices using unity input system.
    /// </summary>
    [MixedRealityDataProvider(
        typeof(IMixedRealityInputSystem),
        (SupportedPlatforms)(-1))]  // All platforms supported by Unity
    public class UnityTouchDeviceManager : BaseDeviceManager, IMixedRealityExtensionService
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Friendly name of the service.</param>
        /// <param name="priority">Service priority. Used to determine order of instantiation.</param>
        /// <param name="profile">The service's configuration profile.</param>
        public UnityTouchDeviceManager(string name, uint priority, BaseMixedRealityProfile profile) : base(name, priority, profile) { }

        private static readonly Dictionary<int, UnityTouchController> ActiveTouches = new Dictionary<int, UnityTouchController>();

        private Touch[] touches;

        public override void PreServiceUpdate()
        {
            UpdateTouchController((controller, touch) =>
            {
                controller.UpdateControllerData(touch);
                controller.UpdateTransform();
            });
        }

        /// <inheritdoc />
        public override void Update()
        {
            for (var i = 0; i < Input.touches.Length; i++)
            {
                Touch touch = Input.touches[i];

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        UpdateTouchController((controller, touch) => controller.StartTouch());
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        UpdateTouchController((controller, touch) => RemoveTouchController(controller));
                        break;
                }
            }

            foreach (var controller in ActiveTouches)
            {
                controller.Value?.UpdateController();
            }
        }

        /// <inheritdoc />
        public override void Disable()
        {
            foreach (var controller in ActiveTouches)
            {
                if (controller.Value == null || MixedRealityToolkit.InputSystem == null) { continue; }

                foreach (var inputSource in MixedRealityToolkit.InputSystem.DetectedInputSources)
                {
                    if (inputSource.SourceId == controller.Value.InputSource.SourceId)
                    {
                        MixedRealityToolkit.InputSystem.RaiseSourceLost(controller.Value.InputSource, controller.Value);
                    }
                }
            }

            ActiveTouches.Clear();
        }

        private UnityTouchController GetController(Touch touch, bool addController = true)
        {
            // Construct a ray from the current touch coordinates
            Ray ray = CameraCache.Main.ScreenPointToRay(touch.position);

            UnityTouchController controller;

            if (!ActiveTouches.TryGetValue(touch.fingerId, out controller))
            {
                IMixedRealityInputSource inputSource = null;

                if (MixedRealityToolkit.InputSystem != null)
                {
                    var pointers = RequestPointers(typeof(UnityTouchController), Handedness.Any, true);
                    inputSource = MixedRealityToolkit.InputSystem.RequestNewGenericInputSource($"Touch {touch.fingerId}", pointers);
                }

                controller = new UnityTouchController(TrackingState.NotApplicable, Handedness.Any, inputSource);

                if (inputSource != null)
                {
                    for (int i = 0; i < inputSource.Pointers.Length; i++)
                    {
                        inputSource.Pointers[i].Controller = controller;
                        var touchPointer = (IMixedRealityTouchPointer)inputSource.Pointers[i];
                        touchPointer.TouchRay = ray;
                        touchPointer.FingerId = touch.fingerId;
                    }
                }

                if (!controller.SetupConfiguration(typeof(UnityTouchController)))
                {
                    return null;
                }

                ActiveTouches.Add(touch.fingerId, controller);
                MixedRealityToolkit.InputSystem?.RaiseSourceDetected(controller.InputSource, controller);
            }

            Debug.Assert(controller != null);
            return controller;
        }

        private void RemoveTouchController(UnityTouchController controller)
        {
            controller.EndTouch();
            MixedRealityToolkit.InputSystem?.RaiseSourceLost(controller.InputSource, controller);
        }

        private void UpdateTouchController(Action<UnityTouchController, Touch> controllerAction, bool updateCurrentReading = true)
        {
            if (updateCurrentReading)
            {
                touches = Input.touches;
            }

            for (var i = 0; i < touches.Length; i++)
            {
                var controller = GetController(touches[i]);

                if (controller != null)
                {
                    controllerAction?.Invoke(controller, touches[i]);
                }
            }
        }
    }
}
