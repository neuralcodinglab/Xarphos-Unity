//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.3.0
//     from Assets/Xarphos/SharedWork/Xarphos-Basement.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @XarphosBasement : IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @XarphosBasement()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""Xarphos-Basement"",
    ""maps"": [
        {
            ""name"": ""Experiment"",
            ""id"": ""0c1a547e-22aa-4aae-8248-84c703b22548"",
            ""actions"": [
                {
                    ""name"": ""Move"",
                    ""type"": ""Value"",
                    ""id"": ""34bd96bc-3257-4350-9dbd-20a6578f8d03"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Look"",
                    ""type"": ""Value"",
                    ""id"": ""f6b2fbf2-2525-497f-8b48-480edbaae60a"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""TogglePhospheneSim"",
                    ""type"": ""Button"",
                    ""id"": ""3fe95d10-e39d-4cc8-87f5-5a1b2a7a1609"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ToggleEdgeDetection"",
                    ""type"": ""Button"",
                    ""id"": ""3e196229-2795-46f2-bae8-3f888893222d"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""IterateSurfaceReplacement"",
                    ""type"": ""Button"",
                    ""id"": ""e0959e22-032b-4278-9985-09bbe7bf988b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ToggleEyeTrackingMode"",
                    ""type"": ""Button"",
                    ""id"": ""37a70d47-4e8b-4905-a375-e611ffd188ef"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ToggleGazeLocking"",
                    ""type"": ""Button"",
                    ""id"": ""35675ead-d89e-41d8-905e-a26a620f4a17"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ToggleCamLocking"",
                    ""type"": ""Button"",
                    ""id"": ""effd86eb-132c-4dc0-8579-f9f87766e61c"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""978bfe49-cc26-4a3d-ab7b-7d7a29327403"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": "";Gamepad"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""1635d3fe-58b6-4ba9-a4e2-f4b964f6b5c8"",
                    ""path"": ""<XRController>/joystick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""XR"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""WASD"",
                    ""id"": ""00ca640b-d935-4593-8157-c05846ea39b3"",
                    ""path"": ""Dpad"",
                    ""interactions"": """",
                    ""processors"": ""ScaleVector2(x=0,y=0)"",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""8180e8bd-4097-4f4e-ab88-4523101a6ce9"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""1c5327b5-f71c-4f60-99c7-4e737386f1d1"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""2e46982e-44cc-431b-9f0b-c11910bf467a"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""77bff152-3580-4b21-b6de-dcd0c7e41164"",
                    ""path"": ""<Keyboard>/h"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""3ea4d645-4504-4529-b061-ab81934c3752"",
                    ""path"": ""<Joystick>/stick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Joystick"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8ca86599-2946-4e18-9fa8-7bd3eebc7691"",
                    ""path"": ""<XRController>/trackpad"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e891f054-6cdc-4392-88ec-222828cb5834"",
                    ""path"": ""<XRController>/touchpad"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d556355b-2f1d-4663-80a3-eaff580a98ee"",
                    ""path"": ""<XRController>/thumbstick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c1f7a91b-d0fd-4a62-997e-7fb9b69bf235"",
                    ""path"": ""<Gamepad>/rightStick"",
                    ""interactions"": """",
                    ""processors"": ""ScaleVector2(x=15,y=15)"",
                    ""groups"": "";Gamepad"",
                    ""action"": ""Look"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8c8e490b-c610-4785-884f-f04217b23ca4"",
                    ""path"": ""<Pointer>/delta"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": "";Keyboard&Mouse;Touch"",
                    ""action"": ""Look"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""3e5f5442-8668-4b27-a940-df99bad7e831"",
                    ""path"": ""<Joystick>/{Hatswitch}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Joystick"",
                    ""action"": ""Look"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""34a92533-1493-489f-97e9-401f2c472119"",
                    ""path"": ""<Keyboard>/p"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""TogglePhospheneSim"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""32b6aaa4-3eee-42d2-9d12-f0792fc402ca"",
                    ""path"": ""<Keyboard>/3"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""TogglePhospheneSim"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""24a67646-4120-4be0-889b-b8fd6b463fe5"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleEdgeDetection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""0d48eca3-feac-46bd-a760-06ecc1e0d7c0"",
                    ""path"": ""<Keyboard>/2"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleEdgeDetection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""3015442e-b235-486c-a9a6-bbf917e480c6"",
                    ""path"": ""<Keyboard>/t"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""IterateSurfaceReplacement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""ab8dff0c-f727-49c3-a20c-017d3030bfa4"",
                    ""path"": ""<Keyboard>/1"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""IterateSurfaceReplacement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""eec3cc20-0a98-451b-954a-9cfb0fd747ef"",
                    ""path"": ""<Keyboard>/g"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleGazeLocking"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""b685618a-b179-4684-8af0-3a47dc8fd4b4"",
                    ""path"": ""<Keyboard>/5"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleGazeLocking"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d50be507-897d-416c-96ce-65ea22dd8b9b"",
                    ""path"": ""<Keyboard>/c"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleCamLocking"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""1c6fc37a-ea91-4895-9a57-e60854d729ac"",
                    ""path"": ""<Keyboard>/6"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleCamLocking"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""89c72108-0434-4308-b703-f72af2a98878"",
                    ""path"": ""<Keyboard>/c"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleEyeTrackingMode"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""32e146f2-3d6a-4d6f-9e5b-4356f0dbd45e"",
                    ""path"": ""<Keyboard>/4"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ToggleEyeTrackingMode"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Keyboard&Mouse"",
            ""bindingGroup"": ""Keyboard&Mouse"",
            ""devices"": [
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        },
        {
            ""name"": ""Gamepad"",
            ""bindingGroup"": ""Gamepad"",
            ""devices"": [
                {
                    ""devicePath"": ""<Gamepad>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        },
        {
            ""name"": ""Touch"",
            ""bindingGroup"": ""Touch"",
            ""devices"": [
                {
                    ""devicePath"": ""<Touchscreen>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        },
        {
            ""name"": ""Joystick"",
            ""bindingGroup"": ""Joystick"",
            ""devices"": [
                {
                    ""devicePath"": ""<Joystick>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        },
        {
            ""name"": ""XR"",
            ""bindingGroup"": ""XR"",
            ""devices"": [
                {
                    ""devicePath"": ""<XRController>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
        // Experiment
        m_Experiment = asset.FindActionMap("Experiment", throwIfNotFound: true);
        m_Experiment_Move = m_Experiment.FindAction("Move", throwIfNotFound: true);
        m_Experiment_Look = m_Experiment.FindAction("Look", throwIfNotFound: true);
        m_Experiment_TogglePhospheneSim = m_Experiment.FindAction("TogglePhospheneSim", throwIfNotFound: true);
        m_Experiment_ToggleEdgeDetection = m_Experiment.FindAction("ToggleEdgeDetection", throwIfNotFound: true);
        m_Experiment_IterateSurfaceReplacement = m_Experiment.FindAction("IterateSurfaceReplacement", throwIfNotFound: true);
        m_Experiment_ToggleEyeTrackingMode = m_Experiment.FindAction("ToggleEyeTrackingMode", throwIfNotFound: true);
        m_Experiment_ToggleGazeLocking = m_Experiment.FindAction("ToggleGazeLocking", throwIfNotFound: true);
        m_Experiment_ToggleCamLocking = m_Experiment.FindAction("ToggleCamLocking", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }
    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }
    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // Experiment
    private readonly InputActionMap m_Experiment;
    private IExperimentActions m_ExperimentActionsCallbackInterface;
    private readonly InputAction m_Experiment_Move;
    private readonly InputAction m_Experiment_Look;
    private readonly InputAction m_Experiment_TogglePhospheneSim;
    private readonly InputAction m_Experiment_ToggleEdgeDetection;
    private readonly InputAction m_Experiment_IterateSurfaceReplacement;
    private readonly InputAction m_Experiment_ToggleEyeTrackingMode;
    private readonly InputAction m_Experiment_ToggleGazeLocking;
    private readonly InputAction m_Experiment_ToggleCamLocking;
    public struct ExperimentActions
    {
        private @XarphosBasement m_Wrapper;
        public ExperimentActions(@XarphosBasement wrapper) { m_Wrapper = wrapper; }
        public InputAction @Move => m_Wrapper.m_Experiment_Move;
        public InputAction @Look => m_Wrapper.m_Experiment_Look;
        public InputAction @TogglePhospheneSim => m_Wrapper.m_Experiment_TogglePhospheneSim;
        public InputAction @ToggleEdgeDetection => m_Wrapper.m_Experiment_ToggleEdgeDetection;
        public InputAction @IterateSurfaceReplacement => m_Wrapper.m_Experiment_IterateSurfaceReplacement;
        public InputAction @ToggleEyeTrackingMode => m_Wrapper.m_Experiment_ToggleEyeTrackingMode;
        public InputAction @ToggleGazeLocking => m_Wrapper.m_Experiment_ToggleGazeLocking;
        public InputAction @ToggleCamLocking => m_Wrapper.m_Experiment_ToggleCamLocking;
        public InputActionMap Get() { return m_Wrapper.m_Experiment; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(ExperimentActions set) { return set.Get(); }
        public void SetCallbacks(IExperimentActions instance)
        {
            if (m_Wrapper.m_ExperimentActionsCallbackInterface != null)
            {
                @Move.started -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnMove;
                @Move.performed -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnMove;
                @Move.canceled -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnMove;
                @Look.started -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnLook;
                @Look.performed -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnLook;
                @Look.canceled -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnLook;
                @TogglePhospheneSim.started -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnTogglePhospheneSim;
                @TogglePhospheneSim.performed -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnTogglePhospheneSim;
                @TogglePhospheneSim.canceled -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnTogglePhospheneSim;
                @ToggleEdgeDetection.started -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnToggleEdgeDetection;
                @ToggleEdgeDetection.performed -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnToggleEdgeDetection;
                @ToggleEdgeDetection.canceled -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnToggleEdgeDetection;
                @IterateSurfaceReplacement.started -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnIterateSurfaceReplacement;
                @IterateSurfaceReplacement.performed -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnIterateSurfaceReplacement;
                @IterateSurfaceReplacement.canceled -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnIterateSurfaceReplacement;
                @ToggleEyeTrackingMode.started -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnToggleEyeTrackingMode;
                @ToggleEyeTrackingMode.performed -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnToggleEyeTrackingMode;
                @ToggleEyeTrackingMode.canceled -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnToggleEyeTrackingMode;
                @ToggleGazeLocking.started -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnToggleGazeLocking;
                @ToggleGazeLocking.performed -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnToggleGazeLocking;
                @ToggleGazeLocking.canceled -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnToggleGazeLocking;
                @ToggleCamLocking.started -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnToggleCamLocking;
                @ToggleCamLocking.performed -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnToggleCamLocking;
                @ToggleCamLocking.canceled -= m_Wrapper.m_ExperimentActionsCallbackInterface.OnToggleCamLocking;
            }
            m_Wrapper.m_ExperimentActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Move.started += instance.OnMove;
                @Move.performed += instance.OnMove;
                @Move.canceled += instance.OnMove;
                @Look.started += instance.OnLook;
                @Look.performed += instance.OnLook;
                @Look.canceled += instance.OnLook;
                @TogglePhospheneSim.started += instance.OnTogglePhospheneSim;
                @TogglePhospheneSim.performed += instance.OnTogglePhospheneSim;
                @TogglePhospheneSim.canceled += instance.OnTogglePhospheneSim;
                @ToggleEdgeDetection.started += instance.OnToggleEdgeDetection;
                @ToggleEdgeDetection.performed += instance.OnToggleEdgeDetection;
                @ToggleEdgeDetection.canceled += instance.OnToggleEdgeDetection;
                @IterateSurfaceReplacement.started += instance.OnIterateSurfaceReplacement;
                @IterateSurfaceReplacement.performed += instance.OnIterateSurfaceReplacement;
                @IterateSurfaceReplacement.canceled += instance.OnIterateSurfaceReplacement;
                @ToggleEyeTrackingMode.started += instance.OnToggleEyeTrackingMode;
                @ToggleEyeTrackingMode.performed += instance.OnToggleEyeTrackingMode;
                @ToggleEyeTrackingMode.canceled += instance.OnToggleEyeTrackingMode;
                @ToggleGazeLocking.started += instance.OnToggleGazeLocking;
                @ToggleGazeLocking.performed += instance.OnToggleGazeLocking;
                @ToggleGazeLocking.canceled += instance.OnToggleGazeLocking;
                @ToggleCamLocking.started += instance.OnToggleCamLocking;
                @ToggleCamLocking.performed += instance.OnToggleCamLocking;
                @ToggleCamLocking.canceled += instance.OnToggleCamLocking;
            }
        }
    }
    public ExperimentActions @Experiment => new ExperimentActions(this);
    private int m_KeyboardMouseSchemeIndex = -1;
    public InputControlScheme KeyboardMouseScheme
    {
        get
        {
            if (m_KeyboardMouseSchemeIndex == -1) m_KeyboardMouseSchemeIndex = asset.FindControlSchemeIndex("Keyboard&Mouse");
            return asset.controlSchemes[m_KeyboardMouseSchemeIndex];
        }
    }
    private int m_GamepadSchemeIndex = -1;
    public InputControlScheme GamepadScheme
    {
        get
        {
            if (m_GamepadSchemeIndex == -1) m_GamepadSchemeIndex = asset.FindControlSchemeIndex("Gamepad");
            return asset.controlSchemes[m_GamepadSchemeIndex];
        }
    }
    private int m_TouchSchemeIndex = -1;
    public InputControlScheme TouchScheme
    {
        get
        {
            if (m_TouchSchemeIndex == -1) m_TouchSchemeIndex = asset.FindControlSchemeIndex("Touch");
            return asset.controlSchemes[m_TouchSchemeIndex];
        }
    }
    private int m_JoystickSchemeIndex = -1;
    public InputControlScheme JoystickScheme
    {
        get
        {
            if (m_JoystickSchemeIndex == -1) m_JoystickSchemeIndex = asset.FindControlSchemeIndex("Joystick");
            return asset.controlSchemes[m_JoystickSchemeIndex];
        }
    }
    private int m_XRSchemeIndex = -1;
    public InputControlScheme XRScheme
    {
        get
        {
            if (m_XRSchemeIndex == -1) m_XRSchemeIndex = asset.FindControlSchemeIndex("XR");
            return asset.controlSchemes[m_XRSchemeIndex];
        }
    }
    public interface IExperimentActions
    {
        void OnMove(InputAction.CallbackContext context);
        void OnLook(InputAction.CallbackContext context);
        void OnTogglePhospheneSim(InputAction.CallbackContext context);
        void OnToggleEdgeDetection(InputAction.CallbackContext context);
        void OnIterateSurfaceReplacement(InputAction.CallbackContext context);
        void OnToggleEyeTrackingMode(InputAction.CallbackContext context);
        void OnToggleGazeLocking(InputAction.CallbackContext context);
        void OnToggleCamLocking(InputAction.CallbackContext context);
    }
}
