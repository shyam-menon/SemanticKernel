using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace SK_AgentWorkflows.Tools;

public class ProjectTools
{
    private static Dictionary<string, ProjectTask> _tasks = new();
    private static Dictionary<string, Resource> _resources = new();
    private static List<string> _updates = new();

    [KernelFunction]
    [Description("Create a new task in the project")]
    public string CreateTask(
        [Description("Name of the task")] string name,
        [Description("Description of the task")] string description,
        [Description("Estimated duration in hours")] double estimatedHours)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        _tasks[id] = new ProjectTask
        {
            Id = id,
            Name = name,
            Description = description,
            EstimatedHours = estimatedHours,
            Status = "pending"
        };
        
        _updates.Add($"Created task: {name}");
        return id;
    }

    [KernelFunction]
    [Description("Update the status of a task")]
    public string UpdateTaskStatus(
        [Description("ID of the task")] string taskId,
        [Description("New status (pending|in_progress|completed|blocked)")] string status)
    {
        if (!_tasks.ContainsKey(taskId))
            return "Error: Task not found";

        _tasks[taskId].Status = status;
        _updates.Add($"Updated task {_tasks[taskId].Name} status to {status}");
        return "Status updated successfully";
    }

    [KernelFunction]
    [Description("Assign a resource to a task")]
    public string AssignResource(
        [Description("ID of the task")] string taskId,
        [Description("Name of the resource")] string resourceName,
        [Description("Skills of the resource")] string skills)
    {
        if (!_tasks.ContainsKey(taskId))
            return "Error: Task not found";

        var resourceId = Guid.NewGuid().ToString("N")[..8];
        _resources[resourceId] = new Resource
        {
            Id = resourceId,
            Name = resourceName,
            Skills = skills,
            AssignedTaskId = taskId
        };

        _updates.Add($"Assigned {resourceName} to task {_tasks[taskId].Name}");
        return resourceId;
    }

    [KernelFunction]
    [Description("Get the current status of all tasks")]
    public string GetProjectStatus()
    {
        var status = new
        {
            total_tasks = _tasks.Count,
            completed_tasks = _tasks.Count(t => t.Value.Status == "completed"),
            in_progress_tasks = _tasks.Count(t => t.Value.Status == "in_progress"),
            blocked_tasks = _tasks.Count(t => t.Value.Status == "blocked"),
            pending_tasks = _tasks.Count(t => t.Value.Status == "pending")
        };

        return JsonSerializer.Serialize(status);
    }

    [KernelFunction]
    [Description("Get recent project updates")]
    public string GetRecentUpdates()
    {
        return JsonSerializer.Serialize(
            _updates.TakeLast(5).ToList()
        );
    }

    [KernelFunction]
    [Description("Clear all project data")]
    public string ResetProject()
    {
        _tasks.Clear();
        _resources.Clear();
        _updates.Clear();
        return "Project data reset successfully";
    }
}

public class ProjectTask
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public double EstimatedHours { get; set; }
    public string Status { get; set; } = "pending";
}

public class Resource
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Skills { get; set; } = "";
    public string AssignedTaskId { get; set; } = "";
}
