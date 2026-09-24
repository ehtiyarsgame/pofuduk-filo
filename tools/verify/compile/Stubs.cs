// Signature stubs for UPM packages unavailable offline. Bodies are irrelevant — compile check only.
using System;
using Unity.Collections;
using Unity.Jobs;
namespace Unity.Burst { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Assembly)] public class BurstCompileAttribute : Attribute { } }
namespace Unity.Collections
{
    public struct NativeList<T> : IDisposable where T : unmanaged
    {
        public NativeList(int capacity, AllocatorManager.AllocatorHandle allocator) { }
        public NativeList(AllocatorManager.AllocatorHandle allocator) { }
        public int Length { get => 0; set { } }
        public int Capacity { get => 0; set { } }
        public bool IsCreated => true;
        public T this[int index] { get => default; set { } }
        public void Add(in T value) { }
        public void AddRange(NativeArray<T> array) { }
        public void Clear() { }
        public void RemoveAtSwapBack(int index) { }
        public NativeArray<T> AsArray() => default;
        public NativeArray<T> AsDeferredJobArray() => default;
        public void Dispose() { }
        public JobHandle Dispose(JobHandle inputDeps) => default;
    }
    public struct NativeQueue<T> : IDisposable where T : unmanaged
    {
        public NativeQueue(AllocatorManager.AllocatorHandle allocator) { }
        public int Count => 0;
        public bool IsCreated => true;
        public void Enqueue(T value) { }
        public bool TryDequeue(out T item) { item = default; return false; }
        public void Clear() { }
        public ParallelWriter AsParallelWriter() => default;
        public void Dispose() { }
        public struct ParallelWriter { public void Enqueue(T value) { } }
    }
    public struct NativeParallelMultiHashMapIterator<TKey> where TKey : unmanaged { }
    public struct NativeParallelMultiHashMap<TKey, TValue> : IDisposable where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
    {
        public NativeParallelMultiHashMap(int capacity, AllocatorManager.AllocatorHandle allocator) { }
        public bool IsCreated => true;
        public int Capacity { get => 0; set { } }
        public void Add(TKey key, TValue item) { }
        public void Clear() { }
        public bool TryGetFirstValue(TKey key, out TValue item, out NativeParallelMultiHashMapIterator<TKey> it) { item = default; it = default; return false; }
        public bool TryGetNextValue(out TValue item, ref NativeParallelMultiHashMapIterator<TKey> it) { item = default; return false; }
        public void Dispose() { }
    }
    public static class AllocatorManager
    {
        public struct AllocatorHandle { public static implicit operator AllocatorHandle(Allocator a) => default; }
    }
}
namespace UnityEngine.InputSystem
{
    public class InputDevice { }
    public class Pointer : InputDevice { public Controls.Vector2Control delta => null; public Controls.Vector2Control position => null; }
    public class Mouse : Pointer { public static Mouse current => null; public Controls.ButtonControl leftButton => null; }
    public class Keyboard : InputDevice { public static Keyboard current => null; public Controls.KeyControl escapeKey => null; }
    namespace Controls
    {
        public class InputControl<T> where T : struct { public T ReadValue() => default; }
        public class Vector2Control : InputControl<Vector2> { }
        public class ButtonControl : InputControl<float> { public bool isPressed => false; public bool wasPressedThisFrame => false; public bool wasReleasedThisFrame => false; }
        public class KeyControl : ButtonControl { }
    }
    namespace EnhancedTouch
    {
        public static class EnhancedTouchSupport { public static void Enable() { } public static void Disable() { } public static bool enabled => false; }
        public struct Touch
        {
            public static ReadOnlyArray<Touch> activeTouches => default;
            public Vector2 delta => default; public Vector2 screenPosition => default; public int touchId => 0;
            public UnityEngine.InputSystem.TouchPhase phase => default;
        }
    }
    public enum TouchPhase { None, Began, Moved, Ended, Canceled, Stationary }
    public struct ReadOnlyArray<T> { public int Count => 0; public T this[int i] => default; }
    namespace UI { public class InputSystemUIInputModule : UnityEngine.EventSystems.BaseInputModule { public override void Process() { } public void AssignDefaultActions() { } } }
}
namespace UnityEngine { public static class Handheld { public static void Vibrate() { } } } // missing from the 2021.3 reference pack; exists in Unity 6
