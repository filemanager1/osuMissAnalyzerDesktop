using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OsuMissAnalyzer.UI.ViewModels;
using SixLabors.ImageSharp;
using System;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OsuMissAnalyzer.UI.Views
{
    public partial class MissWindow : Window
    {
        public MissWindow()
        {
            InitializeComponent();

            Activated += (s, e) =>
            {
                App.IsWindowInBackground = false;
                if (App.PendingForegroundReplay)
                {
                    App.PendingForegroundReplay = false;
                    _ = App.Load(App.ReplayLoader);
                }
            };
            Deactivated += (s, e) => App.IsWindowInBackground = true;

            DataContextChanged += (a, b) =>
            {
                if (DataContext is MissWindowViewModel vm)
                {
                    MissCanvas.GetObservable(BoundsProperty).Subscribe(value => vm.Bounds = value);
                    PointerWheelChanged += vm.OnMouseWheel;
                    KeyDown += vm.OnKeyDown;
                    PointerReleased += vm.OnMouseReleased;

                    vm.ClipboardCopyHandler = async img =>
                    {
                        CopyImageToClipboard(img);
                        await ShowCopyNotification();
                    };
                }
            };
        }

        private static void CopyImageToClipboard(SixLabors.ImageSharp.Image img)
        {
            using var ms = new MemoryStream();
            img.SaveAsBmp(ms);
            byte[] bmpBytes = ms.ToArray();

            int dibSize = bmpBytes.Length - 14;
            IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)dibSize);
            if (hGlobal == IntPtr.Zero)
                throw new InvalidOperationException("GlobalAlloc failed");

            try
            {
                IntPtr pData = GlobalLock(hGlobal);
                if (pData == IntPtr.Zero)
                    throw new InvalidOperationException("GlobalLock failed");

                try
                {
                    Marshal.Copy(bmpBytes, 14, pData, dibSize);
                }
                finally
                {
                    GlobalUnlock(hGlobal);
                }

                if (!OpenClipboard(IntPtr.Zero))
                    throw new InvalidOperationException("OpenClipboard failed");

                try
                {
                    EmptyClipboard();
                    if (SetClipboardData(CF_DIB, hGlobal) == IntPtr.Zero)
                    {
                        hGlobal = IntPtr.Zero;
                        throw new InvalidOperationException("SetClipboardData failed");
                    }
                    hGlobal = IntPtr.Zero;
                }
                finally
                {
                    CloseClipboard();
                }
            }
            finally
            {
                if (hGlobal != IntPtr.Zero)
                    GlobalFree(hGlobal);
            }
        }

        private async Task ShowCopyNotification()
        {
            CopyNotification.IsVisible = true;
            CopyNotification.Opacity = 1;
            await Task.Delay(1200);
            CopyNotification.Opacity = 0;
            await Task.Delay(300);
            CopyNotification.IsVisible = false;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalFree(IntPtr hMem);

        private const uint GMEM_MOVEABLE = 0x0002;
        private const uint CF_DIB = 8;
    }
}
