using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;
using Microsoft.Identity.Client;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class XcMSAL : MonoBehaviour
{
    public Text LogTextField;
    public Text DeviceCodeTextField;

    //private readonly string clientId = "ebe2ab4d-12b3-4446-8480-5c3828d04c50";
    private readonly string clientId = "6afec070-b576-4a2f-8d95-41f317b28e06"; //DesktopTestApp
    private readonly string redirectUrl = "https://login.microsoftonline.com/common/oauth2/nativeclient";
    private readonly string authority = "https://login.microsoftonline.com/common";
    private readonly List<string> scopes = new List<string>() { "User.Read" };
    private readonly string clientName = "DesktopTestApp";
    private string _deviceCode = "-";
    private string _frameworkversion;

    #region DLL Imports
    private const string UnityWindowClassName = "UnityWndClass";

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    //[System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern System.IntPtr GetActiveWindow();

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowsProc lpEnumFunc, IntPtr lParam);
    #endregion
    public string AuthorityOverride { get; set; }
    public string ExtraQueryParams { get; set; }
    public string LoginHint { get; set; }
    public IAccount CurrentUser { get; set; }
    public IPublicClientApplication PublicClientApplication { get; set; }
    static class TokenCacheHelper
    {
        public static string CacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + "msalcache.txt";

        private static readonly object FileLock = new object();

        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                args.TokenCache.DeserializeMsalV3(File.Exists(CacheFilePath)
                    ? File.ReadAllBytes(CacheFilePath)
                    : null);
            }
        }

        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                lock (FileLock)
                {
                    // reflect changesgs in the persistent store
                    File.WriteAllBytes(CacheFilePath, args.TokenCache.SerializeMsalV3());
                }
            }
        }
    }
    public static System.IntPtr GetWindowHandle()
    {
        return GetActiveWindow();
    }
    public void CreateOrUpdatePublicClientApp(string interactiveAuthority, string applicationId)
    {
        var builder = PublicClientApplicationBuilder
            .Create(applicationId)
            //.WithParentActivityOrWindow(GetWindowHandle)
            //.WithParentActivityOrWindow(new WindowInteropHelper(this).Handle)
            .WithClientName(clientName);
        builder.WithParentActivityOrWindow(GetActiveWindow);

        if (!string.IsNullOrWhiteSpace(interactiveAuthority))
        {
            // Use the override authority provided
            builder = builder.WithAuthority(new Uri(interactiveAuthority), true);
        }

        PublicClientApplication = builder.Build();

        PublicClientApplication.UserTokenCache.SetBeforeAccess(TokenCacheHelper.BeforeAccessNotification);
        PublicClientApplication.UserTokenCache.SetAfterAccess(TokenCacheHelper.AfterAccessNotification);
    }
    public async Task<AuthenticationResult> AcquireTokenInteractiveAsyncXC(
            IEnumerable<string> scopes,
            Prompt uiBehavior,
            string extraQueryParams)
    {
        CreateOrUpdatePublicClientApp(authority, clientId);

        AuthenticationResult result;
        if (CurrentUser != null)
        {
            result = await PublicClientApplication
                .AcquireTokenInteractive(scopes)
                .WithAccount(CurrentUser)
                .WithPrompt(uiBehavior)
                .WithExtraQueryParameters(extraQueryParams)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
        }
        else
        {
            result = await PublicClientApplication
                .AcquireTokenInteractive(scopes)
                .WithLoginHint(LoginHint)
                .WithPrompt(uiBehavior)
                .WithExtraQueryParameters(extraQueryParams)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
        }
        CurrentUser = result.Account;

        return result;
    }

    // Start is called before the first frame update
    void Start()
    {
        //GameObject logText = GameObject.Find("LogText");
        //GameObject deviceCode = GameObject.Find("DeviceCodeText");

        LogTextField = GameObject.Find("LogText").GetComponent<Text>();
        DeviceCodeTextField = GameObject.Find("DeviceCodeText").GetComponent<Text>();
        Debug.Log("MSAL Start");
    }

    // Update is called once per frame
    void Update()
    {
        DeviceCodeTextField.text = $"Device code: {_deviceCode}";
        //Debug.Log("MSAL Update");
    }

    //// Start is called before the first frame update
    //// https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    //async void Start()
    //{
    //    await Login();

    //}

    //// Update is called once per frame
    //// https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    //void Update()
    //{
    //    DeviceCodeTextField.text = $"Device code: {_deviceCode}";
    //}

    public async void SilentMSALLogin()
    {
        await SilentLogin();
    }

    public async void InteractiveMSAKLogin()
    {
        //await InteractiveLogin();
        await DoAcquireTokenInteractive();
    }

    private async Task<AuthenticationResult> DoAcquireTokenInteractive()
    {
        AuthenticationResult authenticationResult;
        try
        {
            authenticationResult = await AcquireTokenInteractiveAsyncXC(
                scopes,
                Prompt.SelectAccount,
                ExtraQueryParams).ConfigureAwait(true);

        }
        catch (Exception exc)
        {
            CreateException(exc);
            authenticationResult = null;
        }
        return authenticationResult;
    }

    private async Task InteractiveLogin()
    {
        IPublicClientApplication PublicClientApp = PublicClientApplicationBuilder.Create(clientId)
            .WithAuthority(authority)
            .WithRedirectUri(redirectUrl)
            .WithLogging((level, message, pii) => {
                Debug.Log($"MSAL [{level}] {pii} - {message}");
            }, LogLevel.Verbose, true)
            .Build();

        Debug.Log("Getting Accounts");
        var accounts = await PublicClientApp.GetAccountsAsync();

        AuthenticationResult result;
        try
        {
            Debug.Log("Trying Silent Token");
            result = await PublicClientApp.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                        .ExecuteAsync();
        }
        catch (MsalUiRequiredException)
        {
            Debug.Log("Failed. Trying Interactive Token");
            // This is the line where I can't remove the "parent" object 
            // (in this case "windowPtr") without the error seen in the image 
            // below. It has no compile problems like this but it always has a 
            // null pointer exception when it runs. Everything is the same in the 
            // first "Try" except this "parent" object I'm forced to supply.
            result = await PublicClientApp.AcquireTokenInteractive(scopes)
                        .WithUseEmbeddedWebView(true)
                        .ExecuteAsync();


        }
    }

    private async Task SilentLogin()
    {
        IPublicClientApplication app = PublicClientApplicationBuilder.Create(clientId)
            .WithAuthority(authority)
            .WithRedirectUri(redirectUrl)
            .WithLogging((level, message, pii) => {
                Debug.Log($"MSAL [{level}] {pii} - {message}");
            }, LogLevel.Verbose, true)
            .Build();

        await GetToken(); // Acquires token with device code
        await GetToken(); // Acquires token silently

        async Task GetToken()
        {
            AuthenticationResult authResult = null;

            try
            {
                var accounts = await app.GetAccountsAsync();
                Log("MSAL acquiring token silently.");
                authResult = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync(CancellationToken.None);
                Log("MSAL acquired token silently.");
            }
            catch (MsalUiRequiredException)
            {
                Log("MSAL acquiring token with device code.");
                authResult = await app
                    .AcquireTokenWithDeviceCode(
                        scopes,
                        deviceCodeResult => {
                            _deviceCode = deviceCodeResult.UserCode;
                            //CopyToClipboard(deviceCodeResult.UserCode);

                            return Task.CompletedTask;
                        })
                    .ExecuteAsync();
                Log("MSAL acquired token with device code.");
            }
            catch (Exception ex)
            {
                Log($"MSAL Exception acquiring token: {ex}");
            }

            if (authResult is null)
            {
                Log("MSAL auth result is null.");
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine($"MSAL Username={authResult.Account.Username}");
                //sb.AppendLine($"MSAL AccessToken={authResult.AccessToken}");
                //sb.AppendLine($"MSAL IdToken={authResult.IdToken}");
                sb.AppendLine($"MSAL Account={authResult.Account}");
                sb.AppendLine($"MSAL ExpiresOn={authResult.ExpiresOn}");
                sb.AppendLine($"MSAL Scopes={string.Join(";", authResult.Scopes)}");
                sb.AppendLine($"MSAL Tenant={authResult.TenantId}");
                sb.AppendLine($"MSAL TokenSource={authResult.AuthenticationResultMetadata.TokenSource}");
                Log(sb.ToString(), false);
            }
        }
    }

    private void Log(string message, bool addToDebugLog = true)
    {
        LogTextField.text += message + Environment.NewLine;
        if (addToDebugLog)
        {
            Debug.Log(message);
        }
    }
    private void CreateException(Exception ex)
    {
        string output = string.Empty;

        if (ex is MsalException exception)
        {
            output += string.Format(
                CultureInfo.InvariantCulture,
                "Error Code - {0}" + Environment.NewLine + "Message - {1}" + Environment.NewLine,
                exception.ErrorCode,
                exception.Message);

            if (exception is MsalServiceException)
            {
                output += string.Format(CultureInfo.InvariantCulture, "Status Code - {0}" + Environment.NewLine, ((MsalServiceException)exception).StatusCode);
                output += string.Format(CultureInfo.InvariantCulture, "Claims - {0}" + Environment.NewLine, ((MsalServiceException)exception).Claims);
                output += string.Format(CultureInfo.InvariantCulture, "Raw Response - {0}" + Environment.NewLine, ((MsalServiceException)exception).ResponseBody);
            }
        }
        else
        {
            output = ex.Message + Environment.NewLine + ex.StackTrace;
        }

        Log(output);
    }
}
