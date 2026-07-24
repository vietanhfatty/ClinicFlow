namespace MyProject.Application.Configuration;

/// <summary>
/// Cấu hình hàng chờ khám. Bind từ section "QueueSettings" trong appsettings.json.
/// </summary>
public class QueueSettings
{
    /// <summary>
    /// Số phút dung sai (grace period) sau giờ hẹn. Trong khoảng này, bệnh nhân check-in
    /// vẫn giữ ưu tiên theo giờ đặt gốc; quá khoảng này sẽ bị chuyển sang "Late".
    /// </summary>
    public int GracePeriodMinutes { get; set; } = 15;
}
