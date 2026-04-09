using System.Reflection;

namespace Github.Tests;

internal static class OctokitTestHelpers
{
    internal static void SetBackingField(object instance, Type type, string propertyName, object value)
    {
        var field = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(f => f.Name.Contains(propertyName, StringComparison.OrdinalIgnoreCase)
                              || f.Name.Contains($"<{propertyName}>"));

        field?.SetValue(instance, value);
    }
}
