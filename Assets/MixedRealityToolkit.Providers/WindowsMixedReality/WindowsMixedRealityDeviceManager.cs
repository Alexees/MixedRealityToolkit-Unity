// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Attributes;
using Microsoft.MixedReality.Toolkit.Core.Definitions;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Providers;
using System;

#if UNITY_WSA
using Microsoft.MixedReality.Toolkit.Core.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Core.Definitions.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Extensions;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Services;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.WSA.Input;
using WsaGestureSettings = UnityEngine.XR.WSA.Input.GestureSettings;
#endif // UNITY_WSA

namespace Microsoft.MixedReality.Toolkit.Providers.WindowsMixedReality
{
    [MixedRealityDataProvider(
        typeof(IMixedRealityInputSystem),
        SupportedPlatforms.WindowsUniversal)]
    public class WindowsMixedRealityDeviceManager : BaseDeviceManager, IMixedRealityExtensionService
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Friendly name of the service.</param>
        /// <param name="priority">Service priority. Used to determine order of instantiation.</param>
        /// <param name="profile">The service's configuration profile.</param>
        public WindowsMixedRealityDeviceManager(string name, uint priority, BaseMixedRealityProfile profile) : base(name, priority, profile) { }

#if UNITY_WSA

        /// <summary>
        /// Dictionary to capture all active controllers detected
        /// </summary>
        private readonly Dictionary<uint, IMixedRealityController> activeControllers = new Dictionary<uint, IMixedRealityController>();

        /// <summary>
        /// Cache of the states captured from the Unity InteractionManager for UWP
        /// </summary>
        InteractionSourceState[] interactionmanagerStates;

        /// <summary>
        /// The current source state reading for the Unity InteractionManager for UWP
        /// </summary>
        public InteractionSourceState[] LastInteractionManagerStateReading { get; protected set; }

        /// <inheritdoc/>
        public override IMixedRealityController[] GetActiveControllers()
        {
            return activeControllers.Values.ToArray();
        }

        #region Gesture Settings

        private static bool[] recognizerEnabled = new[] { false, false};

        private const int gestureRecognizerIndex = 0;
        private const int navigationRecognizerIndex = 1;

        /// <summary>
        /// Enables or disables the gesture recognizer.
        /// </summary>
        /// <remarks>
        /// Automatically disabled navigation recognizer if enabled.
        /// </remarks>
        public static bool GestureRecognizerEnabled
        {
            get => recognizerEnabled[gestureRecognizerIndex];
            set => AssignRecognizer(value, gestureRecognizerIndex);
        }

        /// <summary>
        /// Enables or disables the navigation recognizer.
        /// </summary>
        /// <remarks>
        /// Automatically disables the gesture recognizer if enabled.
        /// </remarks>
        public static bool NavigationRecognizerEnabled
        {
            get => recognizerEnabled[navigationRecognizerIndex];
            set => AssignRecognizer(value, navigationRecognizerIndex);
        }

        private static void AssignRecognizer(bool enableRecognizer, int index)
        {
            if (gestureRecognizer[index] == null)
            {
                recognizerEnabled[index] = false;
                return;
            }

            recognizerEnabled[index] = enableRecognizer;

            if (!Application.isPlaying) { return; }

            if (!gestureRecognizer[index].IsCapturingGestures() && recognizerEnabled[index])
            {
                GestureRecognizerEnabled = false;
                gestureRecognizer[index].StartCapturingGestures();
            }

            if (gestureRecognizer[index].IsCapturingGestures() && !recognizerEnabled[index])
            {
                gestureRecognizer[index].CancelGestures();
            }
        }

        private static WindowsGestureSettings gestureSettings = WindowsGestureSettings.Hold | WindowsGestureSettings.ManipulationTranslate;

        /// <summary>
        /// Current Gesture Settings for the GestureRecognizer
        /// </summary>
        public static WindowsGestureSettings GestureSettings
        {
            get { return gestureSettings; }
            set
            {
                gestureSettings = value;

                if (Application.isPlaying)
                {
                    gestureRecognizer[gestureRecognizerIndex]?.UpdateAndResetGestures(WSAGestureSettings);
                }
            }
        }

        private static WindowsGestureSettings navigationSettings = WindowsGestureSettings.NavigationX | WindowsGestureSettings.NavigationY | WindowsGestureSettings.NavigationZ;

        /// <summary>
        /// Current Navigation Gesture Recognizer Settings.
        /// </summary>
        public static WindowsGestureSettings NavigationSettings
        {
            get { return navigationSettings; }
            set
            {
                navigationSettings = value;

                if (Application.isPlaying)
                {
                    gestureRecognizer[navigationRecognizerIndex]?.UpdateAndResetGestures(WSANavigationSettings);
                }
            }
        }

        private static WindowsGestureSettings railsNavigationSettings = WindowsGestureSettings.NavigationRailsX | WindowsGestureSettings.NavigationRailsY | WindowsGestureSettings.NavigationRailsZ;

        /// <summary>
        /// Current Navigation Gesture Recognizer Rails Settings.
        /// </summary>
        public static WindowsGestureSettings RailsNavigationSettings
        {
            get { return railsNavigationSettings; }
            set
            {
                railsNavigationSettings = value;

                if (Application.isPlaying)
                {
                    gestureRecognizer[navigationRecognizerIndex]?.UpdateAndResetGestures(WSARailsNavigationSettings);
                }
            }
        }

        private static bool useRailsNavigation = true;

        /// <summary>
        /// Should the Navigation Gesture Recognizer use Rails?
        /// </summary>
        public static bool UseRailsNavigation
        {
            get { return useRailsNavigation; }
            set
            {
                useRailsNavigation = value;

                if (Application.isPlaying)
                {
                    gestureRecognizer[navigationRecognizerIndex]?.UpdateAndResetGestures(useRailsNavigation ? WSANavigationSettings : WSARailsNavigationSettings);
                }
            }
        }

        private MixedRealityInputAction holdAction = MixedRealityInputAction.None;
        private MixedRealityInputAction navigationAction = MixedRealityInputAction.None;
        private MixedRealityInputAction manipulationAction = MixedRealityInputAction.None;

        private static GestureRecognizer[] gestureRecognizer = new GestureRecognizer[2];
        private static WsaGestureSettings WSAGestureSettings => (WsaGestureSettings)gestureSettings;

        private static WsaGestureSettings WSANavigationSettings => (WsaGestureSettings)navigationSettings;
        private static WsaGestureSettings WSARailsNavigationSettings => (WsaGestureSettings)railsNavigationSettings;

        #endregion Gesture Settings

        #region IMixedRealityDeviceManager Interface

        /// <inheritdoc/>
        public override void Enable()
        {
            if (!Application.isPlaying) { return; }

            RegisterGestureEvents();
            RegisterNavigationEvents();

            var activeProfile = MixedRealityToolkit.Instance.ActiveProfile;
            var gestureProfile = activeProfile.InputSystemProfile.GesturesProfile;

            if (activeProfile.IsInputSystemEnabled &&
                activeProfile.InputSystemProfile.GesturesProfile != null)
            {
                GestureSettings = gestureProfile.ManipulationGestures;
                NavigationSettings = gestureProfile.NavigationGestures;
                RailsNavigationSettings = gestureProfile.RailsNavigationGestures;
                UseRailsNavigation = gestureProfile.UseRailsNavigation;

                for (int i = 0; i < gestureProfile.Gestures.Length; i++)
                {
                    var gesture = gestureProfile.Gestures[i];

                    switch (gesture.GestureType)
                    {
                        case GestureInputType.Hold:
                            holdAction = gesture.Action;
                            break;
                        case GestureInputType.Manipulation:
                            manipulationAction = gesture.Action;
                            break;
                        case GestureInputType.Navigation:
                            navigationAction = gesture.Action;
                            break;
                    }
                }
            }

            InteractionManager.InteractionSourceDetected += InteractionManager_InteractionSourceDetected;
            InteractionManager.InteractionSourceLost += InteractionManager_InteractionSourceLost;

            interactionmanagerStates = InteractionManager.GetCurrentReading();

            // Avoids a Unity Editor bug detecting a controller from the previous run during the first frame
#if !UNITY_EDITOR
            // NOTE: We update the source state data, in case an app wants to query it on source detected.
            foreach(var reading in interactionmanagerStates)
            {
                InteractionManager_InteractionSourceDetected(new InteractionSourceDetectedEventArgs(reading));
            }

#endif
            GestureRecognizerEnabled =
                activeProfile.IsInputSystemEnabled &&
                gestureProfile != null &&
                gestureProfile.WindowsGestureAutoStart == AutoStartBehavior.AutoStart;
        }

        public override void PreServiceUpdate()
        {
            UpdateControllers((controller, sourceState) =>
            {
                controller.UpdateControllerData(sourceState);
                controller.UpdateTransform();
            });
        }

        /// <inheritdoc/>
        public override void Update()
        {
            UpdateControllers((controller, sourceState) => controller.UpdateController(), false);

            LastInteractionManagerStateReading = interactionmanagerStates;
        }

        private void UpdateControllers(Action<WindowsMixedRealityController, InteractionSourceState> controllerAction, bool updateCurrentReading = true)
        {
            if (updateCurrentReading)
            {
                interactionmanagerStates = InteractionManager.GetCurrentReading();
            }

            for (var i = 0; i < interactionmanagerStates?.Length; i++)
            {
                var controller = GetController(interactionmanagerStates[i].source);

                if (controller != null)
                {
                    controllerAction(controller, interactionmanagerStates[i]);
                }
            }
        }

        private void RegisterGestureEvents()
        {
            var recognizer = gestureRecognizer[gestureRecognizerIndex];

            if (recognizer == null)
            {
                recognizer = gestureRecognizer[gestureRecognizerIndex] = new GestureRecognizer();
            }

            recognizer.HoldStarted += GestureRecognizer_HoldStarted;
            recognizer.HoldCompleted += GestureRecognizer_HoldCompleted;
            recognizer.HoldCanceled += GestureRecognizer_HoldCanceled;

            recognizer.ManipulationStarted += GestureRecognizer_ManipulationStarted;
            recognizer.ManipulationUpdated += GestureRecognizer_ManipulationUpdated;
            recognizer.ManipulationCompleted += GestureRecognizer_ManipulationCompleted;
            recognizer.ManipulationCanceled += GestureRecognizer_ManipulationCanceled;
        }

        private void UnregisterGestureEvents()
        {
            var recognizer = gestureRecognizer[gestureRecognizerIndex];

            if (recognizer == null) { return; }

            recognizer.HoldStarted -= GestureRecognizer_HoldStarted;
            recognizer.HoldCompleted -= GestureRecognizer_HoldCompleted;
            recognizer.HoldCanceled -= GestureRecognizer_HoldCanceled;

            recognizer.ManipulationStarted -= GestureRecognizer_ManipulationStarted;
            recognizer.ManipulationUpdated -= GestureRecognizer_ManipulationUpdated;
            recognizer.ManipulationCompleted -= GestureRecognizer_ManipulationCompleted;
            recognizer.ManipulationCanceled -= GestureRecognizer_ManipulationCanceled;
        }

        private void RegisterNavigationEvents()
        {
            var recognizer = gestureRecognizer[navigationRecognizerIndex];

            if (recognizer == null)
            {
                recognizer = gestureRecognizer[navigationRecognizerIndex] = new GestureRecognizer();
            }

            recognizer.NavigationStarted += NavigationGestureRecognizer_NavigationStarted;
            recognizer.NavigationUpdated += NavigationGestureRecognizer_NavigationUpdated;
            recognizer.NavigationCompleted += NavigationGestureRecognizer_NavigationCompleted;
            recognizer.NavigationCanceled += NavigationGestureRecognizer_NavigationCanceled;
        }

        private void UnregisterNavigationEvents()
        {
            var recognizer = gestureRecognizer[navigationRecognizerIndex];

            if (recognizer == null) { return; }

            recognizer.NavigationStarted -= NavigationGestureRecognizer_NavigationStarted;
            recognizer.NavigationUpdated -= NavigationGestureRecognizer_NavigationUpdated;
            recognizer.NavigationCompleted -= NavigationGestureRecognizer_NavigationCompleted;
            recognizer.NavigationCanceled -= NavigationGestureRecognizer_NavigationCanceled;
        }

        /// <inheritdoc/>
        public override void Disable()
        {
            UnregisterGestureEvents();
            gestureRecognizer[navigationRecognizerIndex]?.Dispose();

            UnregisterNavigationEvents();
            gestureRecognizer[navigationRecognizerIndex]?.Dispose();

            InteractionManager.InteractionSourceDetected -= InteractionManager_InteractionSourceDetected;
            InteractionManager.InteractionSourceLost -= InteractionManager_InteractionSourceLost;

            UpdateControllers((controller, sourceState) => RemoveController(controller, sourceState.source), false);
        }

        #endregion IMixedRealityDeviceManager Interface

        #region Controller Utilities

        /// <summary>
        /// Retrieve the source controller from the Active Store, or create a new device and register it
        /// </summary>
        /// <param name="interactionSource">Source State provided by the SDK</param>
        /// <param name="addController">Should the Source be added as a controller if it isn't found?</param>
        /// <returns>New or Existing Controller Input Source</returns>
        private WindowsMixedRealityController GetController(InteractionSource interactionSource)
        {
            //If a device is already registered with the ID provided, just return it.
            if (activeControllers.ContainsKey(interactionSource.id))
            {
                var controller = activeControllers[interactionSource.id] as WindowsMixedRealityController;
                Debug.Assert(controller != null);
                return controller;
            }

            Handedness controllingHand = interactionSource.MixedRealityHandedness();

            var pointers = interactionSource.supportsPointing ? RequestPointers(typeof(WindowsMixedRealityController), controllingHand) : null;
            string nameModifier = controllingHand == Handedness.None ? interactionSource.kind.ToString() : controllingHand.ToString();
            var inputSource = MixedRealityToolkit.InputSystem?.RequestNewGenericInputSource($"Mixed Reality Controller {nameModifier}", pointers);
            var detectedController = new WindowsMixedRealityController(TrackingState.NotTracked, controllingHand, inputSource);

            if (!detectedController.SetupConfiguration(typeof(WindowsMixedRealityController)))
            {
                // Controller failed to be setup correctly.
                return null;
            }

            for (int i = 0; i < detectedController.InputSource?.Pointers?.Length; i++)
            {
                detectedController.InputSource.Pointers[i].Controller = detectedController;
            }

            activeControllers.Add(interactionSource.id, detectedController);

            // SourceDetected gets raised when a new controller is detected and, if previously present, 
            // when OnEnable is called. Do not create a new controller here.
            MixedRealityToolkit.InputSystem?.RaiseSourceDetected(detectedController.InputSource, detectedController);

            return detectedController;
        }

        #endregion Controller Utilities

        /// <summary>
        /// Remove the selected controller from the Active Store
        /// </summary>
        /// <param name="interactionSourceState">Source State provided by the SDK to remove</param>
        private void RemoveController(WindowsMixedRealityController controller, InteractionSource interactionSource)
        {
            MixedRealityToolkit.InputSystem?.RaiseSourceLost(controller.InputSource, controller);
            activeControllers.Remove(interactionSource.id);
        }

        #region Unity InteractionManager Events

        /// <summary>
        /// SDK Interaction Source Detected Event handler
        /// </summary>
        /// <param name="args">SDK source detected event arguments</param>
        private void InteractionManager_InteractionSourceDetected(InteractionSourceDetectedEventArgs args)
        {

            // Avoids a Unity Editor bug detecting a controller from the previous run during the first frame
#if UNITY_EDITOR
            if (Time.frameCount <= 1)
            {
                return;
            }
#endif
            UpdateControllers((controller, sourceState) =>
            {
                controller.UpdateControllerData(sourceState);
                controller.UpdateTransform();
                controller.UpdateController();
            }, true);
        }

        /// <summary>
        /// SDK Interaction Source Lost Event handler
        /// </summary>
        /// <param name="args">SDK source updated event arguments</param>
        private void InteractionManager_InteractionSourceLost(InteractionSourceLostEventArgs args)
        {
            RaiseInputSystemEvent(args.state.source, controller => RemoveController(controller, args.state.source));
        }

        #endregion Unity InteractionManager Events

        #region Gesture Recognizer Events

        private void GestureRecognizer_HoldStarted(HoldStartedEventArgs args)
        {
            RaiseInputSystemEvent(args.source, controller => MixedRealityToolkit.InputSystem?.RaiseGestureStarted(controller, holdAction));
        }

        private void GestureRecognizer_HoldCompleted(HoldCompletedEventArgs args)
        {
            RaiseInputSystemEvent(args.source, controller => MixedRealityToolkit.InputSystem.RaiseGestureCompleted(controller, holdAction));
        }

        private void GestureRecognizer_HoldCanceled(HoldCanceledEventArgs args)
        {
            RaiseInputSystemEvent(args.source, controller => MixedRealityToolkit.InputSystem.RaiseGestureCanceled(controller, holdAction));
        }

        private void GestureRecognizer_ManipulationStarted(ManipulationStartedEventArgs args)
        {
            RaiseInputSystemEvent(args.source, controller => MixedRealityToolkit.InputSystem.RaiseGestureStarted(controller, manipulationAction));
        }

        private void GestureRecognizer_ManipulationUpdated(ManipulationUpdatedEventArgs args)
        {
            RaiseInputSystemEvent(args.source, controller => MixedRealityToolkit.InputSystem.RaiseGestureUpdated(controller, manipulationAction, args.cumulativeDelta));
        }

        private void GestureRecognizer_ManipulationCompleted(ManipulationCompletedEventArgs args)
        {
            RaiseInputSystemEvent(args.source, controller => MixedRealityToolkit.InputSystem.RaiseGestureCompleted(controller, manipulationAction, args.cumulativeDelta));
        }

        private void GestureRecognizer_ManipulationCanceled(ManipulationCanceledEventArgs args)
        {
            RaiseInputSystemEvent(args.source, controller => MixedRealityToolkit.InputSystem.RaiseGestureCanceled(controller, manipulationAction));
        }

        #endregion Gesture Recognizer Events

        #region Navigation Recognizer Events

        private void NavigationGestureRecognizer_NavigationStarted(NavigationStartedEventArgs args)
        {
            RaiseInputSystemEvent(args.source, controller => MixedRealityToolkit.InputSystem.RaiseGestureStarted(controller, navigationAction));
        }

        private void NavigationGestureRecognizer_NavigationUpdated(NavigationUpdatedEventArgs args)
        {
            RaiseInputSystemEvent(args.source, controller => MixedRealityToolkit.InputSystem.RaiseGestureUpdated(controller, navigationAction, args.normalizedOffset));
        }

        private void NavigationGestureRecognizer_NavigationCompleted(NavigationCompletedEventArgs args)
        {
            RaiseInputSystemEvent(args.source, controller => MixedRealityToolkit.InputSystem.RaiseGestureCompleted(controller, navigationAction, args.normalizedOffset));
        }

        private void NavigationGestureRecognizer_NavigationCanceled(NavigationCanceledEventArgs args)
        {
            RaiseInputSystemEvent(args.source, controller => MixedRealityToolkit.InputSystem.RaiseGestureCanceled(controller, navigationAction));
        }

        private void RaiseInputSystemEvent(InteractionSource source, Action<WindowsMixedRealityController> raiseInputEvent)
        {
            var controller = GetController(source);
            if (controller != null)
            {
                raiseInputEvent(controller);
            }
        }

        #endregion Navigation Recognizer Events

#endif // UNITY_WSA

    }
}
