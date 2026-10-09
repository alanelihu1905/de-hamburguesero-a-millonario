using System;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeField : Attribute { }

    public class MonoBehaviour
    {
        protected static T FindFirstObjectByType<T>() where T : class => null;
    }

    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b);
    }
}
