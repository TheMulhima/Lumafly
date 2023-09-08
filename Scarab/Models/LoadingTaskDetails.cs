using System;
using System.Threading.Tasks;

namespace Scarab.Models;

/// <summary>
/// struct to store details about a task that needs to run before re loading the app
/// </summary>
public struct LoadingTaskDetails
{
    public LoadingTaskDetails(Func<Task> task, string loadingMessage)
    {
        LoadingMessage = loadingMessage;
        Task = task;
    }

    public Func<Task> Task { get; }
    public string LoadingMessage { get; }
}