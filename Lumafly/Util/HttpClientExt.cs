using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Lumafly.Interfaces;
using Lumafly.Models;
using Org.BouncyCastle.Asn1.Ocsp;

namespace Lumafly.Util;

public static class HttpClientExt
{
    private static readonly string[] github_hosts = new string[]
        {
            "github.com",
            "raw.githubusercontent.com"
        };
    public static async Task<T> TryDo<T>(this HttpClient client, ISettings? settings, Uri uri, 
        Func<HttpClient, Uri, CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default) where T : class
    {
        if (settings is null ||
            !settings.UseGithubMirror ||
            string.IsNullOrEmpty(settings.GithubMirrorFormat) ||
            !github_hosts.Contains(uri.Host.ToLower()))
        {
            return await action(client, uri, cancellationToken);
        }
        T? result;
        try
        {
            var m_uri = string.Format(settings.GithubMirrorFormat,
                uri.ToString(), uri.Host, uri.Query, uri.PathAndQuery);
            
            Trace.WriteLine("Try use mirror url: " + m_uri);
            result = await action(client, new(m_uri), cancellationToken);
        }
        catch (Exception ex) when (ex is UriFormatException or HttpRequestException)
        {
            Trace.Write(ex);
            result = null;
        }
        if (result is null)
        {
            Trace.WriteLine("Use original url: " + uri.ToString());
            result = await action(client, uri, cancellationToken);
        }
        return result;
    }
    
    public static Task<HttpResponseMessage> SendAsync2(this HttpClient client, 
        ISettings? settings,
        HttpRequestMessage message,
        HttpCompletionOption httpCompletionOption,
        CancellationToken cancellationToken = default)
    {
        return client.TryDo(settings, message.RequestUri!, async (client, uri, cts) =>
        {
            message.RequestUri = uri;
            var resp = await client.SendAsync(message, httpCompletionOption, cancellationToken);
            resp.EnsureSuccessStatusCode();
            return resp;
        }, cancellationToken);
    }
    
    public static Task<HttpResponseMessage> GetAsync2(this HttpClient client,
        ISettings? settings,
        string uri,
        CancellationToken cancellation = default)
    {
        return TryDo(client, settings, new(uri), async (client, uri, cts) =>
        {
            var result = await client.GetAsync(uri);
            result.EnsureSuccessStatusCode();
            return result;
        }, cancellation);
    }

    public static Task<string> GetStringAsync2(this HttpClient client,
        ISettings? settings,
        string uri,
        CancellationToken cancellation = default)
    {
        return TryDo(client, settings, new(uri), async (client, uri, cts) =>
        {
            var result = await client.GetStringAsync(uri, cts);
            return result;
        }, cancellation);
    }
    public static Task<string> GetStringAsync2(this HttpClient client,
        ISettings? settings,
        Uri uri,
        CancellationToken cancellation = default)
    {
        return TryDo(client, settings, uri, async (client, uri, cts) =>
        {
            var result = await client.GetStringAsync(uri, cts);
            return result;
        }, cancellation);
    }

    public static async Task<(ArraySegment<byte>, HttpResponseMessage)> DownloadBytesWithProgressAsync
    (
        this HttpClient self,
        ISettings? settings,
        Uri uri,
        IProgress<DownloadProgressArgs> progress,
        CancellationToken cts = default
    )
    {
        HttpResponseMessage resp = await self.SendAsync2
        (
            settings,
            new HttpRequestMessage {
                Version = self.DefaultRequestVersion,
                Method = HttpMethod.Get,
                RequestUri = uri
            },
            HttpCompletionOption.ResponseHeadersRead,
            cts
        ).ConfigureAwait(false);

        Debug.Assert(resp is not null);

        resp.EnsureSuccessStatusCode();

        HttpContent content = resp.Content;

        await using Stream stream = await content.ReadAsStreamAsync(cts).ConfigureAwait(false);

        int dl_size = content.Headers.ContentLength is { } len
            ? (int) len
            : 65536;

        byte[] pool_buffer = ArrayPool<byte>.Shared.Rent(65536);
        
        Memory<byte> buf = pool_buffer;

        var memory = new MemoryStream(dl_size);

        var args = new DownloadProgressArgs {
            TotalBytes = (int?) content.Headers.ContentLength,
        };

        progress.Report(args);

        try
        {
            while (true)
            {
                cts.ThrowIfCancellationRequested();

                int read = await stream.ReadAsync(buf, cts).ConfigureAwait(false);
                
                await memory.WriteAsync(buf[..read], cts).ConfigureAwait(false);

                if (read == 0)
                    break;

                args.BytesRead += read;

                progress.Report(args);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pool_buffer);
        }

        args.Completed = true;

        progress.Report(args);
        
        ArraySegment<byte> res_segment = memory.TryGetBuffer(out ArraySegment<byte> out_buffer)
            ? out_buffer
            : memory.ToArray();

        return (res_segment, resp);
    }
}