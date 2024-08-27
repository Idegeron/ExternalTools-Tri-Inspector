using System;
using UnityEngine;

namespace TriInspector
{
    public abstract class TriPropertyOverrideAvailability
    {
        private static TriPropertyOverrideAvailability Override { get; set; }
        public static TriPropertyOverrideAvailability Current { get; private set; }

        public abstract bool TryIsEnable(TriProperty property, out bool isEnable);

        public static EnterPropertyScope BeginProperty()
        {
            return new EnterPropertyScope().Init();
        }

        public static OverrideScope BeginOverride(TriPropertyOverrideAvailability overrideAvailability)
        {
            return new OverrideScope(overrideAvailability);
        }

        public struct EnterPropertyScope : IDisposable
        {
            private TriPropertyOverrideAvailability _previousAvailability;

            public EnterPropertyScope Init()
            {
                _previousAvailability = Current;
                Current = Override;
                Override = null;
                return this;
            }

            public void Dispose()
            {
                Override = Current;
                Current = _previousAvailability;
            }
        }

        public readonly struct OverrideScope : IDisposable
        {
            public OverrideScope(TriPropertyOverrideAvailability availability)
            {
                if (Override != null)
                {
                    Debug.LogError($"TriPropertyContext already overriden with {Override.GetType()}");
                }

                Override = availability;
            }

            public void Dispose()
            {
                Override = null;
            }
        }
    }
}