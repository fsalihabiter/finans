using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace Finans.Api.Observability;

/// <summary>
/// Redaksiyon politikası iskeleti (12 §3, 11 §7): yapılandırılmış log'da (`@nesne`)
/// hassas adlı özellikler `***` ile maskelenir — parola/token/secret/email asla
/// düz yazılmaz. Yalnızca hassas özellik İÇEREN nesnelere müdahale eder (dar etki).
/// Faz 1'de maskelenecek alan listesi genişletilir.
/// </summary>
public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> Sensitive = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passwordHash", "token", "tokenHash", "refreshToken", "accessToken",
        "apiKey", "secret", "authorization", "email",
    };

    private const string Mask = "***";

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, [NotNullWhen(true)] out LogEventPropertyValue? result)
    {
        result = null;
        var type = value.GetType();

        // Skaler/koleksiyon tipleri varsayılan davranışa bırak.
        if (type.IsPrimitive || value is string || value is IEnumerable)
            return false;

        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();

        if (properties.Length == 0 || !properties.Any(p => Sensitive.Contains(p.Name)))
            return false; // hassas alan yoksa karışma

        var logProperties = properties.Select(p => new LogEventProperty(
            p.Name,
            Sensitive.Contains(p.Name)
                ? new ScalarValue(Mask)
                : propertyValueFactory.CreatePropertyValue(SafeGet(p, value), destructureObjects: true)));

        result = new StructureValue(logProperties, type.Name);
        return true;
    }

    private static object? SafeGet(PropertyInfo property, object target)
    {
        try
        {
            return property.GetValue(target);
        }
        catch
        {
            return null;
        }
    }
}
