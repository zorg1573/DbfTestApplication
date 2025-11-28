using System;
using System.Threading.Tasks;
using System.Windows.Forms;

public static class ControlExtensions
{
    public static Task InvokeAsync(this Control control, Func<Task> func)
    {
        var tcs = new TaskCompletionSource<bool>();

        control.BeginInvoke(new Action(async () =>
        {
            try
            {
                await func();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }));

        return tcs.Task;
    }
}
