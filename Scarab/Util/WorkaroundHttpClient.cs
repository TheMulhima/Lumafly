using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Scarab.Enums;
using Scarab.Models;

namespace Scarab.Util;

public static class WorkaroundHttpClient
{
    /// <summary>
    /// Re-try an action with the IPv4 workaround client
    /// </summary>
    /// <param name="httpSetting">Whether or not to skip trying the normal client</param>
    /// <param name="f">The action to try</param>
    /// <param name="config">A configurator for the HttpClient</param>
    /// <typeparam name="T">Return type of the action</typeparam>
    /// <remarks>It is expected that the action has a timeout, otherwise this will run indefinitely.</remarks>
    /// <returns>A result info containing the client, result, and whether the workaround was used.</returns>
    public static async Task<ResultInfo<T>> TryWithWorkaroundAsync<T>(
        HttpSetting httpSetting,
        Func<HttpClient, Task<T>> f,
        Action<HttpClient> config
    )
    {
        if (httpSetting != HttpSetting.OnlyWorkaround)
        {
            var hc = new HttpClient();

            try
            {
                config(hc);

                return new ResultInfo<T>
                (
                    await f(hc),
                    hc,
                    false
                );
            }
            catch (TaskCanceledException)
            {
                hc.Dispose();
                Trace.WriteLine("Failed with normal client, trying workaround.");
            }
        }

        var workaround = CreateWorkaroundClient();

        try
        {
            config(workaround);

            return new ResultInfo<T>
            (
                await f(workaround),
                workaround,
                true
            );
        }
        catch
        {
            workaround.Dispose();
            throw;
        }
    }

    // .NET has a thing with using IPv6 for IPv4 stuff, so on
    // networks and/or drivers w/ poor support this fails.
    // https://github.com/dotnet/runtime/issues/47267
    // https://github.com/fifty-six/Scarab/issues/47
    private static HttpClient CreateWorkaroundClient()
    {
        return new HttpClient(new SocketsHttpHandler {
            ConnectCallback = IPv4ConnectAsync
        });

        static async ValueTask<Stream> IPv4ConnectAsync(SocketsHttpConnectionContext context,
            CancellationToken cancellationToken)
        {
            // By default, we create dual-mode sockets:
            // Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            // Defaults to dual-mode sockets, which uses IPv6 for IPv4 stuff.
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.NoDelay = true;

            try
            {
                await socket.ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    }
}