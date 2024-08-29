/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using UnityEngine;
using UnityEngine.EventSystems;

namespace Meta.XR.ImmersiveDebugger.UserInterface
{
    internal class ProxyInputModule
    {
        private readonly GameObject _owner;
        private readonly OVRCursor _cursor;
        private EventSystem _eventSystem;

        public OVRInputModule InputModule { get; private set; }

        public ProxyInputModule(GameObject owner, OVRCursor cursor)
        {
            _cursor = cursor;
            _owner = owner;
        }

        public bool Refresh()
        {
            if (InputModule) return true;

            SearchForEventSystem();

            return InputModule;
        }

        private void SearchForEventSystem()
        {
            var eventSystem = GameObject.FindAnyObjectByType<EventSystem>();
            if (!eventSystem && RuntimeSettings.Instance.CreateEventSystem)
            {
                eventSystem = _owner.AddComponent<EventSystem>();
            }
            SetupEventSystem(eventSystem);
        }

        private void SetupEventSystem(EventSystem eventSystem)
        {
            _eventSystem = eventSystem;

            if (!_eventSystem) return;

            if (!_eventSystem.TryGetComponent<OVRInputModule>(out var inputModule))
            {
                inputModule = _eventSystem.gameObject.AddComponent<PanelInputModule>();
                _eventSystem.UpdateModules();
            }
            SetupInputModule(inputModule);
        }

        private void SetupInputModule(OVRInputModule inputModule)
        {
            InputModule = inputModule;

            if (!InputModule) return;

            InputModule.m_Cursor ??= _cursor;
            InputModule.allowActivationOnMobileDevice = true;
            InputModule.joyPadClickButton = OVRInput.Button.Any;
        }
    }
}

