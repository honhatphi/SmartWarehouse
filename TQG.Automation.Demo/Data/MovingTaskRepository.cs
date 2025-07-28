using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace TQG.Automation.Demo.Data;

public class MovingTaskRepository
{

    public static readonly MovingTaskRepository Instance = new();

    private MovingTaskRepository() { }

    private readonly List<MovingTask> _tasks = [];

    public BindingList<MovingTask> GetAllTasks() => new(_tasks);

    public void AddTask(MovingTask task)
    {
        if (string.IsNullOrEmpty(task.TaskId))
            throw new ArgumentException("TaskId is required");

        if (_tasks.Any(t => t.TaskId == task.TaskId))
            throw new InvalidOperationException("TaskId already exists");

        _tasks.Add(task);
    }

    public void DeleteTask(string taskId)
    {
        var task = _tasks.FirstOrDefault(t => t.TaskId == taskId);
        if (task != null)
            _tasks.Remove(task);
    }

    public void DeleteByBarcode(string barcode)
    {
        var task = _tasks.FirstOrDefault(t => t.Barcode == barcode);

        if (task != null)
            _tasks.Remove(task);
    }

    public void DeleteAllTasks()
    {
        _tasks.Clear();
    }

    public List<MovingTask> GetAll()
    {
        return [.. _tasks];
    }

    public int Count => _tasks.Count;
}
