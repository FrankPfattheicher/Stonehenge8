using System;
using IctBaden.Stonehenge.Core;

namespace IctBaden.Stonehenge.Extension;

public class SessionStateProvider : IStateProvider
{
    private readonly AppSession _session;

    public SessionStateProvider(AppSession session)
    {
        _session = session;
    }
    public bool GetExpanded(string id) => (bool) Convert.ChangeType(_session[$"expanded-{id}"] ?? false, typeof(bool));
    public void SetExpanded(string id, bool expanded) => _session[$"expanded-{id}"] = expanded;
    public bool GetChecked(string id) => (bool) Convert.ChangeType(_session[$"checked-{id}"] ?? false, typeof(bool));
    public void SetChecked(string id, bool expanded) => _session[$"checked-{id}"] = expanded;
}