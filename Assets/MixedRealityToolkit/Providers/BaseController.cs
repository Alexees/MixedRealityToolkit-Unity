// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.Devices;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Services;
using UnityEngine;
using System;

namespace Microsoft.MixedReality.Toolkit.Core.Providers
{
    /// <summary>
    /// Base Controller class to inherit from for all controllers.
    /// </summary>
    public abstract class BaseController : IMixedRealityController
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="trackingState"></param>
        /// <param name="controllerHandedness"></param>
        /// <param name="inputSource"></param>
        /// <param name="interactions"></param>
        protected BaseController(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
        {
            TrackingState = trackingState;
            ControllerHandedness = controllerHandedness;
            InputSource = inputSource;
            AssignInteractions(interactions);
            IsPositionAvailable = false;
            IsPositionApproximate = false;
            IsRotationAvailable = false;

            Enabled = true;
        }

        /// <summary>
        /// The default interactions for this controller.
        /// </summary>
        public virtual MixedRealityInteractionMapping[] DefaultInteractions { get; } = null;

        /// <summary>
        /// The Default Left Handed interactions for this controller.
        /// </summary>
        public virtual MixedRealityInteractionMapping[] DefaultLeftHandedInteractions { get; } = null;

        /// <summary>
        /// The Default Right Handed interactions for this controller.
        /// </summary>
        public virtual MixedRealityInteractionMapping[] DefaultRightHandedInteractions { get; } = null;

        #region IMixedRealityController Implementation

        /// <inheritdoc />
        public bool Enabled { get; set; }

        /// <inheritdoc />
        public TrackingState TrackingState { get; protected set; }

        /// <inheritdoc />
        public Handedness ControllerHandedness { get; }

        /// <inheritdoc />
        public IMixedRealityInputSource InputSource { get; }

        public IMixedRealityControllerVisualizer Visualizer { get; private set; }

        /// <inheritdoc />
        public bool IsPositionAvailable { get; protected set; }

        /// <inheritdoc />
        public bool IsPositionApproximate { get; protected set; }

        /// <inheritdoc />
        public bool IsRotationAvailable { get; protected set; }

        /// <inheritdoc />
        protected MixedRealityInteractionMapping[] InputInteractions { get; set; } = null;

        /// <inheritdoc />
        protected MixedRealityInteractionMapping[] SpatialInteractions { get; set; } = null;

        #endregion IMixedRealityController Implementation

        /// <summary>
        /// Setups up the configuration based on the Mixed Reality Controller Mapping Profile.
        /// </summary>
        /// <param name="controllerType"></param>
        public bool SetupConfiguration(Type controllerType)
        {
            var inputSystemProfile = MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile;

            if (inputSystemProfile.IsControllerMappingEnabled)
            {
                if (inputSystemProfile.ControllerVisualizationProfile.RenderMotionControllers)
                {
                    TryRenderControllerModel(controllerType);
                }

                // We can only enable controller profiles if mappings exist.
                var controllerMappings = inputSystemProfile.ControllerMappingProfile.MixedRealityControllerMappingProfiles;

                // Have to test that a controller type has been registered in the profiles,
                // else its Unity Input manager mappings will not have been set up by the inspector.
                bool profileFound = false;

                for (int i = 0; i < controllerMappings?.Length; i++)
                {
                    if (controllerMappings[i].ControllerType.Type == controllerType)
                    {
                        profileFound = true;

                        // If it is an exact match, assign interaction mappings.
                        if (controllerMappings[i].Handedness == ControllerHandedness &&
                            controllerMappings[i].Interactions.Length > 0)
                        {
                            AssignInteractions(controllerMappings[i].Interactions);
                            break;
                        }
                    }
                }

                // If no controller mappings found, warn the user.  Does not stop the project from running.
                if (InputInteractions == null || InputInteractions.Length < 1)
                {
                    SetupDefaultInteractions(ControllerHandedness);

                    // We still don't have controller mappings, so this may be a custom controller. 
                    if (InputInteractions == null || InputInteractions.Length < 1)
                    {
                        Debug.LogWarning($"No Controller interaction mappings found for {controllerType}.");
                        return false;
                    }
                }

                if (!profileFound)
                {
                    Debug.LogWarning($"No controller profile found for type {controllerType}, please ensure all controllers are defined in the configured MixedRealityControllerConfigurationProfile.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Assign the default interactions based on controller handedness if necessary.  DefaultInteractions take precedense
        /// </summary>
        /// <param name="controllerHandedness"></param>
        protected virtual void SetupDefaultInteractions(Handedness controllerHandedness)
        {
            MixedRealityInteractionMapping[] mappings;

            switch (controllerHandedness)
            {
                case Handedness.Left:
                    mappings =  DefaultInteractions ?? DefaultLeftHandedInteractions;
                    break;
                case Handedness.Right:
                    mappings = DefaultInteractions ?? DefaultRightHandedInteractions;
                    break;
                default:
                    mappings = DefaultInteractions;
                    break;
            }

            AssignInteractions(mappings);
        }

        /// <summary>
        /// Splits mappings up into spatial and regular interactions if necessary
        /// </summary>
        /// <param name="mappings"></param>
        private void AssignInteractions(MixedRealityInteractionMapping[] mappings)
        {
            if (mappings == null) { return; }

            var positionIndices = SetupControllerActions(mappings);

            if (positionIndices == null || positionIndices.Length == 0)
            {
                InputInteractions = mappings;
                return;
            }

            Array.Sort(positionIndices);

            var inputInteractions = new MixedRealityInteractionMapping[mappings.Length - positionIndices.Length];
            var spatialInteractions = new MixedRealityInteractionMapping[positionIndices.Length];

            int positionalIndex = 0;
            for (int i = 0; i < mappings.Length; i++)
            {
                if (positionalIndex < positionIndices.Length && positionIndices[positionalIndex] == i)
                {
                    spatialInteractions[positionalIndex] = mappings[i];
                    positionalIndex++;
                }
                else
                {
                    inputInteractions[i - positionalIndex] = mappings[i];
                }
            }

            InputInteractions = inputInteractions;
            SpatialInteractions = spatialInteractions;
        }

        /// <summary>
        /// Assign actions from script associated with each mapping.
        /// </summary>
        /// <param name="mapping"></param>
        protected abstract int[] SetupControllerActions(MixedRealityInteractionMapping[] mappings);

        public virtual void UpdateControllerTransform()
        {
            UpdateController(true);
        }

        /// <summary>
        /// Update the controller data from the provided platform state
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        public virtual void UpdateControllerInteractions()
        {
            UpdateController(false);
        }

        protected virtual void UpdateController(bool transformUpdate)
        {
            if (!Enabled) { return; }

            if (InputInteractions == null)
            {
                Debug.LogError($"No interaction configuration for Windows Mixed Reality Motion Controller {ControllerHandedness}");
                Enabled = false;
            }

            MixedRealityInteractionMapping[] interactions;
            if (transformUpdate)
            {
                if (SpatialInteractions == null)
                {
                    return;
                }
                interactions = SpatialInteractions;
            }
            else
            {
                interactions = InputInteractions;
            }

            foreach (var interaction in interactions)
            {
                interaction.ControllerAction?.Invoke(interaction);
            }
        }

        private void TryRenderControllerModel(Type controllerType)
        {
            var inputSystemProfile = MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile;
            var visualizationProfile = inputSystemProfile.ControllerVisualizationProfile;

            GameObject controllerModel = null;

            if (!visualizationProfile.RenderMotionControllers) { return; }

            // If a specific controller template wants to override the global model, assign that instead.
            if (inputSystemProfile.IsControllerMappingEnabled &&
                !visualizationProfile.UseDefaultModels)
            {
                controllerModel = visualizationProfile.GetControllerModelOverride(controllerType, ControllerHandedness);
            }

            // Get the global controller model for each hand.
            if (controllerModel == null)
            {
                if (ControllerHandedness == Handedness.Left && visualizationProfile.GlobalLeftHandModel != null)
                {
                    controllerModel = visualizationProfile.GlobalLeftHandModel;
                }
                else if (ControllerHandedness == Handedness.Right && visualizationProfile.GlobalRightHandModel != null)
                {
                    controllerModel = visualizationProfile.GlobalRightHandModel;
                }
            }

            // TODO: add default model assignment here if no prefabs were found, or if settings specified to use them.

            // If we've got a controller model prefab, then place it in the scene.
            if (controllerModel != null)
            {
                var controllerObject = UnityEngine.Object.Instantiate(controllerModel, MixedRealityToolkit.Instance.MixedRealityPlayspace);
                controllerObject.name = $"{ControllerHandedness}_{controllerObject.name}";
                Visualizer = controllerObject.GetComponent<IMixedRealityControllerVisualizer>();

                if (Visualizer != null)
                {
                    Visualizer.Controller = this;
                }
                else
                {
                    Debug.LogError($"{controllerObject.name} is missing a IMixedRealityControllerVisualizer component!");
                }
            }
        }
    }
}