using System;
using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;

namespace Saraff.Twain.HardwareTests;

internal static class StaTestHost
{
    public static void Run(Action action, TimeSpan timeout)
    {
        Run(
            () =>
            {
                action();
                return true;
            },
            timeout);
    }

    public static T Run<T>(Func<T> action, TimeSpan timeout)
    {
        T result = default(T);
        ExceptionDispatchInfo error = null;
        var thread = new Thread(
            () =>
            {
                try
                {
                    result = action();
                }
                catch (Exception ex)
                {
                    error = ExceptionDispatchInfo.Capture(ex);
                }
            })
        {
            IsBackground = true,
            Name = "Saraff.Twain hardware test"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!thread.Join(timeout))
        {
            throw new TimeoutException($"TWAIN hardware test did not finish within {timeout}.");
        }

        error?.Throw();
        return result;
    }
}

internal sealed class HiddenWindow : IDisposable
{
    public HiddenWindow()
    {
        this.Form = new Form
        {
            Text = "Saraff.Twain hardware test",
            ShowInTaskbar = false,
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32000, -32000),
            Size = new Size(1, 1)
        };
        this.Form.Show();
        Application.DoEvents();
    }

    public Form Form { get; }

    public void Dispose()
    {
        this.Form.Close();
        this.Form.Dispose();
    }
}
