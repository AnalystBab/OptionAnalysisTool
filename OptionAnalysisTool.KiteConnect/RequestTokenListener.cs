using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;
using Microsoft.Extensions.Logging;

namespace OptionAnalysisTool.KiteConnect
{
    public class RequestTokenListener
    {
        private readonly HttpListener _listener;
        private readonly string _redirectUrl;
        private readonly ILogger _logger;
        private TaskCompletionSource<string> _tokenCompletionSource;

        public RequestTokenListener(string redirectUrl, ILogger logger = null)
        {
            _redirectUrl = redirectUrl;
            _logger = logger;
            _listener = new HttpListener();

            // Match the exact redirect URL from Zerodha settings
            var prefix = "http://127.0.0.1:3000/";
            _listener.Prefixes.Add(prefix);
            _logger?.LogDebug("Created HTTP listener with prefix: {prefix}", prefix);

            // Check if we have permission to listen
            EnsureHttpListenerPermission(prefix);
        }

        private void EnsureHttpListenerPermission(string prefix)
        {
            try
            {
                // Check if running with admin rights
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

                _logger?.LogDebug("Checking HTTP listener permissions. IsAdmin: {isAdmin}", isAdmin);

                if (!isAdmin)
                {
                    _logger?.LogInformation("Not running as admin, attempting to add URL reservation");
                    // Add URL reservation for non-admin users
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "netsh",
                            Arguments = $"http add urlacl url={prefix} user={identity.Name}",
                            Verb = "runas",
                            UseShellExecute = true,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                    _logger?.LogInformation("URL reservation added successfully");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to set up HTTP listener permissions");
                throw new Exception($"Failed to set up HTTP listener permissions: {ex.Message}. Try running the application as administrator.", ex);
            }
        }

        public async Task<string> WaitForRequestTokenAsync()
        {
            _tokenCompletionSource = new TaskCompletionSource<string>();

            try
            {
                _logger?.LogDebug("Starting HTTP listener on {url}", "http://localhost:3000/");
                _listener.Start();
                _logger?.LogInformation("HTTP listener started successfully");

                // Handle incoming requests
                _logger?.LogDebug("Beginning to listen for callback request");
                _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);

                // Wait for the token with a longer timeout (10 minutes)
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(10));
                try
                {
                    _logger?.LogInformation("Waiting for callback (timeout: 10 minutes). Please complete the login in your browser.");
                    _logger?.LogInformation("If the browser doesn't open automatically, please manually open this URL: {url}", _redirectUrl);
                    
                    using (cts.Token.Register(() => 
                    {
                        _logger?.LogWarning("Login timeout reached. No callback received from Kite Connect.");
                        _tokenCompletionSource.TrySetCanceled();
                    }))
                    {
                        var token = await _tokenCompletionSource.Task;
                        _logger?.LogInformation("Successfully received callback and token");
                        return token;
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger?.LogError("Timeout waiting for Kite Connect login. Please verify:");
                    _logger?.LogError("1. Browser opened the login page");
                    _logger?.LogError("2. Redirect URL in Kite Connect API settings is set to: http://localhost:3000/");
                    _logger?.LogError("3. You completed the login process");
                    throw new TimeoutException(
                        "Timeout waiting for Kite Connect login. Please verify:\n" +
                        "1. Browser opened the login page\n" +
                        "2. Redirect URL in Kite Connect API settings is set to: http://localhost:3000/\n" +
                        "3. You completed the login process");
                }
            }
            catch (HttpListenerException ex)
            {
                _logger?.LogError(ex, "Failed to start HTTP listener. Error code: {code}", ex.ErrorCode);
                throw new Exception(
                    $"Failed to start HTTP listener. Please ensure:\n" +
                    $"1. Port 3000 is not in use\n" +
                    $"2. You have permission to listen on this port\n" +
                    $"Error: {ex.Message}", ex);
            }
            finally
            {
                try
                {
                    _logger?.LogDebug("Stopping HTTP listener");
                    _listener.Stop();
                    _logger?.LogInformation("HTTP listener stopped successfully");
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error while stopping HTTP listener");
                }
            }
        }

        private void ListenerCallback(IAsyncResult result)
        {
            try
            {
                _logger?.LogDebug("Received callback request");
                var listener = (HttpListener)result.AsyncState!;
                var context = listener.EndGetContext(result);

                // Get the request token from the URL query parameters
                var requestToken = context.Request.QueryString["request_token"];
                _logger?.LogDebug("Request token present: {hasToken}", !string.IsNullOrEmpty(requestToken));

                // Send a response to close the browser window
                var response = context.Response;
                var responseString = @"
                    <html>
                    <head>
                        <title>Authentication Successful</title>
                        <style>
                            body { font-family: Arial, sans-serif; text-align: center; padding-top: 50px; }
                            .success { color: #4CAF50; }
                        </style>
                    </head>
                    <body>
                        <h2 class='success'>Authentication Successful!</h2>
                        <p>You can now close this window and return to the application.</p>
                        <script>
                            setTimeout(function() { window.close(); }, 3000);
                        </script>
                    </body>
                    </html>";

                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.ContentType = "text/html";
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();
                _logger?.LogDebug("Sent success response to browser");

                // Set the result
                if (!string.IsNullOrEmpty(requestToken))
                {
                    _logger?.LogInformation("Successfully received request token");
                    _tokenCompletionSource.TrySetResult(requestToken);
                }
                else
                {
                    var error = "No request token found in the response";
                    _logger?.LogError(error);
                    _tokenCompletionSource.TrySetException(new Exception(error));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in listener callback");
                _tokenCompletionSource.TrySetException(ex);
            }
        }
    }
} 