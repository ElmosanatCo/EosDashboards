using System.Text;
using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Provisioning;

namespace EosDashboards.AdminProvisioner;

public interface IInteractiveConsole
{
    void Write(string value);

    void WriteLine(string value);

    string? ReadLine();

    string? ReadSecret();
}

public sealed class InteractiveInput(
    IInteractiveConsole console,
    IMobileProtector mobileProtector)
{
    public ProvisionSystemAdministratorCommand? Read()
    {
        console.Write("شناسه پایدار سازمانی: ");
        var organizationalId = console.ReadLine();
        console.Write("نام حساب سازمانی: ");
        var accountName = console.ReadLine();
        console.Write("نام کاربری: ");
        var username = console.ReadLine();
        console.Write("رمز عبور (مخفی): ");
        var password = console.ReadSecret();
        console.Write("نام: ");
        var firstName = console.ReadLine();
        console.Write("نام خانوادگی: ");
        var lastName = console.ReadLine();
        console.Write("شماره همراه (مخفی): ");
        var mobile = console.ReadSecret();
        console.Write("رایانامهٔ حساب گوگل (اختیاری و مخفی): ");
        var googleEmail = console.ReadSecret();
        console.WriteLine(string.Empty);

        if (organizationalId is null ||
            accountName is null ||
            username is null ||
            password is null ||
            firstName is null ||
            lastName is null ||
            mobile is null ||
            googleEmail is null)
        {
            return Cancel();
        }

        var normalizedMobile = mobile.Trim();
        string maskedMobile;
        try
        {
            maskedMobile = mobileProtector.Mask(normalizedMobile);
        }
        catch (ArgumentException)
        {
            console.WriteLine("شماره همراه معتبر نیست.");
            return null;
        }

        console.WriteLine($"شماره همراه برای تأیید: {maskedMobile}");
        console.Write("ادامه دهم؟ (بله/yes): ");
        var confirmation = console.ReadLine()?.Trim();
        if (!string.Equals(confirmation, "بله", StringComparison.Ordinal) &&
            !string.Equals(confirmation, "yes", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(confirmation, "y", StringComparison.OrdinalIgnoreCase))
        {
            return Cancel();
        }

        return new ProvisionSystemAdministratorCommand(
            organizationalId,
            accountName,
            username,
            password,
            firstName,
            lastName,
            normalizedMobile,
            googleEmail);
    }

    private ProvisionSystemAdministratorCommand? Cancel()
    {
        console.WriteLine("عملیات لغو شد.");
        return null;
    }
}

public sealed class SystemInteractiveConsole : IInteractiveConsole
{
    public void Write(string value) => Console.Write(value);

    public void WriteLine(string value) => Console.WriteLine(value);

    public string? ReadLine() => Console.ReadLine();

    public string? ReadSecret()
    {
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine();
        }

        var value = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                return value.ToString();
            }

            if (key.Key == ConsoleKey.Escape || key.KeyChar == '\u001a')
            {
                return null;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (value.Length > 0)
                {
                    value.Length--;
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                value.Append(key.KeyChar);
            }
        }
    }
}
