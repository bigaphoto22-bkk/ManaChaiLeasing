using System.Threading;

namespace ManaChaiLeasing.Services;

public static class SingleInstanceService
{
    private const string MutexName =
        @"Local\ManaChaiLeasing.SingleInstance.5A9237D7";

    private const string ActivateEventName =
        @"Local\ManaChaiLeasing.ActivateExisting.5A9237D7";

    private static readonly object SyncRoot = new();

    private static Mutex? _instanceMutex;

    private static EventWaitHandle? _activateEvent;

    private static Thread? _listenerThread;

    private static bool _ownsInstance;

    private static bool _listenerStarted;

    public static bool TryAcquire()
    {
        lock (SyncRoot)
        {
            if (_ownsInstance)
            {
                return true;
            }

            try
            {
                _instanceMutex =
                    new Mutex(
                        initiallyOwned: true,
                        MutexName,
                        out bool createdNew);

                if (!createdNew)
                {
                    _instanceMutex.Dispose();
                    _instanceMutex = null;

                    SignalExistingInstance();

                    return false;
                }

                _ownsInstance = true;

                _activateEvent =
                    new EventWaitHandle(
                        initialState: false,
                        EventResetMode.AutoReset,
                        ActivateEventName);

                return true;
            }
            catch
            {
                // ถ้า Windows ไม่อนุญาตให้สร้าง synchronization object
                // ให้โปรแกรมยังเปิดได้ แทนที่จะ block ผู้ใช้ทั้งหมด
                return true;
            }
        }
    }

    public static void StartActivationListener(
        Action activateExistingWindow)
    {
        ArgumentNullException.ThrowIfNull(
            activateExistingWindow);

        lock (SyncRoot)
        {
            if (!_ownsInstance ||
                _activateEvent is null ||
                _listenerStarted)
            {
                return;
            }

            _listenerStarted = true;

            _listenerThread =
                new Thread(
                    () =>
                    {
                        while (true)
                        {
                            try
                            {
                                _activateEvent.WaitOne();

                                activateExistingWindow();
                            }
                            catch (ObjectDisposedException)
                            {
                                return;
                            }
                            catch
                            {
                                // Listener ต้องไม่ทำให้ Instance หลักปิดตัว
                            }
                        }
                    })
                {
                    IsBackground = true,
                    Name =
                        "ManaChaiLeasing.SingleInstanceListener"
                };

            _listenerThread.Start();
        }
    }

    private static void SignalExistingInstance()
    {
        try
        {
            using EventWaitHandle existingEvent =
                EventWaitHandle.OpenExisting(
                    ActivateEventName);

            existingEvent.Set();
        }
        catch
        {
            // ถ้า Instance แรกกำลังอยู่ช่วง Startup สั้น ๆ
            // อย่างน้อย Instance ที่สองจะไม่เปิด Main Window ซ้ำ
        }
    }
}
