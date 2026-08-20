using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ManaChaiLeasing.Services;

public static class ApplicationDiagnosticsService
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        AppLog.Initialize();

        AppDomain.CurrentDomain.UnhandledException +=
            CurrentDomain_UnhandledException;

        TaskScheduler.UnobservedTaskException +=
            TaskScheduler_UnobservedTaskException;

        if (Application.Current is not null)
        {
            Application.Current.DispatcherUnhandledException +=
                Application_DispatcherUnhandledException;
        }
    }

    private static void Application_DispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        AppLog.Critical(
            "Unhandled WPF dispatcher exception.",
            e.Exception);

        try
        {
            MessageBox.Show(
                "โปรแกรมพบข้อผิดพลาดที่ไม่คาดคิดและจำเป็นต้องปิด\n\n" +
                "ระบบได้บันทึก Technical Log ไว้แล้ว\n" +
                "หากปัญหาเกิดซ้ำ ให้เปิดโปรแกรมใหม่แล้วไปที่\n" +
                "ตั้งค่า > ช่วยเหลือ / ตรวจสอบปัญหา\n" +
                "เพื่อสร้างชุดข้อมูล Support",
                "มานะชัย ลิสซิ่ง - พบข้อผิดพลาด",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // หาก UI อยู่ในสภาพที่แสดง MessageBox ไม่ได้ ให้ปิดโปรแกรมต่อ
        }

        e.Handled = true;

        try
        {
            Application.Current?.Shutdown(
                -1);
        }
        catch
        {
            Environment.Exit(
                -1);
        }
    }

    private static void CurrentDomain_UnhandledException(
        object? sender,
        UnhandledExceptionEventArgs e)
    {
        Exception? exception =
            e.ExceptionObject as Exception;

        AppLog.Critical(
            "Unhandled AppDomain exception.",
            exception);

        if (exception is null)
        {
            AppLog.Critical(
                $"Unhandled AppDomain object: {e.ExceptionObject}");
        }
    }

    private static void TaskScheduler_UnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        AppLog.Error(
            "Unobserved Task exception.",
            e.Exception);

        e.SetObserved();
    }
}
