using System.Collections.Generic;

namespace MyProject.Application.Services;

/// <summary>
/// Defines a single measurable indicator for a lab test service
/// (e.g. "RBC" for a CBC test), including unit and normal range for display.
/// </summary>
public record LabTestIndicatorDefinition(string Key, string Label, string? Unit, string? NormalRange);

/// <summary>
/// Static catalog mapping a lab test service name to the set of structured
/// indicators that Staff must fill in when entering results for that service.
/// Services not present in this catalog (e.g. imaging/ultrasound) fall back
/// to a single free-text result field.
/// </summary>
public static class LabTestIndicatorCatalog
{
    private static readonly Dictionary<string, List<LabTestIndicatorDefinition>> _byServiceName = new()
    {
        ["Xét nghiệm máu tổng quát (CBC)"] = new()
        {
            new("RBC", "Hồng cầu (RBC)", "triệu/µL", "4.2 - 5.9"),
            new("WBC", "Bạch cầu (WBC)", "nghìn/µL", "4.0 - 10.0"),
            new("Hemoglobin", "Huyết sắc tố (Hemoglobin)", "g/dL", "12 - 16"),
            new("Hematocrit", "Hematocrit", "%", "36 - 46"),
            new("Platelets", "Tiểu cầu (Platelets)", "nghìn/µL", "150 - 400"),
        },
        ["Xét nghiệm đường huyết (Glucose)"] = new()
        {
            new("Glucose", "Glucose lúc đói", "mg/dL", "70 - 100"),
        },
        ["Xét nghiệm chức năng gan (ALT, AST)"] = new()
        {
            new("ALT", "ALT (SGPT)", "U/L", "7 - 56"),
            new("AST", "AST (SGOT)", "U/L", "10 - 40"),
        },
        ["Xét nghiệm chức năng thận (Urea, Creatinine)"] = new()
        {
            new("Urea", "Urea", "mg/dL", "7 - 20"),
            new("Creatinine", "Creatinine", "mg/dL", "0.6 - 1.2"),
        },
        ["Điện tâm đồ (ECG)"] = new()
        {
            new("HeartRate", "Nhịp tim", "bpm", "60 - 100"),
            new("PRInterval", "PR interval", "ms", "120 - 200"),
            new("QRSDuration", "QRS duration", "ms", "80 - 120"),
            new("QTInterval", "QT interval", "ms", "350 - 440"),
        },
    };

    /// <summary>
    /// Gets the structured indicator definitions for a lab test service by name.
    /// Returns an empty list if the service has no structured indicators
    /// (e.g. imaging services like X-ray or ultrasound, which use free-text results).
    /// </summary>
    public static List<LabTestIndicatorDefinition> GetIndicators(string serviceName)
    {
        return _byServiceName.TryGetValue(serviceName, out var indicators)
            ? indicators
            : new List<LabTestIndicatorDefinition>();
    }

    /// <summary>
    /// Determines whether a lab test service has a defined set of structured indicators.
    /// </summary>
    public static bool HasIndicators(string serviceName) => _byServiceName.ContainsKey(serviceName);
}
