using System.Net.Http;

namespace Scarab.Models;

public class ResultInfo<T>
{
    public T          Result           { get; }
    public HttpClient Client           { get; }
    public bool       NeededWorkaround { get; }

    public ResultInfo(T result, HttpClient client, bool neededWorkaround)
    {
        Result = result;
        Client = client;
        NeededWorkaround = neededWorkaround;
    }
}