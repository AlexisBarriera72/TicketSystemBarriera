namespace BarrieraMoving.Mobile;

// Puente entre las Task de Google Play Services (Android.Gms.Tasks.Task, estilo
// callback) y async/await de .NET. Se define aquí para no depender de que el
// binding traiga su propia extensión AsAsync (varía entre versiones).
internal static class GmsTaskExtensions
{
    public static System.Threading.Tasks.Task<Java.Lang.Object?> ToAwaitable(
        this global::Android.Gms.Tasks.Task task)
    {
        var tcs = new TaskCompletionSource<Java.Lang.Object?>();
        task.AddOnCompleteListener(new CompleteListener(tcs));
        return tcs.Task;
    }

    private sealed class CompleteListener(TaskCompletionSource<Java.Lang.Object?> tcs)
        : Java.Lang.Object, global::Android.Gms.Tasks.IOnCompleteListener
    {
        public void OnComplete(global::Android.Gms.Tasks.Task task)
        {
            if (task.IsSuccessful)
                tcs.TrySetResult(task.Result);
            else
                tcs.TrySetException(task.Exception ?? new Java.Lang.Exception("GMS task failed"));
        }
    }
}
