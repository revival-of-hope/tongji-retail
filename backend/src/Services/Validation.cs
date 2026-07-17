using System.Net.Mail;

namespace RetailSystem.Backend.Services;

/// <summary>
/// 提供接口层可复用的轻量业务参数校验。
/// </summary>
public static class Validation
{
    public static string? Register(string username, string password, string? email)
    {
        username = username.Trim();

        if (username.Length is < 3 or > 50)
        {
            return "用户名长度必须为 3—50 个字符";
        }

        if (password.Length is < 6 or > 100)
        {
            return "密码长度必须为 6—100 个字符";
        }

        if (!string.IsNullOrWhiteSpace(email) && !MailAddress.TryCreate(email, out _))
        {
            return "邮箱格式不正确";
        }

        return null;
    }

    public static string? Product(string name, decimal price, int stock)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
        {
            return "商品名称不能为空且不能超过 200 个字符";
        }

        if (price <= 0)
        {
            return "商品价格必须大于 0";
        }

        if (stock < 0)
        {
            return "库存不能小于 0";
        }

        return null;
    }

    public static string? Ticket(string subject, string description)
    {
        if (string.IsNullOrWhiteSpace(subject) || subject.Trim().Length > 200)
        {
            return "工单主题不能为空且不能超过 200 个字符";
        }

        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 2000)
        {
            return "工单描述不能为空且不能超过 2000 个字符";
        }

        return null;
    }
}
