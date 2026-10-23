using System;
using System.Collections;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Bounded coroutine runner for the native canvas host. Async tasks advance only on its UI thread.</summary>
    public sealed class UiRoutineQueue : IDisposable
    {
        private readonly List<Stack<IEnumerator>> _routines = new List<Stack<IEnumerator>>();
        public bool HasWork => _routines.Count > 0;
        public void Run(IEnumerator routine) { var stack = new Stack<IEnumerator>(); stack.Push(routine); _routines.Add(stack); }
        public void Tick()
        {
            // Newly added work starts on the next tick; callbacks may enqueue but never reenter this loop.
            int count = _routines.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                var stack = _routines[i]; int budget = 64;
                try
                {
                    while (stack.Count > 0 && budget-- > 0)
                    {
                        if (!stack.Peek().MoveNext()) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                        if (stack.Peek().Current is IEnumerator nested) stack.Push(nested); else break;
                    }
                }
                catch { DisposeStack(stack); }
                if (stack.Count == 0) _routines.RemoveAt(i);
            }
        }
        private static void DisposeStack(Stack<IEnumerator> stack) { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); }
        public void Dispose() { foreach (var stack in _routines) DisposeStack(stack); _routines.Clear(); }
    }
}
