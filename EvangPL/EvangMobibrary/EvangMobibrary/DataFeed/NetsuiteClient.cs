using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.Messaging;
using EvangSol.Mobibrary.Dialog;
using EvangSol.Mobibrary.EvangModel;
using EvangSol.Mobibrary.EvangViewModel;
using EvangSol.Mobibrary.Utilities.Common;
using EvangSol.Mobibrary.Utilities.Message;
using System.Text;
using System.Text.Json;

namespace EvangSol.Mobibrary.DataFeed;

#region data structure
public class EvangDatum<I, D> where I : EvangJsonModel where D : EvangJsonModel
{
    public I? Info { get; set; }
    public List<D>? Data { get; set; }
}

public class EvangSubDatum
{
    public string? SubName { get; set; }
    public string? SubJson { get; set; }
}

public class RequestData<I, D> : EvangDatum<I, D> where I : EvangJsonModel where D : EvangJsonModel
{
    public string restlet_id { get; set; }
    public string? Operate { get; set; }

    public RequestData(string restletid)
    {
        restlet_id = restletid;
    }
}

public class RequestData : RequestData<EvangJsonModel, EvangJsonModel>
{
    public RequestData(string restletid) : base(restletid) { }
}

public class ResponseData<I, D> : EvangDatum<I, D> where I : EvangJsonModel where D : EvangJsonModel
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public EvangDatum<I, D>? Datum { get; set; }
    public List<EvangSubDatum>? SubData { get; set; }
}
#endregion

public static class NetsuiteClient
{
    public static LoadingDialog? loading;
    public static OAuth2Client oauth2_client = new();

    static List<string> runninglist = [];
    static double timeout = 100;

    public static async Task<ResponseData<EvangJsonModel, EvangJsonModel>?> Post(
        this EvangContentVM? ecvm,
        string restletid,
        Func<string?>? functojson = null,
        OnDialogConfirm? onConfirm = null,
        bool showerr = true,
        bool ignoredualcheck = false)
    {
        return await Post<EvangJsonModel, EvangJsonModel, EvangJsonModel, EvangJsonModel>(ecvm, new RequestData(restletid), functojson, onConfirm, showerr, ignoredualcheck);
    }

    public static async Task<ResponseData<TResInfo, TResData>?> Post<TResInfo, TResData>(
        this EvangContentVM? ecvm,
        string restletid,
        Func<string?>? functojson = null,
        OnDialogConfirm? onConfirm = null,
        bool showerr = true,
        bool ignoredualcheck = false)
        where TResInfo : EvangJsonModel where TResData : EvangJsonModel
    {
        return await Post<EvangJsonModel, EvangJsonModel, TResInfo, TResData>(ecvm, new RequestData(restletid), functojson, onConfirm, showerr, ignoredualcheck);
    }

    public static async Task<ResponseData<EvangJsonModel, EvangJsonModel>?> Post<TReqInfo, TReqData>(
        this EvangContentVM? ecvm,
        RequestData<TReqInfo, TReqData> request,
        Func<string?>? functojson = null,
        OnDialogConfirm? onConfirm = null,
        bool showerr = true,
        bool ignoredualcheck = false)
        where TReqInfo : EvangJsonModel where TReqData : EvangJsonModel
    {
        return await Post<TReqInfo, TReqData, EvangJsonModel, EvangJsonModel>(ecvm, request, functojson, onConfirm, showerr, ignoredualcheck);
    }

    public static async Task<ResponseData<TResInfo, TResData>?> Post<TReqInfo, TReqData, TResInfo, TResData>(
        this EvangContentVM? ecvm,
        RequestData<TReqInfo, TReqData> request,
        Func<string?>? functojson = null,
        OnDialogConfirm? onConfirm = null,
        bool showerr = true,
        bool ignoredualcheck = false)
        where TReqInfo : EvangJsonModel where TReqData : EvangJsonModel
        where TResInfo : EvangJsonModel where TResData : EvangJsonModel
    {
        if (!LocalMemory.restlets.Keys.Contains(request.restlet_id))
        {
            if (ecvm != null && showerr)
                ErrorDialog(ecvm, "Invalid restlet id.", EvangDialog.DialogType.Error);
            return null;
        }
        var endpoint = LocalMemory.restlets[request.restlet_id];

        if (!ignoredualcheck && runninglist.Contains(request.restlet_id))
            return null;
        runninglist.Add(request.restlet_id);

        //showing loading dialog
        if (loading == null)
            loading = new LoadingDialog();
        await RunOnMainThreadAsync(null, () => loading.LoadingShow(ecvm));

        //get access token
        var (accesstoken, errmsg) = await oauth2_client.GetValidAccessTokenAsync();
        if (string.IsNullOrEmpty(accesstoken))
        {
            if (ecvm != null && showerr)
                ErrorDialog(ecvm, errmsg ?? "Can not retieve access token.", EvangDialog.DialogType.Error);
            return null;
        }

        //serialize request body
        var json = functojson?.Invoke() ?? JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //set authorization header
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accesstoken);

        HttpResponseMessage? response = null;
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(timeout));

            //get post task
            var postTask = client.PostAsync(endpoint, content);

            //create a delay task
            var timeoutTask = Task.Delay(Timeout.InfiniteTimeSpan, linkedCts.Token);

            //wait for either the POST request to complete or the timeout to occur
            var completedTask = await Task.WhenAny(postTask, timeoutTask);
            //check if it's cancelled
            if (linkedCts.Token.IsCancellationRequested && !postTask.IsCompleted)
            {
                //close loading dialog
                if (loading != null && ecvm != null)
                {
                    await RunOnMainThreadAsync(ecvm, () =>
                    {
                        if (loading != null)
                        {
                            if (loading.LoadingClose())
                                loading = null;
                        }
                    });
                }
                // Timeout occurred
                WeakReferenceMessenger.Default.Send(new NetsuiteTimeoutMessage(oauth2_client.ClientId, endpoint));
                return null;
            }

            response = await postTask;
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                //deserialize response
                var result = JsonSerializer.Deserialize<ResponseData<TResInfo, TResData>>(responseContent);
                if (result != null)
                {
                    if (!result.Success)
                    {
                        if (ecvm != null && showerr)
                            ErrorDialog(ecvm, result.ErrorMessage ?? "NetSuite failed to get data.", EvangDialog.DialogType.Error, onConfirm);
                        return null;
                    }
                    return result;
                }
                else
                {
                    if (ecvm != null && showerr)
                        ErrorDialog(ecvm, "Can not deserialize Json data.", EvangDialog.DialogType.Error, onConfirm);
                }
            }
            else
            {
                if (ecvm != null && showerr)
                    ErrorDialog(ecvm, responseContent.ToString());
            }
        }
        catch (JsonException ex)
        {
            if (ecvm != null && showerr)
                ExceptionDialog(ecvm, ex.Message);
        }
        catch (Exception ex)
        {
            if (ecvm != null && showerr)
                ErrorToast(ecvm, ex.Message);
        }
        finally
        {
            //clear authorization header to avoid leaking token in future requests
            client.DefaultRequestHeaders.Authorization = null;
            runninglist.Remove(request.restlet_id);

            if (loading != null && ecvm != null)
            {
                await RunOnMainThreadAsync(ecvm, () =>
                {
                    //Depending on the timing, it may be null, so check again just before close.
                    if (loading != null)
                    {
                        if (loading.LoadingClose())
                            loading = null;
                    }
                });
            }
        }
        return null;
    }

    static void ErrorDialog(EvangContentVM ecvm, string errmsg, EvangDialog.DialogType dlgtype = EvangDialog.DialogType.Error, OnDialogConfirm? onConfirm = null)
    {
        RunOnMainThread(null, () =>
        {
            var errdlg = new MessageDialog(BaseUtils.GetCustomString("lblError") ?? "Error", errmsg, dlgtype, onConfirm);
            ecvm.ShowPopup(errdlg);
        });
    }

    static void ExceptionDialog(EvangContentVM ecvm, string exmsg)
    {
        RunOnMainThread(null, () =>
        {
            var exdlg = new ExceptionDialog(BaseUtils.GetCustomString("lblError") ?? "Error", exmsg);
            ecvm.ShowPopup(exdlg);
        });
    }

    static async void ErrorToast(EvangContentVM ecvm, string errmsg)
    {
#if WINDOWS
            //await Toast.Make(errmsg, CommunityToolkit.Maui.Core.ToastDuration.Long).Show();
            if (ecvm != null)
                await Task.Run(() => ErrorDialog(ecvm, errmsg));
#else
        var snackbarOptions = new SnackbarOptions
        {
            BackgroundColor = BaseUtils.GetColor("ErrorBackground") ?? Colors.Red,
            TextColor = Colors.White,
            ActionButtonTextColor = Colors.Yellow,
            CornerRadius = new CornerRadius(10),
            Font = Microsoft.Maui.Font.SystemFontOfSize(20),
            ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(20),
            //CharacterSpacing = 0.5
        };
        var snackbar = Snackbar.Make(errmsg, duration: TimeSpan.FromSeconds(1000), visualOptions: snackbarOptions);
        await snackbar.Show();
#endif
    }

    public static void RunOnMainThread(this EvangContentVM? ecvm, Action action)
    {
#if ANDROID
        Microsoft.Maui.ApplicationModel.Platform.CurrentActivity!.RunOnUiThread(action);
#elif IOS || MACCATALYST
            //nsObject.BeginInvokeOnMainThread(action);
#elif WINDOWS
            //Device.BeginInvokeOnMainThread(action);
            Application.Current?.MainPage?.Dispatcher.Dispatch(action);
#endif
    }

    public static Task RunOnMainThreadAsync(this EvangContentVM? ecvm, Action action)
    {
        var tcs = new TaskCompletionSource<bool>();

#if ANDROID
        Microsoft.Maui.ApplicationModel.Platform.CurrentActivity!.RunOnUiThread(() =>
        {
            try
            {
                action();
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
#elif IOS || MACCATALYST
            //
#elif WINDOWS
            //in Windows, the loading dialog may not close depending on the environment.
            //waiting for a while to avoid this
            BaseUtils.SetTimer(150, () =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
#endif

        return tcs.Task;
    }

    //public static async Task<T?> Get<T>(string endpoint) where T : EvangJsonModel
    //{
    //    var (accesstoken, errmsg) = await oauth2_client.GetValidAccessTokenAsync();
    //    if (accesstoken == null)
    //        throw new UnauthorizedAccessException(errmsg);

    //    using var client = new HttpClient();
    //    client.DefaultRequestHeaders.Authorization = new("Bearer", accesstoken);

    //    var response = await client.GetAsync(endpoint);
    //    response.EnsureSuccessStatusCode();
    //    var json = await response.Content.ReadAsStringAsync();
    //    return JsonSerializer.Deserialize<T>(json);
    //}
}
